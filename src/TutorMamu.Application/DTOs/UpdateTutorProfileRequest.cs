
using System.ComponentModel.DataAnnotations;

namespace TutorMamu.Application.DTOs.Tutors;

public class UpdateTutorProfileRequest
{
    [Required]
    [MaxLength(200)]
    public string Headline { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Bio { get; set; } = string.Empty;

    [Range(1, 100000)]
    public decimal HourlyRate { get; set; }

    [MaxLength(100)]
    public string? Area { get; set; }

    public bool TeachesOnline { get; set; }

    public List<Guid> TopicIds { get; set; } = new();
}
