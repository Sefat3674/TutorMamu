using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorMamu.API.Extensions;
using TutorMamu.Application.DTOs;
using TutorMamu.Application.Interfaces;
using TutorMamu.Domain.Entities;

namespace TutorMamu.API.Controllers.Student;

[ApiController]
[Authorize(Roles = Roles.Student)]
[Route("api/student/bookings")]
public class StudentBookingController : ControllerBase
{
    private readonly IBookingService _bookings;

    public StudentBookingController(IBookingService bookings) => _bookings = bookings;

    /// <summary>Request a 1h, 2h or 3h class. Price is calculated automatically.</summary>
    [HttpPost]
    public async Task<ActionResult<BookingDto>> Create(CreateBookingDto dto) =>
        Ok(await _bookings.CreateAsync(User.GetUserId(), dto));

    [HttpGet]
    public async Task<ActionResult<List<BookingDto>>> Mine() =>
        Ok(await _bookings.GetStudentBookingsAsync(User.GetUserId()));

    /// <summary>
    /// Template shortcut that marks the booking paid. In production, call BookingService.PayAsync
    /// from the payment gateway's verified callback instead and remove this endpoint.
    /// </summary>
    [HttpPost("{id:guid}/pay")]
    public async Task<ActionResult<BookingDto>> Pay(Guid id, PayBookingDto dto) =>
        Ok(await _bookings.PayAsync(User.GetUserId(), id, dto));

    /// <summary>Student confirms the class happened; escrow is released to the tutor.</summary>
    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<BookingDto>> Confirm(Guid id) =>
        Ok(await _bookings.ConfirmCompletedAsync(User.GetUserId(), id));

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<BookingDto>> Cancel(Guid id) =>
        Ok(await _bookings.CancelAsync(User.GetUserId(), id));
}
