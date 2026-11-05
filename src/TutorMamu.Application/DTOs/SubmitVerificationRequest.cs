
using System.ComponentModel.DataAnnotations;

namespace TutorMamu.Application.DTOs.Tutors;

public class SubmitVerificationRequest
{
    [Required]
    [MinLength(1)]
    public List<Guid> DocumentIds { get; set; } = new();
}
