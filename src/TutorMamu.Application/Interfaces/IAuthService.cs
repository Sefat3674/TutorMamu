
using TutorMamu.Application.DTOs;
using TutorMamu.Application.DTOs.Auth;

namespace TutorMamu.Application.Interfaces;

public interface IAuthService
{
    // Email and password authentication
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);

    // Phone OTP authentication
    Task<SendOtpResponseDto> SendOtpAsync(SendOtpRequestDto dto);
    Task<AuthResponseDto> VerifyOtpAsync(VerifyOtpRequestDto dto);
    Task<SendOtpResponseDto> ResendOtpAsync(ResendOtpRequestDto dto);

    // Token and session management
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto);
    Task LogoutAsync(LogoutRequestDto dto);
    Task<AuthenticatedUserDto> GetCurrentUserAsync(Guid userId);
    Task RevokeOtherSessionsAsync(
        Guid userId,
        RevokeSessionsRequestDto dto);
}
