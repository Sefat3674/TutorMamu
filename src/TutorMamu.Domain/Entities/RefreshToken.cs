using System.ComponentModel.DataAnnotations;

namespace TutorMamu.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Match ApplicationUser.Id
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(500)]
    public string TokenHash { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? DeviceId { get; set; }

    [MaxLength(255)]
    public string? DeviceName { get; set; }

    [MaxLength(30)]
    public string? Platform { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RevokedAt { get; set; }

    [MaxLength(500)]
    public string? ReplacedByTokenHash { get; set; }

    public ApplicationUser? User { get; set; }
}