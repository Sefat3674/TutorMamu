
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorMamu.Application.DTOs.Tutors;
using TutorMamu.Application.Interfaces;

namespace TutorMamu.API.Controllers;

[ApiController]
[Route("api/tutors")]
[Authorize(Roles = "Teacher")]
public class TutorsController : ControllerBase
{
    private readonly ITutorService _tutorService;

    public TutorsController(ITutorService tutorService)
    {
        _tutorService = tutorService;
    }

    private Guid GetUserId()
    {
        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userId, out var id))
            throw new UnauthorizedAccessException(
                "Invalid authentication.");

        return id;
    }

    private IActionResult ErrorResponse(Exception ex)
    {
        return ex switch
        {
            KeyNotFoundException =>
                NotFound(new { success = false, message = ex.Message }),

            InvalidOperationException =>
                Conflict(new { success = false, message = ex.Message }),

            ArgumentException =>
                BadRequest(new { success = false, message = ex.Message }),

            UnauthorizedAccessException =>
                Unauthorized(new { success = false, message = ex.Message }),

            _ => throw ex
        };
    }

    // =====================================================
    // 1. CREATE PROFILE
    // POST: api/tutors/profile
    // =====================================================

    [HttpPost("profile")]
    public async Task<IActionResult> CreateProfile(
        [FromBody] CreateTutorProfileRequest request)
    {
        try
        {
            var result = await _tutorService.CreateProfileAsync(
                GetUserId(), request);

            return CreatedAtAction(
                nameof(GetMyProfile),
                null,
                new
                {
                    success = true,
                    message = "Tutor profile created successfully.",
                    data = result
                });
        }
        catch (Exception ex) when (
            ex is KeyNotFoundException ||
            ex is InvalidOperationException ||
            ex is ArgumentException ||
            ex is UnauthorizedAccessException)
        {
            return ErrorResponse(ex);
        }
    }

    // =====================================================
    // 2. GET MY PROFILE
    // GET: api/tutors/me
    // =====================================================

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var result = await _tutorService.GetMyProfileAsync(
            GetUserId());

        if (result == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Tutor profile not found."
            });
        }

        return Ok(new
        {
            success = true,
            data = result
        });
    }

    // =====================================================
    // 3. UPDATE MY PROFILE
    // PUT: api/tutors/me
    // =====================================================

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateTutorProfileRequest request)
    {
        try
        {
            var result = await _tutorService.UpdateProfileAsync(
                GetUserId(), request);

            if (result == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Tutor profile not found."
                });
            }

            return Ok(new
            {
                success = true,
                message = "Tutor profile updated successfully.",
                data = result
            });
        }
        catch (Exception ex) when (ex is ArgumentException)
        {
            return ErrorResponse(ex);
        }
    }

    // =====================================================
    // 4. UPLOAD DOCUMENT
    // POST: api/tutors/me/documents
    // =====================================================

    [HttpPost("me/documents")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadDocument(
        [FromForm] UploadTutorDocumentRequest request)
    {
        try
        {
            var result = await _tutorService.UploadDocumentAsync(
                GetUserId(), request);

            return Ok(new
            {
                success = true,
                message = "Document uploaded successfully.",
                data = result
            });
        }
        catch (Exception ex) when (
            ex is KeyNotFoundException ||
            ex is InvalidOperationException ||
            ex is ArgumentException)
        {
            return ErrorResponse(ex);
        }
    }

    // =====================================================
    // 5. GET MY DOCUMENTS
    // GET: api/tutors/me/documents
    // =====================================================

    [HttpGet("me/documents")]
    public async Task<IActionResult> GetMyDocuments()
    {
        try
        {
            var result = await _tutorService.GetMyDocumentsAsync(
                GetUserId());

            return Ok(new
            {
                success = true,
                data = result
            });
        }
        catch (KeyNotFoundException ex)
        {
            return ErrorResponse(ex);
        }
    }

    // =====================================================
    // 6. DELETE DOCUMENT
    // DELETE: api/tutors/me/documents/{id}
    // =====================================================

    [HttpDelete("me/documents/{id:guid}")]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        try
        {
            var deleted = await _tutorService.DeleteDocumentAsync(
                GetUserId(), id);

            if (!deleted)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Document not found."
                });
            }

            return Ok(new
            {
                success = true,
                message = "Document deleted successfully."
            });
        }
        catch (InvalidOperationException ex)
        {
            return ErrorResponse(ex);
        }
    }

    // =====================================================
    // 7. GET VERIFICATION STATUS
    // GET: api/tutors/me/verification
    // =====================================================

    [HttpGet("me/verification")]
    public async Task<IActionResult> GetVerificationStatus()
    {
        try
        {
            var result = await _tutorService
                .GetVerificationStatusAsync(GetUserId());

            return Ok(new
            {
                success = true,
                data = result
            });
        }
        catch (KeyNotFoundException ex)
        {
            return ErrorResponse(ex);
        }
    }

    // =====================================================
    // 8. SUBMIT VERIFICATION
    // POST: api/tutors/me/submit-verification
    // =====================================================

    [HttpPost("me/submit-verification")]
    public async Task<IActionResult> SubmitVerification(
        [FromBody] SubmitVerificationRequest request)
    {
        try
        {
            var result = await _tutorService
                .SubmitVerificationAsync(GetUserId(), request);

            return Ok(new
            {
                success = true,
                message = "Verification submitted successfully.",
                data = result
            });
        }
        catch (Exception ex) when (
            ex is KeyNotFoundException ||
            ex is InvalidOperationException ||
            ex is ArgumentException)
        {
            return ErrorResponse(ex);
        }
    }

    // =====================================================
    // 9. PUBLIC TUTOR DETAILS
    // GET: api/tutors/{tutorId}
    // =====================================================

    [AllowAnonymous]
    [HttpGet("{tutorId:guid}")]
    public async Task<IActionResult> GetPublicTutor(Guid tutorId)
    {
        var result = await _tutorService.GetPublicTutorAsync(
            tutorId);

        if (result == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Verified tutor not found."
            });
        }

        return Ok(new
        {
            success = true,
            data = result
        });
    }
}
