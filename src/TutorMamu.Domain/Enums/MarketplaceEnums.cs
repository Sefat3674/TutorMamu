namespace TutorMamu.Domain.Enums;

public enum VerificationStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Suspended = 3
}

/// <summary>
/// Booking lifecycle: PendingPayment -> Paid (money held in escrow) -> Accepted -> Settled.
/// Declined / unanswered requests end as Refunded; student cancellations end as Cancelled.
/// </summary>
public enum BookingStatus
{
    PendingPayment = 0,
    Paid = 1,
    Accepted = 2,
    Completed = 3,
    Settled = 4,
    Cancelled = 5,
    Refunded = 6
}

public enum PaymentStatus
{
    Pending = 0,
    Held = 1,
    Released = 2,
    Refunded = 3,
    Failed = 4
}
