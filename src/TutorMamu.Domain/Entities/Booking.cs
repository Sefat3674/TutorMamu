using TutorMamu.Domain.Enums;

namespace TutorMamu.Domain.Entities;

public class Booking : BaseEntity
{
    public Guid StudentId { get; set; }
    public ApplicationUser Student { get; set; } = null!;

    public Guid TutorProfileId { get; set; }
    public TutorProfile TutorProfile { get; set; } = null!;

    public Guid TopicId { get; set; }
    public Topic Topic { get; set; } = null!;

    public DateTime StartTime { get; set; }          // UTC
    public int DurationHours { get; set; }           // 1, 2 or 3
    public decimal HourlyRate { get; set; }          // snapshot at booking time
    public decimal TotalAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal TutorPayout => TotalAmount - CommissionAmount;

    public BookingStatus Status { get; set; } = BookingStatus.PendingPayment;
    public DateTime? ResponseDeadline { get; set; }  // tutor must answer before this
    public string? MeetingLink { get; set; }
    public string? Notes { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public class Payment : BaseEntity
{
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;   // bKash, Nagad, Card ...
    public string? TransactionRef { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
}

public class Review : BaseEntity
{
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public int Rating { get; set; }                      // 1..5
    public string? Comment { get; set; }
}

public class ApplicationSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
