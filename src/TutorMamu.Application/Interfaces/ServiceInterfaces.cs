using TutorMamu.Application.DTOs;
using TutorMamu.Domain.Entities;
using TutorMamu.Domain.Enums;

namespace TutorMamu.Application.Interfaces;



public interface ITokenService
{
    AuthResponseDto CreateToken(ApplicationUser user, IList<string> roles);
}

public interface ITopicService
{
    Task<List<TopicDto>> ListAsync();
    Task<TopicDto> CreateAsync(CreateTopicDto dto);
}

public interface ITutorProfileService
{
    Task<TutorProfileDto> GetMineAsync(Guid userId);

    Task<TutorProfileDto> UpdateMineAsync(
        Guid userId,
        UpdateTutorProfileDto dto);

    Task<List<TutorProfileDto>> SearchAsync(Guid? topicId);

    Task<List<TutorProfileDto>> ListByStatusAsync(
        VerificationsStatus status);

    Task<TutorProfileDto> SetVerificationAsync(
        Guid profileId,
        VerificationsStatus status);
}

public interface IBookingService
{
    Task<BookingDto> CreateAsync(Guid studentId, CreateBookingDto dto);
    Task<BookingDto> PayAsync(Guid studentId, Guid bookingId, PayBookingDto dto);
    Task<BookingDto> RespondAsync(Guid tutorUserId, Guid bookingId, RespondBookingDto dto);
    Task<BookingDto> ConfirmCompletedAsync(Guid studentId, Guid bookingId);
    Task<BookingDto> CancelAsync(Guid studentId, Guid bookingId);
    Task<List<BookingDto>> GetStudentBookingsAsync(Guid studentId);
    Task<List<BookingDto>> GetTutorBookingsAsync(Guid tutorUserId);

    /// <summary>Refunds paid bookings the tutor never answered. Called by a background worker.</summary>
    Task<int> ExpireUnansweredAsync();
}

/// <summary>Sends SMS / email. Implemented in Infrastructure; the template ships with a logging stub.</summary>
public interface INotificationService
{
    Task NotifyTutorNewBookingAsync(Guid tutorUserId, Booking booking);
    Task NotifyStudentResponseAsync(Guid studentUserId, Booking booking, bool accepted);
}
