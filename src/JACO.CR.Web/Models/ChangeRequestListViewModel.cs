namespace JACO.CR.Web.Models;

public class ChangeRequestListViewModel
{
    public int TotalCount { get; set; }
    public int DraftCount { get; set; }
    public int PendingApprovalCount { get; set; }
    public int CompletedCount { get; set; }

    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Department { get; set; }

    public IReadOnlyList<CRLookupValue> Departments { get; set; } = new List<CRLookupValue>();
    public List<ChangeRequest> Rows { get; set; } = new();

    public static readonly (string Value, string Label)[] StatusTabs =
    [
        ("", "All"),
        ("Draft", "Draft"),
        ("Pending Approval", "Pending"),
        ("Completed", "Approved"),
        ("Rejected", "Rejected"),
        ("Sent Back", "Sent Back"),
        ("Withdrawn", "Withdrawn"),
    ];
}
