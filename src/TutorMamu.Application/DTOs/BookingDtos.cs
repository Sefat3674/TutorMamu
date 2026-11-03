using TutorMamu.Domain.Entities;

namespace TutorMamu.Application.DTOs;

public record CreateBookingDto(Guid TutorProfileId, Guid TopicId, DateTime StartTime, int DurationHours, string? Notes);

public record PayBookingDto(string Method, string? TransactionRef);

public record RespondBookingDto(bool Accept, string? MeetingLink);

public record BookingDto(
    Guid Id,
    Guid StudentId,
    Guid TutorProfileId,
    Guid TopicId,
    DateTime StartTime,
    int DurationHours,
    decimal HourlyRate,
    decimal TotalAmount,
    decimal CommissionAmount,
    decimal TutorPayout,
    string Status,
    DateTime? ResponseDeadline,
    string? MeetingLink,
    string? Notes)
{
    public static BookingDto From(Booking b) => new(
        b.Id, b.StudentId, b.TutorProfileId, b.TopicId, b.StartTime, b.DurationHours,
        b.HourlyRate, b.TotalAmount, b.CommissionAmount, b.TutorPayout,
        b.Status.ToString(), b.ResponseDeadline, b.MeetingLink, b.Notes);
}
