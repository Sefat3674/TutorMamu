using Microsoft.Extensions.Options;
using TutorMamu.Application.Common;
using TutorMamu.Application.DTOs;
using TutorMamu.Application.Interfaces;
using TutorMamu.Domain.Entities;
using TutorMamu.Domain.Enums;

namespace TutorMamu.Application.Services;

public class BookingService : IBookingService
{
    private readonly IRepository<Booking> _bookings;
    private readonly IRepository<TutorProfile> _tutors;
    private readonly IRepository<Topic> _topics;
    private readonly IRepository<Payment> _payments;
    private readonly INotificationService _notifications;
    private readonly MarketplaceOptions _options;

    public BookingService(
        IRepository<Booking> bookings,
        IRepository<TutorProfile> tutors,
        IRepository<Topic> topics,
        IRepository<Payment> payments,
        INotificationService notifications,
        IOptions<MarketplaceOptions> options)
    {
        _bookings = bookings;
        _tutors = tutors;
        _topics = topics;
        _payments = payments;
        _notifications = notifications;
        _options = options.Value;
    }

    /// <summary>Price = hourly rate x hours. Commission = total x rate. Pure function, easy to unit test.</summary>
    public static (decimal Total, decimal Commission) CalculatePrice(decimal hourlyRate, int hours, decimal commissionRate)
    {
        if (hourlyRate < 0) throw new ArgumentOutOfRangeException(nameof(hourlyRate));
        if (hours <= 0) throw new ArgumentOutOfRangeException(nameof(hours));
        if (commissionRate < 0 || commissionRate > 1) throw new ArgumentOutOfRangeException(nameof(commissionRate));

        var total = Math.Round(hourlyRate * hours, 2);
        var commission = Math.Round(total * commissionRate, 2);
        return (total, commission);
    }

    public async Task<BookingDto> CreateAsync(Guid studentId, CreateBookingDto dto)
    {
        if (dto.DurationHours < _options.MinDurationHours || dto.DurationHours > _options.MaxDurationHours)
            throw new AppException($"Class duration must be {_options.MinDurationHours} to {_options.MaxDurationHours} hours.");

        var start = DateTime.SpecifyKind(dto.StartTime, DateTimeKind.Utc);
        if (start <= DateTime.UtcNow)
            throw new AppException("Class start time must be in the future.");

        var tutor = await _tutors.GetByIdAsync(dto.TutorProfileId)
            ?? throw new AppException("Tutor not found.", 404);
        if (tutor.VerificationsStatus != VerificationsStatus.Approved)
            throw new AppException("This tutor is not available for booking.");
        if (tutor.UserId == studentId)
            throw new AppException("You cannot book your own class.");

        if (!await _topics.AnyAsync(t => t.Id == dto.TopicId && t.IsActive))
            throw new AppException("Topic not found.", 404);

        // Prevent double-booking the tutor's slot (paid or accepted classes only).
        var end = start.AddHours(dto.DurationHours);
        var windowStart = start.AddHours(-_options.MaxDurationHours);
        var nearby = await _bookings.ListAsync(b =>
            b.TutorProfileId == tutor.Id
            && (b.Status == BookingStatus.Paid || b.Status == BookingStatus.Accepted)
            && b.StartTime > windowStart && b.StartTime < end);
        if (nearby.Any(b => b.StartTime.AddHours(b.DurationHours) > start))
            throw new AppException("The tutor already has a class in this time slot.", 409);

        var (total, commission) = CalculatePrice(tutor.HourlyRate, dto.DurationHours, _options.CommissionRate);
        var booking = new Booking
        {
            StudentId = studentId,
            TutorProfileId = tutor.Id,
            TopicId = dto.TopicId,
            StartTime = start,
            DurationHours = dto.DurationHours,
            HourlyRate = tutor.HourlyRate,
            TotalAmount = total,
            CommissionAmount = commission,
            Status = BookingStatus.PendingPayment,
            Notes = dto.Notes
        };

        await _bookings.AddAsync(booking);
        await _bookings.SaveChangesAsync();
        return BookingDto.From(booking);
    }

    /// <summary>
    /// Marks the booking paid and holds the money (escrow). In production call this from your payment
    /// gateway's verified callback/webhook, never directly from a client request.
    /// </summary>
    public async Task<BookingDto> PayAsync(Guid studentId, Guid bookingId, PayBookingDto dto)
    {
        var booking = await GetOwnedByStudentAsync(studentId, bookingId);
        if (booking.Status != BookingStatus.PendingPayment)
            throw new AppException("This booking is not awaiting payment.");

        await _payments.AddAsync(new Payment
        {
            BookingId = booking.Id,
            Amount = booking.TotalAmount,
            Method = dto.Method,
            TransactionRef = dto.TransactionRef,
            Status = PaymentStatus.Held
        });

        booking.Status = BookingStatus.Paid;
        booking.ResponseDeadline = DateTime.UtcNow.AddHours(_options.ResponseWindowHours);
        booking.UpdatedAt = DateTime.UtcNow;
        _bookings.Update(booking);
        await _bookings.SaveChangesAsync();

        var tutor = await _tutors.GetByIdAsync(booking.TutorProfileId);
        if (tutor != null)
            await _notifications.NotifyTutorNewBookingAsync(tutor.UserId, booking);

        return BookingDto.From(booking);
    }

