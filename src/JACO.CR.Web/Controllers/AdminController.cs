using JACO.CR.Web.Data;
using JACO.CR.Web.Models;
using JACO.CR.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JACO.CR.Web.Controllers;

[Authorize(Policy = "CRAdmin")]
public sealed class AdminController(CrDbContext db, CRLookupService lookups) : Controller
{
    public static readonly string[] SupportedTypes =
    [
        "Department",
        "Priority",
        "Impact",
        "ChangeReason"
    ];

    // Landing page for the "Administration" nav link -- a hub of subpages (Lookup Values,
    // All Change Requests) rather than jumping straight into one of them, the same pattern
    // Approval's own Admin/Index uses for its subpages (Rule Builder, PPF Monitor, etc.).
    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> LookupValues(string? type = null)
    {
        type ??= "Department";

        if (!SupportedTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            type = "Department";

        ViewBag.LookupTypes = SupportedTypes;

        var values = await db.CRLookupValues
            .Where(x => x.LookupType == type)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayText)
            .ToListAsync();

        ViewBag.SelectedType = type;
        return View(values);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string lookupType, string value, string displayText, int sortOrder = 10)
    {
        if (!SupportedTypes.Contains(lookupType, StringComparer.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Invalid lookup type.";
            return RedirectToAction(nameof(LookupValues));
        }

        value = value.Trim();
        displayText = displayText.Trim();

        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(displayText))
        {
            TempData["Error"] = "Value and Display Text are required.";
            return RedirectToAction(nameof(LookupValues), new { type = lookupType });
        }

        if (await db.CRLookupValues.AnyAsync(x =>
            x.LookupType == lookupType && x.Value == value))
        {
            TempData["Error"] = "This lookup value already exists.";
            return RedirectToAction(nameof(LookupValues), new { type = lookupType });
        }

        db.CRLookupValues.Add(new CRLookupValue
        {
            LookupType = lookupType,
            Value = value,
            DisplayText = displayText,
            SortOrder = sortOrder,
            Active = true
        });

        await db.SaveChangesAsync();
        TempData["Success"] = "Lookup value added.";
        return RedirectToAction(nameof(LookupValues), new { type = lookupType });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var item = await db.CRLookupValues.FindAsync(id);
        if (item is null) return NotFound();

        item.Active = !item.Active;
        await db.SaveChangesAsync();

        TempData["Success"] = item.Active
            ? "Lookup value activated."
            : "Lookup value deactivated.";

        return RedirectToAction(nameof(LookupValues), new { type = item.LookupType });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string displayText, int sortOrder)
    {
        var item = await db.CRLookupValues.FindAsync(id);
        if (item is null) return NotFound();

        displayText = displayText.Trim();
        if (string.IsNullOrWhiteSpace(displayText))
        {
            TempData["Error"] = "Display Text is required.";
            return RedirectToAction(nameof(LookupValues), new { type = item.LookupType });
        }

        item.DisplayText = displayText;
        item.SortOrder = sortOrder;

        await db.SaveChangesAsync();
        TempData["Success"] = "Lookup value updated.";
        return RedirectToAction(nameof(LookupValues), new { type = item.LookupType });
    }

    // A CR Administrator can see every change request in the system, not just their own --
    // same list/filter/sort/export behavior as ChangeRequest/Index (via the shared
    // FilteredQuery), just sourced from every creator instead of the current user.
    [HttpGet]
    public async Task<IActionResult> AllRequests(string? search, string? status, string? department, string? sort, string dir = "asc")
    {
        var all = db.ChangeRequests.AsNoTracking();

        var model = new ChangeRequestListViewModel
        {
            TotalCount = await all.CountAsync(),
            DraftCount = await all.CountAsync(x => x.Status == "Draft"),
            PendingApprovalCount = await all.CountAsync(x => x.Status == "Pending Approval"),
            CompletedCount = await all.CountAsync(x => x.Status == "Completed"),
            Search = search,
            Status = status,
            Department = department,
            Sort = sort,
            Dir = dir,
            Departments = await lookups.GetAsync("Department"),
            BasePath = "/Admin/AllRequests",
            ExportPath = "/Admin/ExportAllRequests",
            IsAdminView = true
        };

        model.Rows = await ChangeRequestController.FilteredQuery(all, search, status, department, sort, dir).ToListAsync();
        return View("~/Views/ChangeRequest/Index.cshtml", model);
    }

    [HttpGet]
    public async Task<IActionResult> ExportAllRequests(string? search, string? status, string? department, string? sort, string dir = "asc")
    {
        var rows = await ChangeRequestController.FilteredQuery(db.ChangeRequests.AsNoTracking(), search, status, department, sort, dir).ToListAsync();
        var bytes = CsvHelper.ToCsvBytes(rows,
            ["CR No.", "Title", "Department", "Priority", "Status", "Created By", "Created On"],
            x => [x.CRNumber, x.Title, x.Department, x.Priority, x.Status, x.CreatorUserName, x.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")]);
        return File(bytes, "text/csv", $"all-change-requests-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
    }
}
