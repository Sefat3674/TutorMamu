using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorMamu.API.Extensions;
using TutorMamu.Application.DTOs;
using TutorMamu.Application.Interfaces;
using TutorMamu.Domain.Entities;

namespace TutorMamu.API.Controllers.Teacher;

[ApiController]
[Authorize(Roles = Roles.Teacher)]
[Route("api/teacher/bookings")]
public class TeacherBookingController : ControllerBase
{
    private readonly IBookingService _bookings;

    public TeacherBookingController(IBookingService bookings) => _bookings = bookings;

    /// <summary>Incoming requests (paid bookings) and upcoming classes.</summary>
    [HttpGet]
    public async Task<ActionResult<List<BookingDto>>> Incoming() =>
        Ok(await _bookings.GetTutorBookingsAsync(User.GetUserId()));

    /// <summary>Accept (with meeting link) or decline a paid request before its deadline.</summary>
    [HttpPost("{id:guid}/respond")]
    public async Task<ActionResult<BookingDto>> Respond(Guid id, RespondBookingDto dto) =>
        Ok(await _bookings.RespondAsync(User.GetUserId(), id, dto));
}
