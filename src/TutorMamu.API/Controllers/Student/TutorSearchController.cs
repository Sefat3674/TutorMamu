using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorMamu.Application.DTOs;
using TutorMamu.Application.Interfaces;

namespace TutorMamu.API.Controllers.Student;

[ApiController]
[AllowAnonymous]
[Route("api/tutors")]
public class TutorSearchController : ControllerBase
{
    private readonly ITutorProfileService _tutors;

    public TutorSearchController(ITutorProfileService tutors) => _tutors = tutors;

    /// <summary>Approved tutors, optionally filtered by topic (e.g. Integration).</summary>
    [HttpGet]
    public async Task<ActionResult<List<TutorProfileDto>>> Search([FromQuery] Guid? topicId) =>
        Ok(await _tutors.SearchAsync(topicId));
}
