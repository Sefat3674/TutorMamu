using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorMamu.Application.Common;
using TutorMamu.Application.DTOs;
using TutorMamu.Application.Interfaces;
using TutorMamu.Domain.Entities;
using TutorMamu.Domain.Enums;

namespace TutorMamu.API.Controllers.Admin;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin/tutors")]
public class TutorVerificationController : ControllerBase
{
    private readonly ITutorProfileService _tutors;

    public TutorVerificationController(ITutorProfileService tutors) => _tutors = tutors;

    /// <summary>List tutors by status (default Pending) for the verification queue.</summary>
    [HttpGet]
    public async Task<ActionResult<List<TutorProfileDto>>> List([FromQuery] string status = "Pending") =>
        Ok(await _tutors.ListByStatusAsync(ParseStatus(status)));

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<TutorProfileDto>> SetStatus(Guid id, SetVerificationDto dto) =>
        Ok(await _tutors.SetVerificationAsync(id, ParseStatus(dto.Status)));

    private static VerificationStatus ParseStatus(string value) =>
        Enum.TryParse<VerificationStatus>(value, true, out var status)
            ? status
            : throw new AppException("Status must be Pending, Approved, Rejected or Suspended.");
}
