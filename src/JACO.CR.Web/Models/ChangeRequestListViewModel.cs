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
    public string? Sort { get; set; }
    public string Dir { get; set; } = "asc";

    public IReadOnlyList<CRLookupValue> Departments { get; set; } = new List<CRLookupValue>();
    public List<ChangeRequest> Rows { get; set; } = new();

    // Lets this same view double as the CR Administrator's "All Change Requests" screen
    // (Admin/AllRequests) as well as a user's own list (ChangeRequest/Index) -- BasePath/
    // ExportPath drive every link on the page, IsAdminView swaps the header text, hides
    // "+ New Change Request", and shows who created each row.
    public string BasePath { get; set; } = "/ChangeRequest";
    public string ExportPath { get; set; } = "/ChangeRequest/Export";
    public bool IsAdminView { get; set; }

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
