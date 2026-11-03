namespace TutorMamu.Application.DTOs.Auth;

public class SendOtpResponseDto
{
    public string VerificationId { get; set; } = string.Empty;
    public string MaskedPhone { get; set; } = string.Empty;
    public int ExpiresIn { get; set; } = 300;
    public int ResendAvailableIn { get; set; } = 60;
}