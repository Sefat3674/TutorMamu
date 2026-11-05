
namespace TutorMamu.Application.DTOs.Tutors;

public class PublicTutorResponse
{
    public Guid TutorId { get; set; }

    public string TutorName { get; set; } = string.Empty;

    public string Headline { get; set; } = string.Empty;

    public string Bio { get; set; } = string.Empty;

    public decimal HourlyRate { get; set; }

    public string? Area { get; set; }

    public bool TeachesOnline { get; set; }

    public decimal AverageRating { get; set; }

    public int ReviewCount { get; set; }

    public List<string> Topics { get; set; } = new();
}
