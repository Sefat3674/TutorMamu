using System.ComponentModel.DataAnnotations;

namespace TutorMamu.Application.DTOs.Auth;

public class ResendOtpRequestDto
{
    [Required]
    public string VerificationId { get; set; } = string.Empty;
}