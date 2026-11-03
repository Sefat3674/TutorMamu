using System.ComponentModel.DataAnnotations;

namespace TutorMamu.Application.DTOs.Auth;

public class VerifyOtpRequestDto
{
    [Required]
    public string VerificationId { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Otp { get; set; } = string.Empty;

    public DeviceDto Device { get; set; } = new();
}

public class DeviceDto
{
    public string? DeviceId { get; set; }
    public string? Platform { get; set; }
    public string? DeviceName { get; set; }
}