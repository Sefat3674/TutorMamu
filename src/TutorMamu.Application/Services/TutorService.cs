using TutorMamu.Application.Common;
using TutorMamu.Application.DTOs;
using TutorMamu.Application.Interfaces;
using TutorMamu.Domain.Entities;
using TutorMamu.Domain.Enums;

namespace TutorMamu.Application.Services;

public class TutorService : ITutorProfileService
{
    private readonly IRepository<TutorProfile> _profiles;
    private readonly IRepository<TutorTopic> _tutorTopics;
    private readonly IRepository<Topic> _topics;

    public TutorService(
        IRepository<TutorProfile> profiles,
        IRepository<TutorTopic> tutorTopics,
        IRepository<Topic> topics)
    {
        _profiles = profiles;
        _tutorTopics = tutorTopics;
        _topics = topics;
    }

    public async Task<TutorProfileDto> GetMineAsync(Guid userId)
    {
        var profile = await FindByUserAsync(userId);
        var topicIds = (await _tutorTopics.ListAsync(t => t.TutorProfileId == profile.Id))
            .Select(t => t.TopicId).ToList();
        return TutorProfileDto.From(profile, topicIds);
    }


    public async Task<TutorProfileDto> UpdateMineAsync(
    Guid userId,
    UpdateTutorProfileDto dto)
    {
        // 1. Validate hourly rate
        if (dto.HourlyRate <= 0)
            throw new AppException(
                "Hourly rate must be greater than zero.");

        // 2. Get existing tracked profile
        var profile = await FindByUserAsync(userId);

        // 3. Validate requested topics
        var wantedTopicIds = (dto.TopicIds ?? new List<Guid>())
            .Distinct()
            .ToHashSet();

        var validTopics = await _topics.ListAsync(
            t => wantedTopicIds.Contains(t.Id) && t.IsActive);

        if (validTopics.Count != wantedTopicIds.Count)
            throw new AppException(
                "One or more selected topics are invalid or inactive.");

        // 4. Update profile properties
        profile.Headline = dto.Headline.Trim();
        profile.Bio = dto.Bio.Trim();
        profile.HourlyRate = dto.HourlyRate;
        profile.Area = dto.Area?.Trim();
        profile.TeachesOnline = dto.TeachesOnline;
        profile.UpdatedAt = DateTime.UtcNow;

        // IMPORTANT:
        // Do not call _profiles.Update(profile).
        // The entity is already tracked by EF Core.

        // 5. Get existing topic relationships
        var existingTopics = await _tutorTopics.ListAsync(
            t => t.TutorProfileId == profile.Id);

        var existingTopicIds = existingTopics
            .Select(t => t.TopicId)
            .ToHashSet();

        // 6. Remove unwanted topics
        foreach (var existing in existingTopics)
        {
            if (!wantedTopicIds.Contains(existing.TopicId))
            {
                _tutorTopics.Remove(existing);
            }
        }

        // 7. Add new topics
        foreach (var topicId in wantedTopicIds)
        {
            if (!existingTopicIds.Contains(topicId))
            {
                await _tutorTopics.AddAsync(new TutorTopic
                {
                    TutorProfileId = profile.Id,
                    TopicId = topicId
                });
            }
        }

        // 8. Save changes
        await _profiles.SaveChangesAsync();

        // 9. Return updated profile
        return TutorProfileDto.From(
            profile,
            wantedTopicIds.ToList());
    }


    public async Task<List<TutorProfileDto>> SearchAsync(Guid? topicId)
    {
        List<TutorProfile> profiles;
        if (topicId.HasValue)
        {
            var tid = topicId.Value;
            var profileIds = (await _tutorTopics.ListAsync(t => t.TopicId == tid))
                .Select(t => t.TutorProfileId).ToList();
            profiles = await _profiles.ListAsync(p =>
                p.VerificationsStatus == VerificationsStatus.Approved && profileIds.Contains(p.Id));
        }
        else
        {
            profiles = await _profiles.ListAsync(p => p.VerificationsStatus == VerificationsStatus.Approved);
        }

        return profiles
            .OrderByDescending(p => p.AverageRating)
            .ThenBy(p => p.HourlyRate)
            .Select(p => TutorProfileDto.From(p))
            .ToList();
    }

    public async Task<List<TutorProfileDto>> ListByStatusAsync(VerificationsStatus status)
    {
        var profiles = await _profiles.ListAsync(p => p.VerificationsStatus == status);
        return profiles.Select(p => TutorProfileDto.From(p)).ToList();
    }

    public async Task<TutorProfileDto> SetVerificationAsync(Guid profileId, VerificationsStatus status)
    {
        var profile = await _profiles.GetByIdAsync(profileId)
            ?? throw new AppException("Tutor profile not found.", 404);

        if (status == VerificationsStatus.Approved && profile.HourlyRate <= 0)
            throw new AppException("Tutor must set an hourly rate before approval.");

        profile.VerificationsStatus = status;
        profile.UpdatedAt = DateTime.UtcNow;
        _profiles.Update(profile);
        await _profiles.SaveChangesAsync();
        return TutorProfileDto.From(profile);
    }

    private async Task<TutorProfile> FindByUserAsync(Guid userId) =>
        (await _profiles.ListAsync(p => p.UserId == userId)).FirstOrDefault()
        ?? throw new AppException("Tutor profile not found.", 404);
}
