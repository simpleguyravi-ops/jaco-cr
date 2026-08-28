using System.Net.Http.Json;
using System.Text.Json;

namespace JACO.CR.Web.Services;

public sealed record ApprovalCreateRequest(
    string ApprovalType,
    string SourceSystem,
    string SourceReference,
    string CreatorUserName,
    string? Subject,
    Dictionary<string, JsonElement>? RoutingContext,
    Dictionary<string, JsonElement>? DecisionData);

public sealed record ApprovalAttachmentResponse(
    long Id,
    string FileName,
    string ContentType,
    long FileSize,
    string UploadedByUserName,
    DateTime UploadedAt);

public sealed record ApprovalCreateResponse(
    string WorkflowNo,
    string Status,
    int? CurrentLevelNo,
    int? RoutingRuleId,
    string? SourceReference);

public sealed record ApprovalWorkflowResponse(
    long Id,
    string WorkflowNo,
    int ApprovalTypeId,
    int? WorkflowVersionId,
    int? RoutingRuleId,
    int CreatorUserId,
    string Status,
    int? CurrentLevelNo,
    string? SourceReference,
    string? Subject,
    string? DataJson,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record TimelineDecision(string ActorName, string ActionCode, string? Comments, DateTime AtUtc);
public sealed record TimelineLevel(int LevelNo, string Mode, List<string> ApproverNames, List<TimelineDecision> Decisions, string LevelStatus);

public sealed class ApprovalApiClient(HttpClient http, IConfiguration config)
{
    private string BaseUrl =>
        config["ApprovalApi:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5001";

    public async Task<ApprovalCreateResponse?> CreateAsync(ApprovalCreateRequest request, CancellationToken ct = default)
    {
        using var response = await http.PostAsJsonAsync($"{BaseUrl}/api/approvals", request, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ApprovalCreateResponse>(cancellationToken: ct);
    }

    public async Task<ApprovalAttachmentResponse?> UploadAttachmentAsync(string workflowNo, HttpContent form, CancellationToken ct = default)
    {
        using var response = await http.PostAsync($"{BaseUrl}/api/approvals/{Uri.EscapeDataString(workflowNo)}/attachments", form, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ApprovalAttachmentResponse>(cancellationToken: ct);
    }

    public async Task<ApprovalWorkflowResponse?> GetAsync(string workflowNo, CancellationToken ct = default)
    {
        using var response = await http.GetAsync($"{BaseUrl}/api/approvals/{Uri.EscapeDataString(workflowNo)}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ApprovalWorkflowResponse>(cancellationToken: ct);
    }

    public async Task<List<TimelineLevel>?> GetTimelineAsync(string workflowNo, CancellationToken ct = default)
    {
        using var response = await http.GetAsync($"{BaseUrl}/api/approvals/{Uri.EscapeDataString(workflowNo)}/timeline", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<List<TimelineLevel>>(cancellationToken: ct);
    }
}
