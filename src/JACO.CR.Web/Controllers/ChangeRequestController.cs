using System.Security.Claims;
using System.Text.Json;
using JACO.CR.Web.Data;
using JACO.CR.Web.Models;
using JACO.CR.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JACO.CR.Web.Controllers;

[Authorize]
public sealed class ChangeRequestController(
    CrDbContext db,
    ApprovalApiClient approvalApi,
    CRLookupService lookups,
    CRAttachmentStorage attachmentStorage) : Controller
{
    // CreatorUserId is a display-only int (CR has no local Users table); it comes
    // from the Portal user id carried in the SSO cookie's NameIdentifier claim.
    int CurrentUserId => int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status, string? department)
    {
        var mine = MineQuery();

        var model = new ChangeRequestListViewModel
        {
            TotalCount = await mine.CountAsync(),
            DraftCount = await mine.CountAsync(x => x.Status == "Draft"),
            PendingApprovalCount = await mine.CountAsync(x => x.Status == "Pending Approval"),
            CompletedCount = await mine.CountAsync(x => x.Status == "Completed"),
            Search = search,
            Status = status,
            Department = department,
            Departments = await lookups.GetAsync("Department")
        };

        model.Rows = await FilteredQuery(mine, search, status, department).ToListAsync();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Export(string? search, string? status, string? department)
    {
        var rows = await FilteredQuery(MineQuery(), search, status, department).ToListAsync();
        var bytes = CsvHelper.ToCsvBytes(rows,
            ["CR No.", "Title", "Department", "Priority", "Status", "Submitted On"],
            x => [x.CRNumber, x.Title, x.Department, x.Priority, x.Status, x.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")]);
        return File(bytes, "text/csv", $"change-requests-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
    }

    IQueryable<ChangeRequest> MineQuery()
    {
        var userName = User.Identity!.Name!;
        return db.ChangeRequests.AsNoTracking().Where(x => x.CreatorUserName == userName);
    }

    static IQueryable<ChangeRequest> FilteredQuery(IQueryable<ChangeRequest> mine, string? search, string? status, string? department)
    {
        var query = mine;
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(department)) query = query.Where(x => x.Department == department);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.CRNumber.Contains(search) || x.Title.Contains(search) || x.Department.Contains(search));
        return query.OrderByDescending(x => x.CreatedAt);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Departments = await lookups.GetAsync("Department");
        ViewBag.Priorities = await lookups.GetAsync("Priority");
        ViewBag.Impacts = await lookups.GetAsync("Impact");
        ViewBag.ChangeReasons = await lookups.GetAsync("ChangeReason");

        return View(new ChangeRequest
    {
        Department = "",
        Priority = "",
        Impact = "",
        RequiredBy = DateTime.Today.AddDays(7),
        CreatorUserId = CurrentUserId,
        CreatorUserName = User.Identity!.Name!
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ChangeRequest model, List<IFormFile>? attachments)
    {
        model.CreatorUserId = CurrentUserId;
        model.CreatorUserName = User.Identity!.Name!;

        if (!await lookups.IsAllowedAsync("Department", model.Department))
            ModelState.AddModelError(nameof(model.Department), "Select a valid Department.");

        if (!await lookups.IsAllowedAsync("Priority", model.Priority))
            ModelState.AddModelError(nameof(model.Priority), "Select a valid Priority.");

        if (!await lookups.IsAllowedAsync("Impact", model.Impact))
            ModelState.AddModelError(nameof(model.Impact), "Select a valid Impact.");

        if (!string.IsNullOrWhiteSpace(model.ChangeReason) &&
            !await lookups.IsAllowedAsync("ChangeReason", model.ChangeReason))
            ModelState.AddModelError(nameof(model.ChangeReason), "Select a valid Change Reason.");

        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => $"{x.Key}: {e.ErrorMessage}"))
                .ToList();

            if (validationErrors.Count > 0)
                TempData["Error"] = "Please correct the highlighted mandatory fields.";

            ViewBag.Departments = await lookups.GetAsync("Department");
            ViewBag.Priorities = await lookups.GetAsync("Priority");
            ViewBag.Impacts = await lookups.GetAsync("Impact");
            ViewBag.ChangeReasons = await lookups.GetAsync("ChangeReason");
            return View(model);
        }

        var now = DateTime.Now;
        // SQL Server does not allow NEXT VALUE FOR inside the subquery shape
        // generated by EF Core SqlQueryRaw. Execute it as a scalar command instead.
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT NEXT VALUE FOR dbo.CRIdSequence;";
            var value = await command.ExecuteScalarAsync();

            if (value is null || value == DBNull.Value)
                throw new InvalidOperationException("CRIdSequence did not return a value.");

            model.CRId = Convert.ToInt64(value);
        }
        finally
        {
            await connection.CloseAsync();
        }
        model.Status = "Draft";
        model.CreatedAt = now;
        model.UpdatedAt = now;
        model.CreatedOnDate = DateOnly.FromDateTime(now);
        model.CreatedOnTime = TimeOnly.FromDateTime(now);
        model.LastUpdateDate = DateOnly.FromDateTime(now);
        model.LastUpdateOn = TimeOnly.FromDateTime(now);
        model.UpdatedByUserId = model.CreatorUserId;
        model.UpdatedByUserName = model.CreatorUserName;

        db.ChangeRequests.Add(model);
        await db.SaveChangesAsync();

        if (attachments is not null)
        {
            foreach (var file in attachments.Where(x => x is not null && x.Length > 0))
            {
                var saved = await attachmentStorage.SaveAsync(model.CRId, file);
                db.CRAttachments.Add(new CRAttachment
                {
                    ChangeRequestId = model.Id,
                    OriginalFileName = Path.GetFileName(file.FileName),
                    StoredFileName = saved.storedFileName,
                    ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                    FileSize = file.Length,
                    UploadedByUserName = model.CreatorUserName,
                    UploadedAt = DateTime.Now,
                    TransferStatus = "Pending"
                });
            }

            await db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id)
    {
        var model = await db.ChangeRequests.FindAsync(id);
        if (model is null) return NotFound();

        ViewBag.Attachments = await db.CRAttachments
            .Where(x => x.ChangeRequestId == model.Id)
            .OrderByDescending(x => x.UploadedAt)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(model.ApprovalWorkflowNo))
        {
            ViewBag.Timeline = await approvalApi.GetTimelineAsync(model.ApprovalWorkflowNo);

            var approval = await approvalApi.GetAsync(model.ApprovalWorkflowNo);
            if (approval is not null)
            {
                model.ApprovalStatus = approval.Status;
                model.ApprovalCurrentLevel = approval.CurrentLevelNo;
                model.Status = approval.Status == "Approved" ? "Completed" :
                                approval.Status == "Rejected" ? "Rejected" :
                                approval.Status == "Sent Back" ? "Sent Back" :
                                model.Status;
                var refreshTime = DateTime.Now;
                model.UpdatedAt = refreshTime;
                model.LastUpdateDate = DateOnly.FromDateTime(refreshTime);
                model.LastUpdateOn = TimeOnly.FromDateTime(refreshTime);
                model.UpdatedByUserId = model.CreatorUserId;
                model.UpdatedByUserName = model.CreatorUserName;
                await db.SaveChangesAsync();
            }
        }
        return View(model);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAttachment(long id, IFormFile file)
    {
        var model = await db.ChangeRequests.FindAsync(id);
        if (model is null) return NotFound();

        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Select a document to upload.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var saved = await attachmentStorage.SaveAsync(model.CRId, file);
        db.CRAttachments.Add(new CRAttachment
        {
            ChangeRequestId = model.Id,
            OriginalFileName = Path.GetFileName(file.FileName),
            StoredFileName = saved.storedFileName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            FileSize = file.Length,
            UploadedByUserName = model.CreatorUserName,
            UploadedAt = DateTime.Now,
            TransferStatus = string.IsNullOrWhiteSpace(model.ApprovalWorkflowNo) ? "Pending" : "Pending"
        });
        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(model.ApprovalWorkflowNo))
        {
            // Also make it available to the existing approval workflow.
            var savedAttachment = await db.CRAttachments
                .OrderByDescending(x => x.Id)
                .FirstAsync();

            var transferred = await TransferAttachmentAsync(
                model,
                savedAttachment,
                attachmentStorage);

            TempData[transferred ? "Success" : "Error"] = transferred
                ? $"{savedAttachment.OriginalFileName} transferred to the Approval work item."
                : $"Upload succeeded, but transfer to the Approval work item failed for {savedAttachment.OriginalFileName}.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetryAttachmentTransfer(long id)
    {
        var attachment = await db.CRAttachments.FindAsync(id);
        if (attachment is null)
            return NotFound();

        var model = await db.ChangeRequests.FindAsync(attachment.ChangeRequestId);
        if (model is null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(model.ApprovalWorkflowNo))
        {
            TempData["Error"] = "The change request has not been submitted for approval yet.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        if (attachment.TransferStatus == "Transferred" &&
            attachment.ApprovalAttachmentId.HasValue)
        {
            TempData["Success"] = $"{attachment.OriginalFileName} is already transferred.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        var transferred = await TransferAttachmentAsync(
            model,
            attachment,
            attachmentStorage);

        TempData[transferred ? "Success" : "Error"] = transferred
            ? $"{attachment.OriginalFileName} transferred to the Approval work item."
            : $"Transfer failed for {attachment.OriginalFileName}. Please retry.";

        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAttachment(long id)
    {
        var attachment = await db.CRAttachments.FindAsync(id);
        if (attachment is null) return NotFound();

        var cr = await db.ChangeRequests.FindAsync(attachment.ChangeRequestId);
        if (cr is null) return NotFound();

        var path = attachmentStorage.GetPath(cr.CRId, attachment.StoredFileName);
        if (!System.IO.File.Exists(path)) return NotFound();

        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(stream, attachment.ContentType, attachment.OriginalFileName);
    }

    private async Task<bool> TransferAttachmentAsync(
        ChangeRequest model,
        CRAttachment attachment,
        CRAttachmentStorage storage)
    {
        if (string.IsNullOrWhiteSpace(model.ApprovalWorkflowNo))
        {
            attachment.TransferStatus = "Failed";
            await db.SaveChangesAsync();
            return false;
        }

        var path = storage.GetPath(model.CRId, attachment.StoredFileName);
        if (!System.IO.File.Exists(path))
        {
            attachment.TransferStatus = "Failed";
            await db.SaveChangesAsync();
            return false;
        }

        try
        {
            await using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            using var form = new MultipartFormDataContent();
            form.Add(
                new StringContent(model.CreatorUserName),
                "uploadedByUserName");

            using var content = new StreamContent(stream);
            content.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(
                    string.IsNullOrWhiteSpace(attachment.ContentType)
                        ? "application/octet-stream"
                        : attachment.ContentType);

            form.Add(
                content,
                "file",
                attachment.OriginalFileName);

            var response = await approvalApi.UploadAttachmentAsync(
                model.ApprovalWorkflowNo,
                form);

            if (response is null)
            {
                attachment.TransferStatus = "Failed";
                attachment.ApprovalAttachmentId = null;
                attachment.TransferredAt = null;
                await db.SaveChangesAsync();
                return false;
            }

            attachment.TransferStatus = "Transferred";
            attachment.ApprovalAttachmentId = response.Id;
            attachment.TransferredAt = DateTime.Now;
            await db.SaveChangesAsync();
            return true;
        }
        catch
        {
            attachment.TransferStatus = "Failed";
            attachment.ApprovalAttachmentId = null;
            attachment.TransferredAt = null;
            await db.SaveChangesAsync();
            return false;
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(long id)
    {
        var model = await db.ChangeRequests.FindAsync(id);
        if (model is null) return NotFound();

        if (model.Status is "Completed" or "Rejected")
        {
            TempData["Error"] = "Closed change requests cannot be submitted.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Every field the Rule Builder can offer as a criteria FIELD (see
        // Approval's WorkflowFields catalog for ApprovalTypeId=CR) must actually be present
        // here, or a rule written against it can never match anything.
        var data = new Dictionary<string, JsonElement>
        {
            ["title"] = JsonDocument.Parse(JsonSerializer.Serialize(model.Title)).RootElement.Clone(),
            ["department"] = JsonDocument.Parse(JsonSerializer.Serialize(model.Department)).RootElement.Clone(),
            ["priority"] = JsonDocument.Parse(JsonSerializer.Serialize(model.Priority)).RootElement.Clone(),
            ["businessRequirements"] = JsonDocument.Parse(JsonSerializer.Serialize(model.BusinessRequirements)).RootElement.Clone(),
            ["requiredBy"] = JsonDocument.Parse(JsonSerializer.Serialize(model.RequiredBy?.ToString("yyyy-MM-dd"))).RootElement.Clone(),
            ["impact"] = JsonDocument.Parse(JsonSerializer.Serialize(model.Impact)).RootElement.Clone(),
            ["changeReason"] = JsonDocument.Parse(JsonSerializer.Serialize(model.ChangeReason)).RootElement.Clone(),
            ["tangibleBenefits"] = JsonDocument.Parse(JsonSerializer.Serialize(model.TangibleBenefits)).RootElement.Clone(),
            ["intangibleBenefits"] = JsonDocument.Parse(JsonSerializer.Serialize(model.IntangibleBenefits)).RootElement.Clone()
        };
        var routing = new Dictionary<string, JsonElement>(data);

        var response = await approvalApi.CreateAsync(new ApprovalCreateRequest(
            "CR",
            "JACO-CR",
            model.CRNumber,
            model.CreatorUserName,
            model.Title,
            routing,
            data));

        if (response is null)
        {
            TempData["Error"] = "Approval service could not create the workflow.";
            return RedirectToAction(nameof(Details), new { id });
        }

        model.ApprovalWorkflowNo = response.WorkflowNo;

        var crAttachments = await db.CRAttachments
            .Where(x => x.ChangeRequestId == model.Id)
            .OrderBy(x => x.Id)
            .ToListAsync();

        foreach (var attachment in crAttachments.Where(
                     x => x.TransferStatus != "Transferred" || !x.ApprovalAttachmentId.HasValue))
        {
            await TransferAttachmentAsync(
                model,
                attachment,
                attachmentStorage);
        }

        model.ApprovalStatus = response.Status;
        model.ApprovalCurrentLevel = response.CurrentLevelNo;
        model.Status = "Pending Approval";
        var submitTime = DateTime.Now;
        model.UpdatedAt = submitTime;
        model.LastUpdateDate = DateOnly.FromDateTime(submitTime);
        model.LastUpdateOn = TimeOnly.FromDateTime(submitTime);
        model.UpdatedByUserId = model.CreatorUserId;
        model.UpdatedByUserName = model.CreatorUserName;

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }
}
