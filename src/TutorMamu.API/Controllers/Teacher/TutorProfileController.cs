using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorMamu.API.Extensions;
using TutorMamu.Application.DTOs;
using TutorMamu.Application.Interfaces;
using TutorMamu.Domain.Entities;

namespace TutorMamu.API.Controllers.Teacher;

[ApiController]
[Authorize(Roles = Roles.Teacher)]
[Route("api/teacher/profile")]
public class TutorProfileController : ControllerBase
{
    private readonly ITutorProfileService _profiles;

    public TutorProfileController(ITutorProfileService profiles) => _profiles = profiles;

    [HttpGet]
    public async Task<ActionResult<TutorProfileDto>> Get() =>
        Ok(await _profiles.GetMineAsync(User.GetUserId()));

    /// <summary>Update bio, hourly rate, area and the topics this tutor teaches.</summary>
    [HttpPut]
    public async Task<ActionResult<TutorProfileDto>> Update(UpdateTutorProfileDto dto) =>
        Ok(await _profiles.UpdateMineAsync(User.GetUserId(), dto));
}
