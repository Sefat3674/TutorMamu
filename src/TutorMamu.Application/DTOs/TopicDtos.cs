using TutorMamu.Domain.Entities;

namespace TutorMamu.Application.DTOs;

public record CreateTopicDto(string Name, string? NameBn, Guid? ParentId);

public record TopicDto(Guid Id, string Name, string? NameBn, string Slug, Guid? ParentId, bool IsActive)
{
    public static TopicDto From(Topic t) => new(t.Id, t.Name, t.NameBn, t.Slug, t.ParentId, t.IsActive);
}
