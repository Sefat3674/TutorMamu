using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TutorMamu.Application.DTOs;
using TutorMamu.Application.DTOs.Auth;
using TutorMamu.Application.Interfaces;

namespace TutorMamu.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Register as Student or Teacher.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto) =>
        Ok(await _auth.RegisterAsync(dto));

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto) =>
        Ok(await _auth.LoginAsync(dto));

    [AllowAnonymous]
    [HttpPost("send-otp")]
    public async Task<ActionResult<SendOtpResponseDto>> SendOtp(
        [FromBody] SendOtpRequestDto dto)
    {
        return Ok(await _auth.SendOtpAsync(dto));
    }

    /// <summary>Verify OTP and authenticate the user.</summary>
    [AllowAnonymous]
    [HttpPost("verify-otp")]
    public async Task<ActionResult<AuthResponseDto>> VerifyOtp(
        [FromBody] VerifyOtpRequestDto dto)
    {
        return Ok(await _auth.VerifyOtpAsync(dto));
    }

    /// <summary>Resend OTP.</summary>
    [AllowAnonymous]
    [HttpPost("resend-otp")]
    public async Task<ActionResult<SendOtpResponseDto>> ResendOtp(
        [FromBody] ResendOtpRequestDto dto)
    {
        return Ok(await _auth.ResendOtpAsync(dto));
    }

    /// <summary>Refresh access token using refresh token.</summary>
    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken(
        [FromBody] RefreshTokenRequestDto dto)
    {
        return Ok(await _auth.RefreshTokenAsync(dto));
    }

    /// <summary>Logout and revoke the supplied refresh token.</summary>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequestDto dto)
    {
        await _auth.LogoutAsync(dto);
        return NoContent();
    }

    /// <summary>Get the authenticated user's profile.</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthenticatedUserDto>> GetCurrentUser()
    {
        var userId = GetCurrentUserId();

        return Ok(await _auth.GetCurrentUserAsync(userId));
    }

    /// <summary>Revoke all other active sessions.</summary>
    [Authorize]
    [HttpPost("revoke-other-sessions")]
    public async Task<IActionResult> RevokeOtherSessions(
        [FromBody] RevokeSessionsRequestDto dto)
    {
        var userId = GetCurrentUserId();

        await _auth.RevokeOtherSessionsAsync(userId, dto);

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            throw new UnauthorizedAccessException(
                "Invalid or missing authenticated user ID.");
        }

        return parsedUserId;
    }
}
