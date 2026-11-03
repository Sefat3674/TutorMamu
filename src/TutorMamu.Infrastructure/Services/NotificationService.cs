using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TutorMamu.Application.Interfaces;
using TutorMamu.Domain.Entities;

namespace TutorMamu.Infrastructure.Services;

/// <summary>
/// Development stub: logs the SMS and email instead of sending them.
/// Replace the two Send methods with your SMS gateway and SMTP/email provider.
/// </summary>
public class LoggingNotificationService : INotificationService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly ILogger<LoggingNotificationService> _logger;

    public LoggingNotificationService(UserManager<ApplicationUser> users, ILogger<LoggingNotificationService> logger)
    {
        _users = users;
        _logger = logger;
    }

    public async Task NotifyTutorNewBookingAsync(Guid tutorUserId, Booking booking)
    {
        var tutor = await _users.FindByIdAsync(tutorUserId.ToString());
        if (tutor == null) return;

        var text = $"New class request: {booking.DurationHours}h on {booking.StartTime:yyyy-MM-dd HH:mm} UTC. " +
                   $"Please accept or decline before {booking.ResponseDeadline:HH:mm} UTC.";
        await SendSmsAsync(tutor.PhoneNumber, text);
        await SendEmailAsync(tutor.Email, "New class request", text);
    }

    public async Task NotifyStudentResponseAsync(Guid studentUserId, Booking booking, bool accepted)
    {
        var student = await _users.FindByIdAsync(studentUserId.ToString());
        if (student == null) return;

        var text = accepted
            ? $"Your class on {booking.StartTime:yyyy-MM-dd HH:mm} UTC was accepted."
            : "Your class request was declined or not answered. Your payment will be refunded.";
        await SendSmsAsync(student.PhoneNumber, text);
        await SendEmailAsync(student.Email, "Class request update", text);
    }

    private Task SendSmsAsync(string? phone, string message)
    {
        if (!string.IsNullOrWhiteSpace(phone))
            _logger.LogInformation("[SMS stub] to {Phone}: {Message}", phone, message);
        return Task.CompletedTask;
    }

    private Task SendEmailAsync(string? email, string subject, string message)
    {
        if (!string.IsNullOrWhiteSpace(email))
            _logger.LogInformation("[Email stub] to {Email} | {Subject}: {Message}", email, subject, message);
        return Task.CompletedTask;
    }
}
