
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorMamu.Application.DTOs.Users;
using TutorMamu.Application.Interfaces;

namespace TutorMamu.API.Controllers;

[ApiController]
[Authorize]
[Route("api/users/me")]
public class UserRolesController : ControllerBase
{
    private readonly IUserService _userService;

    public UserRolesController(IUserService userService)
    {
        _userService = userService;
    }

    // GET: api/users/me/roles
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        try
        {
            var userId = GetCurrentUserId();

            var roles = await _userService.GetRolesAsync(userId);

            return Ok(new
            {
                success = true,
                message = "Roles retrieved successfully.",
                data = roles
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    // POST: api/users/me/roles
    [HttpPost("roles")]
    public async Task<IActionResult> ActivateRole(
        [FromBody] ActivateRoleRequestDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();

            var result = await _userService.ActivateRoleAsync(
                userId, dto);

            return Ok(new
            {
                success = true,
                message = "Teacher registration started.",
                data = result
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    // PUT: api/users/me/active-role
    [HttpPut("active-role")]
    public async Task<IActionResult> SwitchActiveRole(
        [FromBody] SwitchActiveRoleRequestDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();

            var activeRole = await _userService.SwitchActiveRoleAsync(
                userId, dto);

            return Ok(new
            {
                success = true,
                message = "Active role switched successfully.",
                data = new
                {
                    activeRole
                }
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    // Extract authenticated user ID from JWT
    private Guid GetCurrentUserId()
    {
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new UnauthorizedAccessException(
                "Invalid or missing user identity.");
        }

        return userId;
    }
}
