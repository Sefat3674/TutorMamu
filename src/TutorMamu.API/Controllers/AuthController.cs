using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorMamu.Application.DTOs;
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
}
