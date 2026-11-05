
using TutorMamu.Application.DTOs.Tutors;

namespace TutorMamu.Application.Interfaces;

public interface ITutorService
{
    // Profile
    Task<TutorProfileResponse> CreateProfileAsync(
        Guid userId,
        CreateTutorProfileRequest request);

    Task<TutorProfileResponse?> GetMyProfileAsync(Guid userId);

    Task<TutorProfileResponse?> UpdateProfileAsync(
        Guid userId,
        UpdateTutorProfileRequest request);

    // Documents
    Task<TutorDocumentResponse> UploadDocumentAsync(
        Guid userId,
        UploadTutorDocumentRequest request);

    Task<List<TutorDocumentResponse>> GetMyDocumentsAsync(Guid userId);

    Task<bool> DeleteDocumentAsync(Guid userId, Guid documentId);

    // Verification
    Task<VerificationResponse> GetVerificationStatusAsync(Guid userId);

    Task<VerificationResponse> SubmitVerificationAsync(
        Guid userId,
        SubmitVerificationRequest request);

    // Public tutor
    Task<PublicTutorResponse?> GetPublicTutorAsync(Guid tutorId);
}


