using TutorMamu.Application.Common;
using TutorMamu.Application.DTOs;
using TutorMamu.Application.Interfaces;
using TutorMamu.Domain.Entities;
using TutorMamu.Domain.Enums;

namespace TutorMamu.Application.Services;

public class TutorProfileService : ITutorProfileService
{
    private readonly IRepository<TutorProfile> _profiles;
    private readonly IRepository<TutorTopic> _tutorTopics;
    private readonly IRepository<Topic> _topics;

    public TutorProfileService(
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

    public async Task<TutorProfileDto> UpdateMineAsync(Guid userId, UpdateTutorProfileDto dto)
    {
        if (dto.HourlyRate <= 0)
            throw new AppException("Hourly rate must be greater than zero.");

        var profile = await FindByUserAsync(userId);
        profile.Headline = dto.Headline.Trim();
        profile.Bio = dto.Bio.Trim();
        profile.HourlyRate = dto.HourlyRate;
        profile.Area = dto.Area?.Trim();
        profile.TeachesOnline = dto.TeachesOnline;
        profile.UpdatedAt = DateTime.UtcNow;
        _profiles.Update(profile);

        // Replace the tutor's topic list. All changes are saved together below.
        foreach (var existing in await _tutorTopics.ListAsync(t => t.TutorProfileId == profile.Id))
            _tutorTopics.Remove(existing);

        var wanted = dto.TopicIds.Distinct().ToList();
        var validTopics = await _topics.ListAsync(t => wanted.Contains(t.Id) && t.IsActive);
        foreach (var topic in validTopics)
            await _tutorTopics.AddAsync(new TutorTopic { TutorProfileId = profile.Id, TopicId = topic.Id });

        await _profiles.SaveChangesAsync();
        return TutorProfileDto.From(profile, validTopics.Select(t => t.Id).ToList());
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
                p.VerificationStatus == VerificationStatus.Approved && profileIds.Contains(p.Id));
        }
        else
        {
            profiles = await _profiles.ListAsync(p => p.VerificationStatus == VerificationStatus.Approved);
        }

        return profiles
            .OrderByDescending(p => p.AverageRating)
            .ThenBy(p => p.HourlyRate)
            .Select(p => TutorProfileDto.From(p))
            .ToList();
    }

    public async Task<List<TutorProfileDto>> ListByStatusAsync(VerificationStatus status)
    {
        var profiles = await _profiles.ListAsync(p => p.VerificationStatus == status);
        return profiles.Select(p => TutorProfileDto.From(p)).ToList();
    }

    public async Task<TutorProfileDto> SetVerificationAsync(Guid profileId, VerificationStatus status)
    {
        var profile = await _profiles.GetByIdAsync(profileId)
            ?? throw new AppException("Tutor profile not found.", 404);

        if (status == VerificationStatus.Approved && profile.HourlyRate <= 0)
            throw new AppException("Tutor must set an hourly rate before approval.");

        profile.VerificationStatus = status;
        profile.UpdatedAt = DateTime.UtcNow;
        _profiles.Update(profile);
        await _profiles.SaveChangesAsync();
        return TutorProfileDto.From(profile);
    }

    private async Task<TutorProfile> FindByUserAsync(Guid userId) =>
        (await _profiles.ListAsync(p => p.UserId == userId)).FirstOrDefault()
        ?? throw new AppException("Tutor profile not found.", 404);
}
