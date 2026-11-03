using System.ComponentModel.DataAnnotations;

namespace TutorMamu.Application.DTOs.Auth;

public class RevokeSessionsRequestDto
{
    [Required]
    public string CurrentRefreshToken { get; set; } = string.Empty;
}