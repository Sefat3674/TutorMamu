
namespace TutorMamu.Application.DTOs.Tutors;

public class VerificationResponse
{
    public bool PhoneVerified { get; set; }

    public string NidStatus { get; set; } = "NOT_SUBMITTED";

    public string EducationStatus { get; set; } = "NOT_SUBMITTED";

    public string OverallStatus { get; set; } = "NOT_SUBMITTED";

    public string? RejectionReason { get; set; }

    public DateTime? SubmittedAt { get; set; }
}
