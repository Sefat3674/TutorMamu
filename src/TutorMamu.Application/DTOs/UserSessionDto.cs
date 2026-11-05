
namespace TutorMamu.Application.DTOs.Users;

public class UserSessionDto
{
    public Guid Id { get; set; }

    public string? DeviceId { get; set; }

    public string? DeviceName { get; set; }

    public string? Platform { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }
}
