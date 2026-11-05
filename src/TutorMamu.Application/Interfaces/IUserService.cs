
using TutorMamu.Application.DTOs.Users;
using Microsoft.AspNetCore.Http;

namespace TutorMamu.Application.Interfaces;

public interface IUserService
{
    Task<UserProfileDto> GetProfileAsync(Guid userId);

    Task<UserProfileDto> UpdateProfileAsync(
        Guid userId,
        UpdateProfileDto dto);

    Task<string> UploadProfileImageAsync(
        Guid userId,
        IFormFile file);

    Task RequestAccountDeletionAsync(Guid userId);

    Task<List<UserSessionDto>> GetSessionsAsync(Guid userId);
}
