namespace TutorMamu.Domain.Entities;

/// <summary>A learning topic, e.g. Math > Calculus > Integration. Topics form a tree via ParentId.</summary>
public class Topic : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? NameBn { get; set; }
    public string Slug { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public Topic? Parent { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Topic> Children { get; set; } = new List<Topic>();
    public ICollection<TutorTopic> TutorTopics { get; set; } = new List<TutorTopic>();
}
