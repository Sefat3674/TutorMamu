using System.Text.RegularExpressions;
using TutorMamu.Application.Common;
using TutorMamu.Application.DTOs;
using TutorMamu.Application.Interfaces;
using TutorMamu.Domain.Entities;

namespace TutorMamu.Application.Services;

public class TopicService : ITopicService
{
    private readonly IRepository<Topic> _topics;

    public TopicService(IRepository<Topic> topics) => _topics = topics;

    public async Task<List<TopicDto>> ListAsync()
    {
        var items = await _topics.ListAsync(t => t.IsActive);
        return items.OrderBy(t => t.Name).Select(TopicDto.From).ToList();
    }

    public async Task<TopicDto> CreateAsync(CreateTopicDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new AppException("Topic name is required.");

        if (dto.ParentId.HasValue && !await _topics.AnyAsync(t => t.Id == dto.ParentId.Value))
            throw new AppException("Parent topic not found.", 404);

        var slug = Slugify(dto.Name);
        if (await _topics.AnyAsync(t => t.Slug == slug))
            throw new AppException("A topic with this name already exists.", 409);

        var topic = new Topic
        {
            Name = dto.Name.Trim(),
            NameBn = dto.NameBn?.Trim(),
            Slug = slug,
            ParentId = dto.ParentId
        };
        await _topics.AddAsync(topic);
        await _topics.SaveChangesAsync();
        return TopicDto.From(topic);
    }

    public static string Slugify(string name)
    {
        var slug = Regex.Replace(name.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? Guid.NewGuid().ToString("N")[..8] : slug;
    }
}
