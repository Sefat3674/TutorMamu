
using System.ComponentModel.DataAnnotations;

namespace TutorMamu.Application.DTOs.Users;

public class UpdateProfileDto
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(20)]
    public string? Gender { get; set; }

    [MaxLength(100)]
    public string? Division { get; set; }

    [MaxLength(100)]
    public string? District { get; set; }

    [MaxLength(100)]
    public string? Area { get; set; }

    [MaxLength(300)]
    public string? AddressDetails { get; set; }
}
