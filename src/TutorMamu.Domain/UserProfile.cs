using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TutorMamu.Domain.Entities;

public class UserProfile
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // One-to-one relationship with ApplicationUser
    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    // Personal information
    public DateTime? DateOfBirth { get; set; }

    [MaxLength(20)]
    public string? Gender { get; set; }

    // Address information
    [MaxLength(100)]
    public string? Division { get; set; }

    [MaxLength(100)]
    public string? District { get; set; }

    [MaxLength(100)]
    public string? Area { get; set; }

    [MaxLength(300)]
    public string? AddressDetails { get; set; }

    // Profile image
    [MaxLength(500)]
    public string? ProfileImageUrl { get; set; }

    // Account deletion
    public DateTime? DeletionRequestedAt { get; set; }

    // Audit information
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}