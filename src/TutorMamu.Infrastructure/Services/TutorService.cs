
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using TutorMamu.Application.DTOs.Tutors;
using TutorMamu.Application.Interfaces;
using TutorMamu.Domain.Entities;
using TutorMamu.Domain.Enums;
using TutorMamu.Infrastructure.Data;

namespace TutorMamu.Infrastructure.Services;

public class TutorService : ITutorService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    private const long MaxFileSize = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".pdf"
        };

    public TutorService(
        ApplicationDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // =====================================================
    // 1. CREATE TUTOR PROFILE
    // =====================================================

    public async Task<TutorProfileResponse> CreateProfileAsync(
        Guid userId,
        CreateTutorProfileRequest request)
    {
        var userExists = await _context.Users
            .AnyAsync(x => x.Id == userId);

        if (!userExists)
            throw new KeyNotFoundException("User not found.");

        var exists = await _context.TutorProfiles
            .AnyAsync(x => x.UserId == userId);

        if (exists)
            throw new InvalidOperationException(
                "Tutor profile already exists.");

        var topicIds = request.TopicIds.Distinct().ToList();

        await ValidateTopicsAsync(topicIds);

        var profile = new TutorProfile
        {
            UserId = userId,
            Headline = request.Headline.Trim(),
            Bio = request.Bio.Trim(),
            HourlyRate = request.HourlyRate,
            Area = request.Area?.Trim(),
            TeachesOnline = request.TeachesOnline,
            VerificationsStatus = VerificationsStatus.NotSubmitted,

            TutorTopics = topicIds.Select(id => new TutorTopic
            {
                TopicId = id
            }).ToList()
        };

        _context.TutorProfiles.Add(profile);

        await _context.SaveChangesAsync();

        return MapProfile(profile);
    }

    // =====================================================
    // 2. GET MY PROFILE
    // =====================================================

    public async Task<TutorProfileResponse?> GetMyProfileAsync(
        Guid userId)
    {
        var profile = await _context.TutorProfiles
            .Include(x => x.User)
            .Include(x => x.TutorTopics)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        return profile == null ? null : MapProfile(profile);
    }

    // =====================================================
    // 3. UPDATE MY PROFILE
    // =====================================================


    public async Task<TutorProfileResponse?> UpdateProfileAsync(
        Guid userId,
        UpdateTutorProfileRequest request)
    {
        // 1. Find existing tracked profile
        var profile = await _context.TutorProfiles
            .Include(x => x.User)
            .Include(x => x.TutorTopics)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (profile == null)
            return null;

        // 2. Validate request
        if (request.HourlyRate <= 0)
            throw new ArgumentException(
                "Hourly rate must be greater than zero.");

        if (string.IsNullOrWhiteSpace(request.Headline))
            throw new ArgumentException(
                "Headline is required.");

        if (string.IsNullOrWhiteSpace(request.Bio))
            throw new ArgumentException(
                "Bio is required.");

        // 3. Get unique topic IDs
        var topicIds = (request.TopicIds ?? new List<Guid>())
            .Distinct()
            .ToHashSet();

        // 4. Validate selected topics
        await ValidateTopicsAsync(topicIds.ToList());

        // 5. Update profile
        profile.Headline = request.Headline.Trim();
        profile.Bio = request.Bio.Trim();
        profile.HourlyRate = request.HourlyRate;
        profile.Area = request.Area?.Trim();
        profile.TeachesOnline = request.TeachesOnline;
        profile.UpdatedAt = DateTime.UtcNow;

        // IMPORTANT:
        // Profile is already tracked by EF Core.
        // Do not call _context.TutorProfiles.Update(profile).

        // 6. Get existing topic relationships
        var existingTopics = profile.TutorTopics.ToList();

        var existingTopicIds = existingTopics
            .Select(x => x.TopicId)
            .ToHashSet();

        // 7. Remove only unwanted topics
        foreach (var existing in existingTopics)
        {
            if (!topicIds.Contains(existing.TopicId))
            {
                profile.TutorTopics.Remove(existing);

                _context.TutorTopics.Remove(existing);
            }
        }

        // 8. Add only new topics
        foreach (var topicId in topicIds)
        {
            if (!existingTopicIds.Contains(topicId))
            {
                var newTopic = new TutorTopic
                {
                    TutorProfileId = profile.Id,
                    TopicId = topicId
                };

                profile.TutorTopics.Add(newTopic);
            }
        }

        // 9. Save changes
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var failedEntities = ex.Entries
                .Select(entry => new
                {
                    Entity = entry.Metadata.ClrType.Name,
                    State = entry.State.ToString()
                })
                .ToList();

            // Log the actual entity responsible for the exception.
            Console.WriteLine(
                "Tutor profile update concurrency exception:");

            foreach (var entry in failedEntities)
            {
                Console.WriteLine(
                    $"Entity: {entry.Entity}, State: {entry.State}");
            }

            throw;
        }

        // 10. Return updated profile
        return MapProfile(profile);
    }


    // =====================================================
    // 4. UPLOAD VERIFICATION DOCUMENT
    // =====================================================

    public async Task<TutorDocumentResponse> UploadDocumentAsync(
        Guid userId,
        UploadTutorDocumentRequest request)
    {
        var profile = await _context.TutorProfiles
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (profile == null)
            throw new KeyNotFoundException(
                "Create your tutor profile first.");

        if (profile.VerificationsStatus == VerificationsStatus.Pending ||
            profile.VerificationsStatus == VerificationsStatus.Approved)
        {
            throw new InvalidOperationException(
                "Documents cannot be changed while verification is pending or approved.");
        }

        if (request.File == null || request.File.Length == 0)
            throw new ArgumentException("Please select a file.");

        if (request.File.Length > MaxFileSize)
            throw new ArgumentException(
                "Maximum file size is 5 MB.");

        if (!Enum.TryParse<TutorDocumentType>(
                request.DocumentType, true, out var documentType) ||
            !Enum.IsDefined(typeof(TutorDocumentType), documentType))
        {
            throw new ArgumentException(
                "Invalid document type.");
        }

        var documentSide = DocumentSide.NOT_APPLICABLE;

        if (documentType == TutorDocumentType.NID)
        {
            if (!Enum.TryParse<DocumentSide>(
                    request.DocumentSide, true, out documentSide) ||
                (documentSide != DocumentSide.FRONT &&
                 documentSide != DocumentSide.BACK))
            {
                throw new ArgumentException(
                    "NID requires FRONT or BACK document side.");
            }
        }

        if (documentType == TutorDocumentType.EDUCATION_CERTIFICATE &&
            !string.IsNullOrWhiteSpace(request.DocumentSide))
        {
            throw new ArgumentException(
                "Education certificates do not require a document side.");
        }

        var extension = Path.GetExtension(
            Path.GetFileName(request.File.FileName));

        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException(
                "Only JPG, JPEG, PNG and PDF files are allowed.");

        var contentType = extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            _ => throw new ArgumentException("Unsupported file type.")
        };

        var duplicate = await _context.TutorDocuments.AnyAsync(x =>
            x.TutorProfileId == profile.Id &&
            x.DocumentType == documentType &&
            x.DocumentSide == documentSide &&
            x.Status != DocumentStatus.Rejected);

        if (duplicate)
            throw new InvalidOperationException(
                "This document has already been uploaded.");

        var folder = Path.Combine(
            _environment.ContentRootPath,
            "App_Data",
            "TutorDocuments",
            userId.ToString());

        Directory.CreateDirectory(folder);

        var storedFileName =
            $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

        var fullPath = Path.Combine(folder, storedFileName);

        try
        {
            await using (var output = new FileStream(
                fullPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                useAsync: true))
            {
                await request.File.CopyToAsync(output);
            }

            var document = new TutorDocument
            {
                TutorProfileId = profile.Id,
                DocumentType = documentType,
                DocumentSide = documentSide,
                OriginalFileName = Path.GetFileName(
                    request.File.FileName),
                StoredFileName = storedFileName,
                StoragePath = fullPath,
                ContentType = contentType,
                FileSize = request.File.Length,
                Status = DocumentStatus.PendingReview,
                UploadedAt = DateTime.UtcNow
            };

            _context.TutorDocuments.Add(document);

            await _context.SaveChangesAsync();

            return MapDocument(document);
        }
        catch
        {
            if (File.Exists(fullPath))
                File.Delete(fullPath);

            throw;
        }
    }

    // =====================================================
    // 5. GET MY DOCUMENTS
    // =====================================================

    public async Task<List<TutorDocumentResponse>> GetMyDocumentsAsync(
        Guid userId)
    {
        var profileId = await _context.TutorProfiles
            .Where(x => x.UserId == userId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync();

        if (profileId == null)
            throw new KeyNotFoundException(
                "Tutor profile not found.");

        var documents = await _context.TutorDocuments
            .Where(x => x.TutorProfileId == profileId.Value)
            .OrderByDescending(x => x.UploadedAt)
            .ToListAsync();

        return documents.Select(MapDocument).ToList();
    }

    // =====================================================
    // 6. DELETE DOCUMENT
    // =====================================================

    public async Task<bool> DeleteDocumentAsync(
        Guid userId,
        Guid documentId)
    {
        var document = await _context.TutorDocuments
            .Include(x => x.TutorProfile)
            .FirstOrDefaultAsync(x =>
                x.Id == documentId &&
                x.TutorProfile.UserId == userId);

        if (document == null)
            return false;

        if (document.Status != DocumentStatus.PendingReview ||
            document.TutorProfile.VerificationsStatus ==
                VerificationsStatus.Pending ||
            document.TutorProfile.VerificationsStatus ==
                VerificationsStatus.Approved)
        {
            throw new InvalidOperationException(
                "This document cannot be deleted.");
        }

        var path = document.StoragePath;

        _context.TutorDocuments.Remove(document);

        await _context.SaveChangesAsync();

        if (File.Exists(path))
            File.Delete(path);

        return true;
    }

    // =====================================================
    // 7. GET VERIFICATION STATUS
    // =====================================================

    public async Task<VerificationResponse> GetVerificationStatusAsync(
        Guid userId)
    {
        var profile = await _context.TutorProfiles
            .Include(x => x.Documents)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (profile == null)
            throw new KeyNotFoundException(
                "Tutor profile not found.");

        var nidDocuments = profile.Documents
            .Where(x => x.DocumentType == TutorDocumentType.NID)
            .ToList();

        var educationDocuments = profile.Documents
            .Where(x => x.DocumentType ==
                TutorDocumentType.EDUCATION_CERTIFICATE)
            .ToList();

        return new VerificationResponse
        {
            PhoneVerified = profile.User.PhoneNumberConfirmed,

            NidStatus = GetGroupStatus(
                nidDocuments,
                requireBothSides: true),

            EducationStatus = GetGroupStatus(
                educationDocuments,
                requireBothSides: false),

            OverallStatus = profile.VerificationsStatus.ToString()
                .ToUpperInvariant(),

            RejectionReason = profile.Documents
                .Where(x => x.Status == DocumentStatus.Rejected)
                .Select(x => x.RejectionReason)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),

            SubmittedAt = profile.VerificationSubmittedAt
        };
    }

    // =====================================================
    // 8. SUBMIT VERIFICATION
    // =====================================================

    public async Task<VerificationResponse> SubmitVerificationAsync(
        Guid userId,
        SubmitVerificationRequest request)
    {
        var profile = await _context.TutorProfiles
            .Include(x => x.User)
            .Include(x => x.Documents)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (profile == null)
            throw new KeyNotFoundException(
                "Tutor profile not found.");

        if (profile.VerificationsStatus == VerificationsStatus.Pending)
            throw new InvalidOperationException(
                "Verification is already pending.");

        if (profile.VerificationsStatus == VerificationsStatus.Approved)
            throw new InvalidOperationException(
                "Your account is already verified.");

        var documentIds = request.DocumentIds.Distinct().ToList();

        if (documentIds.Count != request.DocumentIds.Count)
            throw new ArgumentException(
                "Duplicate document IDs are not allowed.");

        if (!documentIds.Any())
            throw new ArgumentException(
                "Please select verification documents.");

        var selectedDocuments = profile.Documents
            .Where(x => documentIds.Contains(x.Id))
            .ToList();

        if (selectedDocuments.Count != documentIds.Count)
            throw new ArgumentException(
                "One or more documents are invalid.");

        if (selectedDocuments.Any(x =>
                x.Status != DocumentStatus.PendingReview))
        {
            throw new InvalidOperationException(
                "Only pending-review documents can be submitted.");
        }

        var hasNidFront = selectedDocuments.Any(x =>
            x.DocumentType == TutorDocumentType.NID &&
            x.DocumentSide == DocumentSide.FRONT);

        var hasNidBack = selectedDocuments.Any(x =>
            x.DocumentType == TutorDocumentType.NID &&
            x.DocumentSide == DocumentSide.BACK);

        var hasEducation = selectedDocuments.Any(x =>
            x.DocumentType ==
            TutorDocumentType.EDUCATION_CERTIFICATE);

        if (!hasNidFront || !hasNidBack || !hasEducation)
        {
            throw new InvalidOperationException(
                "Please upload NID FRONT, NID BACK and at least one education certificate.");
        }

        if (!profile.User.PhoneNumberConfirmed)
            throw new InvalidOperationException(
                "Verify your phone number before submitting verification.");

        profile.VerificationsStatus = VerificationsStatus.Pending;
        profile.VerificationSubmittedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetVerificationStatusAsync(userId);
    }

    // =====================================================
    // 9. PUBLIC TUTOR DETAILS
    // =====================================================

    public async Task<PublicTutorResponse?> GetPublicTutorAsync(
        Guid tutorId)
    {
        var profile = await _context.TutorProfiles
            .Include(x => x.User)
            .Include(x => x.TutorTopics)
                .ThenInclude(x => x.Topic)
            .FirstOrDefaultAsync(x =>
                x.Id == tutorId &&
                x.VerificationsStatus == VerificationsStatus.Approved);

        if (profile == null)
            return null;

        return new PublicTutorResponse
        {
            TutorId = profile.Id,
            TutorName = profile.User.FullName,
            Headline = profile.Headline,
            Bio = profile.Bio,
            HourlyRate = profile.HourlyRate,
            Area = profile.Area,
            TeachesOnline = profile.TeachesOnline,
            AverageRating = profile.AverageRating,
            ReviewCount = profile.ReviewCount,
            Topics = profile.TutorTopics
                .Select(x => x.Topic.Name)
                .ToList()
        };
    }

    // =====================================================
    // PRIVATE HELPERS
    // =====================================================


    private async Task ValidateTopicsAsync(List<Guid> topicIds)
    {
        if (topicIds.Count == 0)
            return;

        var count = await _context.Topics
            .CountAsync(x =>
                topicIds.Contains(x.Id) &&
                x.IsActive);

        if (count != topicIds.Count)
        {
            throw new ArgumentException(
                "One or more selected topics are invalid or inactive.");
        }
    }


    private static string GetGroupStatus(
        List<TutorDocument> documents,
        bool requireBothSides)
    {
        if (!documents.Any())
            return "NOT_SUBMITTED";

        if (documents.Any(x =>
            x.Status == DocumentStatus.Rejected))
            return "REJECTED";

        if (requireBothSides)
        {
            var hasFront = documents.Any(x =>
                x.DocumentSide == DocumentSide.FRONT &&
                x.Status == DocumentStatus.Approved);

            var hasBack = documents.Any(x =>
                x.DocumentSide == DocumentSide.BACK &&
                x.Status == DocumentStatus.Approved);

            if (hasFront && hasBack)
                return "APPROVED";
        }
        else if (documents.Any(x =>
            x.Status == DocumentStatus.Approved))
        {
            return "APPROVED";
        }

        return "PENDING";
    }

    private static TutorProfileResponse MapProfile(
        TutorProfile profile)
    {
        return new TutorProfileResponse
        {
            Id = profile.Id,
            UserId = profile.UserId,
            TutorName = profile.User?.FullName,
            Headline = profile.Headline,
            Bio = profile.Bio,
            HourlyRate = profile.HourlyRate,
            Area = profile.Area,
            TeachesOnline = profile.TeachesOnline,
            VerificationStatus = profile.VerificationsStatus
                .ToString().ToUpperInvariant(),
            AverageRating = profile.AverageRating,
            ReviewCount = profile.ReviewCount,
            TopicIds = profile.TutorTopics
                .Select(x => x.TopicId)
                .ToList()
        };
    }

    private static TutorDocumentResponse MapDocument(
        TutorDocument document)
    {
        return new TutorDocumentResponse
        {
            Id = document.Id,
            DocumentType = document.DocumentType.ToString(),
            DocumentSide = document.DocumentSide.ToString(),
            OriginalFileName = document.OriginalFileName,
            FileSize = document.FileSize,
            Status = document.Status.ToString().ToUpperInvariant(),
            RejectionReason = document.RejectionReason,
            UploadedAt = document.UploadedAt,
            ReviewedAt = document.ReviewedAt
        };
    }
}
