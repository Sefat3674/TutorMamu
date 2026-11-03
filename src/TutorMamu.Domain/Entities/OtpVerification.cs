using System.ComponentModel.DataAnnotations;

namespace TutorMamu.Domain.Entities;

public class OtpVerification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string VerificationId { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string OtpHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Purpose { get; set; } = "LOGIN";

    public DateTime ExpiresAt { get; set; }

    public DateTime ResendAvailableAt { get; set; }

    public int Attempts { get; set; } = 0;

    public bool IsVerified { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}