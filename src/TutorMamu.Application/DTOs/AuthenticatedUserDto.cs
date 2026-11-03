namespace TutorMamu.Application.DTOs.Auth;

public class AuthenticatedUserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public string ActiveRole { get; set; } = string.Empty;
    public bool IsProfileComplete { get; set; }
    public bool PhoneVerified { get; set; }
    public string? ProfileImage { get; set; }
}