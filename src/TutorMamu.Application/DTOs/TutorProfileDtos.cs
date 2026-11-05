using TutorMamu.Domain.Entities;

namespace TutorMamu.Application.DTOs;

public record UpdateTutorProfileDto(
    string Headline,
    string Bio,
    decimal HourlyRate,
    string? Area,
    bool TeachesOnline,
    List<Guid> TopicIds);

public record SetVerificationDto(string Status); // Approved | Rejected | Suspended | Pending

public record TutorProfileDto(
    Guid Id,
    Guid UserId,
    string Headline,
    string Bio,
    decimal HourlyRate,
    string? Area,
    bool TeachesOnline,
    string VerificationStatus,
    decimal AverageRating,
    int ReviewCount,
    List<Guid> TopicIds)
{
    public static TutorProfileDto From(TutorProfile p, List<Guid>? topicIds = null) => new(
        p.Id, p.UserId, p.Headline, p.Bio, p.HourlyRate, p.Area, p.TeachesOnline,
        p.VerificationsStatus.ToString(), p.AverageRating, p.ReviewCount, topicIds ?? new List<Guid>());
}
