
using TutorMamu.Domain.Enums;

namespace TutorMamu.Domain.Entities;

public class TutorProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string Headline { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;

    public decimal HourlyRate { get; set; }
    public string? Area { get; set; }

    public bool TeachesOnline { get; set; } = true;

    public VerificationsStatus VerificationsStatus { get; set; }
        = VerificationsStatus.NotSubmitted;

    public DateTime? VerificationSubmittedAt { get; set; }

    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }

    public ICollection<TutorTopic> TutorTopics { get; set; }
        = new List<TutorTopic>();

    public ICollection<TutorDocument> Documents { get; set; }
        = new List<TutorDocument>();
}

public class TutorTopic : BaseEntity
{
    public Guid TutorProfileId { get; set; }
    public TutorProfile TutorProfile { get; set; } = null!;

    public Guid TopicId { get; set; }
    public Topic Topic { get; set; } = null!;
}
