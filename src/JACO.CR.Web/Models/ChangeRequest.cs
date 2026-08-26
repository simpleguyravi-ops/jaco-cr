using System.ComponentModel.DataAnnotations;

namespace JACO.CR.Web.Models;

public sealed class ChangeRequest
{
    public long Id { get; set; }
    public long CRId { get; set; }
    public string CRNumber { get; private set; } = "";
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [Required, StringLength(50)] public string Department { get; set; } = "";
    [Required, StringLength(30)] public string Priority { get; set; } = "";
    [Required] public string BusinessRequirements { get; set; } = "";
    [Required] public DateTime? RequiredBy { get; set; }
    [Required, StringLength(30)] public string Impact { get; set; } = "";
    [Required, StringLength(80)] public string ChangeReason { get; set; } = "";
    [Required] public string TangibleBenefits { get; set; } = "";
    [Required] public string IntangibleBenefits { get; set; } = "";

    public int CreatorUserId { get; set; }
    public string CreatorUserName { get; set; } = "";

    // Background/system metadata
    public DateOnly CreatedOnDate { get; set; }
    public TimeOnly CreatedOnTime { get; set; }
    public DateOnly LastUpdateDate { get; set; }
    public TimeOnly LastUpdateOn { get; set; }
    public int? UpdatedByUserId { get; set; }
    public string? UpdatedByUserName { get; set; }


    public string Status { get; set; } = "Draft";
    public string? ApprovalWorkflowNo { get; set; }
    public string? ApprovalStatus { get; set; }
    [StringLength(50)] public string? SAPReferenceId { get; set; }
    public int? ApprovalCurrentLevel { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}




public sealed class CRAttachment
{
    public long Id { get; set; }
    public long ChangeRequestId { get; set; }
    public string OriginalFileName { get; set; } = "";
    public string StoredFileName { get; set; } = "";
    public string ContentType { get; set; } = "application/octet-stream";
    public long FileSize { get; set; }
    public string UploadedByUserName { get; set; } = "";
    public DateTime UploadedAt { get; set; }
    public string TransferStatus { get; set; } = "Pending";
    public long? ApprovalAttachmentId { get; set; }
    public DateTime? TransferredAt { get; set; }
}
