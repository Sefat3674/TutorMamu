
using TutorMamu.Domain.Enums;

namespace TutorMamu.Domain.Entities;

public class TutorDocument : BaseEntity
{
    public Guid TutorProfileId { get; set; }
    public TutorProfile TutorProfile { get; set; } = null!;

    public TutorDocumentType DocumentType { get; set; }

    public DocumentSide DocumentSide { get; set; }
        = DocumentSide.NOT_APPLICABLE;

    public string OriginalFileName { get; set; } = string.Empty;

    public string StoredFileName { get; set; } = string.Empty;

    // Private server-side storage path.
    public string StoragePath { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public DocumentStatus Status { get; set; }
        = DocumentStatus.PendingReview;

    public string? RejectionReason { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }
}
