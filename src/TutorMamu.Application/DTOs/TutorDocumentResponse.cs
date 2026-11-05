
namespace TutorMamu.Application.DTOs.Tutors;

public class TutorDocumentResponse
{
    public Guid Id { get; set; }

    public string DocumentType { get; set; } = string.Empty;

    public string DocumentSide { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? RejectionReason { get; set; }

    public DateTime UploadedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }
}
