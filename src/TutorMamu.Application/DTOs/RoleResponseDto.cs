
namespace TutorMamu.Application.DTOs.Users;

public class RoleResponseDto
{
    public string Role { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string? VerificationStatus { get; set; }
}

public class UserRolesResponseDto
{
    public List<RoleResponseDto> Roles { get; set; } = new();
    public string ActiveRole { get; set; } = "STUDENT";
}
