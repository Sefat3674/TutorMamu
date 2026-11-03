namespace TutorMamu.Application.DTOs;

public record RegisterDto(
    string FullName,
    string Email,
    string PhoneNumber,
    string Password,
    string Role);

public record LoginDto(string Email, string Password);

public record AuthResponseDto(
    string Token,
    DateTime ExpiresAt,
    Guid UserId,
    string Email,
    string FullName,
    IList<string> Roles)
{
    public string RefreshToken { get; set; } = string.Empty;
}