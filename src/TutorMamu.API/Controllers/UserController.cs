
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TutorMamu.Application.DTOs.Users;
using TutorMamu.Application.Interfaces;

namespace TutorMamu.API.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    // GET: api/users/me
    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userId = GetCurrentUserId();

            var profile = await _userService.GetProfileAsync(userId);

            return Ok(new
            {
                success = true,
                message = "Profile retrieved successfully.",
                data = profile
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

    // PUT: api/users/me
    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();

            var profile = await _userService.UpdateProfileAsync(
                userId, dto);

            return Ok(new
            {
                success = true,
                message = "Profile updated successfully.",
                data = profile
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

    // POST: api/users/me/profile-image
    [HttpPost("me/profile-image")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadProfileImage(
        IFormFile file)
    {
        try
        {
            var userId = GetCurrentUserId();

            if (file == null || file.Length == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Please select an image."
                });
            }

            var imageUrl = await _userService.UploadProfileImageAsync(
                userId, file);

            return Ok(new
            {
                success = true,
                message = "Profile image uploaded successfully.",
                data = new
                {
                    profileImageUrl = imageUrl
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

    // POST: api/users/me/deletion-request
    [HttpPost("me/deletion-request")]
    public async Task<IActionResult> RequestAccountDeletion()
    {
        try
        {
            var userId = GetCurrentUserId();

            await _userService.RequestAccountDeletionAsync(userId);

            return Ok(new
            {
                success = true,
                message = "Account deletion request submitted successfully."
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

    // GET: api/users/me/sessions
    [HttpGet("me/sessions")]
    public async Task<IActionResult> GetSessions()
    {
        try
        {
            var userId = GetCurrentUserId();

            var sessions = await _userService.GetSessionsAsync(userId);

            return Ok(new
            {
                success = true,
                message = "Sessions retrieved successfully.",
                data = sessions
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
