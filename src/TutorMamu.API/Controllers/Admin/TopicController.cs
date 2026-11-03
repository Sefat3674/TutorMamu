using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorMamu.Application.DTOs;
using TutorMamu.Application.Interfaces;
using TutorMamu.Domain.Entities;

namespace TutorMamu.API.Controllers.Admin;

[ApiController]
[Route("api/topics")]
public class TopicController : ControllerBase
{
    private readonly ITopicService _topics;

    public TopicController(ITopicService topics) => _topics = topics;

    /// <summary>Public topic tree (flat list with ParentId).</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<TopicDto>>> List() => Ok(await _topics.ListAsync());

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<TopicDto>> Create(CreateTopicDto dto) =>
        Ok(await _topics.CreateAsync(dto));
}