    public async Task<BookingDto> RespondAsync(Guid tutorUserId, Guid bookingId, RespondBookingDto dto)
    {
        var tutor = (await _tutors.ListAsync(t => t.UserId == tutorUserId)).FirstOrDefault()
            ?? throw new AppException("Tutor profile not found.", 404);
        var booking = await _bookings.GetByIdAsync(bookingId)
            ?? throw new AppException("Booking not found.", 404);

        if (booking.TutorProfileId != tutor.Id)
            throw new AppException("This booking belongs to another tutor.", 403);
        if (booking.Status != BookingStatus.Paid)
            throw new AppException("This booking is not waiting for your response.");
        if (booking.ResponseDeadline.HasValue && booking.ResponseDeadline.Value < DateTime.UtcNow)
            throw new AppException("The response window has expired; the student will be refunded.");

        if (dto.Accept)
        {
            booking.Status = BookingStatus.Accepted;
            booking.MeetingLink = dto.MeetingLink;
        }
        else
        {
            await RefundAsync(booking);
            booking.Status = BookingStatus.Refunded;
        }

        booking.UpdatedAt = DateTime.UtcNow;
        _bookings.Update(booking);
        await _bookings.SaveChangesAsync();

        await _notifications.NotifyStudentResponseAsync(booking.StudentId, booking, dto.Accept);
        return BookingDto.From(booking);
    }

    public async Task<BookingDto> ConfirmCompletedAsync(Guid studentId, Guid bookingId)
    {
        var booking = await GetOwnedByStudentAsync(studentId, bookingId);
        if (booking.Status != BookingStatus.Accepted)
            throw new AppException("Only accepted classes can be confirmed.");
        if (booking.StartTime > DateTime.UtcNow)
            throw new AppException("The class has not started yet.");

        foreach (var payment in await _payments.ListAsync(p => p.BookingId == booking.Id && p.Status == PaymentStatus.Held))
        {
            payment.Status = PaymentStatus.Released; // payout to tutor = TotalAmount - CommissionAmount
            _payments.Update(payment);
        }

        booking.Status = BookingStatus.Settled;
        booking.UpdatedAt = DateTime.UtcNow;
        _bookings.Update(booking);
        await _bookings.SaveChangesAsync();
        return BookingDto.From(booking);
    }

    public async Task<BookingDto> CancelAsync(Guid studentId, Guid bookingId)
    {
        var booking = await GetOwnedByStudentAsync(studentId, bookingId);
        if (booking.Status != BookingStatus.PendingPayment && booking.Status != BookingStatus.Paid)
            throw new AppException("This booking can no longer be cancelled.");

        if (booking.Status == BookingStatus.Paid)
            await RefundAsync(booking);

        booking.Status = BookingStatus.Cancelled;
        booking.UpdatedAt = DateTime.UtcNow;
        _bookings.Update(booking);
        await _bookings.SaveChangesAsync();
        return BookingDto.From(booking);
    }

    public async Task<List<BookingDto>> GetStudentBookingsAsync(Guid studentId)
    {
        var items = await _bookings.ListAsync(b => b.StudentId == studentId);
        return items.OrderByDescending(b => b.StartTime).Select(BookingDto.From).ToList();
    }

    public async Task<List<BookingDto>> GetTutorBookingsAsync(Guid tutorUserId)
    {
        var tutor = (await _tutors.ListAsync(t => t.UserId == tutorUserId)).FirstOrDefault()
            ?? throw new AppException("Tutor profile not found.", 404);
        var items = await _bookings.ListAsync(b =>
            b.TutorProfileId == tutor.Id && b.Status != BookingStatus.PendingPayment);
        return items.OrderByDescending(b => b.StartTime).Select(BookingDto.From).ToList();
    }

    public async Task<int> ExpireUnansweredAsync()
    {
        var now = DateTime.UtcNow;
        var expired = await _bookings.ListAsync(b =>
            b.Status == BookingStatus.Paid && b.ResponseDeadline != null && b.ResponseDeadline < now);

        foreach (var booking in expired)
        {
            await RefundAsync(booking);
            booking.Status = BookingStatus.Refunded;
            booking.UpdatedAt = now;
            _bookings.Update(booking);
            await _notifications.NotifyStudentResponseAsync(booking.StudentId, booking, false);
        }

        if (expired.Count > 0)
            await _bookings.SaveChangesAsync();
        return expired.Count;
    }

    private async Task RefundAsync(Booking booking)
    {
        // TODO: also call the payment gateway's refund API here.
        foreach (var payment in await _payments.ListAsync(p => p.BookingId == booking.Id && p.Status == PaymentStatus.Held))
        {
            payment.Status = PaymentStatus.Refunded;
            _payments.Update(payment);
        }
    }

    private async Task<Booking> GetOwnedByStudentAsync(Guid studentId, Guid bookingId)
    {
        var booking = await _bookings.GetByIdAsync(bookingId)
            ?? throw new AppException("Booking not found.", 404);
        if (booking.StudentId != studentId)
            throw new AppException("This booking belongs to another user.", 403);
        return booking;
    }
}
