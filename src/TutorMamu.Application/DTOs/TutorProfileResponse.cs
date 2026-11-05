
namespace TutorMamu.Application.DTOs.Tutors;

public class TutorProfileResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string? TutorName { get; set; }

    public string Headline { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;

    public decimal HourlyRate { get; set; }

    public string? Area { get; set; }

    public bool TeachesOnline { get; set; }

    public string VerificationStatus { get; set; } = string.Empty;

    public decimal AverageRating { get; set; }

    public int ReviewCount { get; set; }

    public List<Guid> TopicIds { get; set; } = new();
}
