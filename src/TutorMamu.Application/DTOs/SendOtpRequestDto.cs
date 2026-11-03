using System.ComponentModel.DataAnnotations;

namespace TutorMamu.Application.DTOs.Auth;

public class SendOtpRequestDto
{
    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    public string Purpose { get; set; } = "LOGIN";
}