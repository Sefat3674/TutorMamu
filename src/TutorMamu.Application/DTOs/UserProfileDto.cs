
namespace TutorMamu.Application.DTOs.Users;

public class UserProfileDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? Division { get; set; }

    public string? District { get; set; }

    public string? Area { get; set; }

    public string? AddressDetails { get; set; }

    public string? ProfileImageUrl { get; set; }

    public List<string> Roles { get; set; } = new();

    public bool IsProfileComplete { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
