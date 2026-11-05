
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TutorMamu.Application.DTOs.Users;
using TutorMamu.Application.Interfaces;
using TutorMamu.Domain.Entities;
using TutorMamu.Infrastructure.Data;

namespace TutorMamu.Infrastructure.Services;

public class UserService(
    UserManager<ApplicationUser> users,
    ApplicationDbContext db,
    RoleManager<ApplicationRole> roleManager,
    IWebHostEnvironment environment) : IUserService
{
    private const long MaxImageSize = 5 * 1024 * 1024;

    public async Task<UserProfileDto> GetProfileAsync(Guid userId)
    {
        var user = await GetUserAsync(userId);
        var profile = await GetOrCreateProfileAsync(userId);

        var roles = await users.GetRolesAsync(user);

        return MapProfile(user, profile, roles);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(
        Guid userId,
        UpdateProfileDto dto)
    {
        var user = await GetUserAsync(userId);
        var profile = await GetOrCreateProfileAsync(userId);

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Name is required.");

        user.FullName = dto.Name.Trim();

        var identityResult = await users.UpdateAsync(user);

        if (!identityResult.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ",
                    identityResult.Errors.Select(x => x.Description)));
        }

        profile.DateOfBirth = dto.DateOfBirth;
        profile.Gender = Clean(dto.Gender);
        profile.Division = Clean(dto.Division);
        profile.District = Clean(dto.District);
        profile.Area = Clean(dto.Area);
        profile.AddressDetails = Clean(dto.AddressDetails);
        profile.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var roles = await users.GetRolesAsync(user);

        return MapProfile(user, profile, roles);
    }

    public async Task<string> UploadProfileImageAsync(
        Guid userId,
        IFormFile file)
    {
        var user = await GetUserAsync(userId);
        var profile = await GetOrCreateProfileAsync(userId);

        if (file == null || file.Length == 0)
            throw new InvalidOperationException("Please select an image.");

        if (file.Length > MaxImageSize)
            throw new InvalidOperationException(
                "Image size cannot exceed 5 MB.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

        if (!allowedExtensions.Contains(extension))
            throw new InvalidOperationException("Unsupported image format.");

        // Validate file signature, not just the extension.
        await using var stream = file.OpenReadStream();
        var header = new byte[12];
        var bytesRead = await stream.ReadAsync(header);

        var isValidImage =
            (extension is ".jpg" or ".jpeg"
                && bytesRead >= 3
                && header[0] == 0xFF
                && header[1] == 0xD8
                && header[2] == 0xFF)
            ||
            (extension == ".png"
                && bytesRead >= 8
                && header.Take(8).SequenceEqual(
                    new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            ||
            (extension == ".webp"
                && bytesRead >= 12
                && header[0] == (byte)'R'
                && header[1] == (byte)'I'
                && header[2] == (byte)'F'
                && header[3] == (byte)'F'
                && header[8] == (byte)'W'
                && header[9] == (byte)'E'
                && header[10] == (byte)'B'
                && header[11] == (byte)'P'
                );

        if (!isValidImage)
            throw new InvalidOperationException("Invalid image file.");

        var webRoot = environment.WebRootPath
            ?? Path.Combine(environment.ContentRootPath, "wwwroot");

        var uploadFolder = Path.Combine(
            webRoot, "uploads", "profiles");

        Directory.CreateDirectory(uploadFolder);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadFolder, fileName);

        await using (var output = new FileStream(
            filePath, FileMode.CreateNew))
        {
            stream.Position = 0;
            await stream.CopyToAsync(output);
        }

        // Delete previous image only if it belongs to this upload folder.
        DeletePreviousImage(profile.ProfileImageUrl, uploadFolder);

        profile.ProfileImageUrl = $"/uploads/profiles/{fileName}";
        profile.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return profile.ProfileImageUrl;
    }

    public async Task RequestAccountDeletionAsync(Guid userId)
    {
        var user = await GetUserAsync(userId);
        var profile = await GetOrCreateProfileAsync(userId);

        if (profile.DeletionRequestedAt != null)
            throw new InvalidOperationException(
                "Account deletion has already been requested.");

        var now = DateTime.UtcNow;

        profile.DeletionRequestedAt = now;
        profile.UpdatedAt = now;

        // Revoke all refresh-token sessions.
        var sessions = await db.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ToListAsync();

        foreach (var session in sessions)
        {
            session.RevokedAt = now;
        }

        await db.SaveChangesAsync();
    }

    public async Task<List<UserSessionDto>> GetSessionsAsync(Guid userId)
    {
        await GetUserAsync(userId);

        return await db.RefreshTokens
            .Where(x =>
                x.UserId == userId &&
                x.RevokedAt == null &&
                x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new UserSessionDto
            {
                Id = x.Id,
                DeviceId = x.DeviceId,
                DeviceName = x.DeviceName,
                Platform = x.Platform,
                CreatedAt = x.CreatedAt,
                ExpiresAt = x.ExpiresAt
            })
            .ToListAsync();
    }

    private async Task<ApplicationUser> GetUserAsync(Guid userId)
    {
        var user = await users.FindByIdAsync(userId.ToString());

        if (user == null)
            throw new KeyNotFoundException("User not found.");

        return user;
    }

    private async Task<UserProfile> GetOrCreateProfileAsync(Guid userId)
    {
        var profile = await db.UserProfiles
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (profile != null)
            return profile;

        var now = DateTime.UtcNow;

        profile = new UserProfile
        {
            UserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.UserProfiles.Add(profile);
        await db.SaveChangesAsync();

        return profile;
    }

    private static UserProfileDto MapProfile(
        ApplicationUser user,
        UserProfile profile,
        IList<string> roles)
    {
        var complete =
            !string.IsNullOrWhiteSpace(user.FullName) &&
            profile.DateOfBirth.HasValue &&
            !string.IsNullOrWhiteSpace(profile.Gender) &&
            !string.IsNullOrWhiteSpace(profile.Division) &&
            !string.IsNullOrWhiteSpace(profile.District) &&
            !string.IsNullOrWhiteSpace(profile.Area);

        return new UserProfileDto
        {
            Id = user.Id,
            Name = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            DateOfBirth = profile.DateOfBirth,
            Gender = profile.Gender,
            Division = profile.Division,
            District = profile.District,
            Area = profile.Area,
            AddressDetails = profile.AddressDetails,
            ProfileImageUrl = profile.ProfileImageUrl,
            Roles = roles.ToList(),
            IsProfileComplete = complete,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static void DeletePreviousImage(
        string? imageUrl,
        string uploadFolder)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return;

        var fileName = Path.GetFileName(imageUrl);

        if (string.IsNullOrWhiteSpace(fileName))
            return;

        var oldPath = Path.GetFullPath(
            Path.Combine(uploadFolder, fileName));

        var folderPath = Path.GetFullPath(uploadFolder)
            + Path.DirectorySeparatorChar;

        if (oldPath.StartsWith(
                folderPath,
                StringComparison.OrdinalIgnoreCase)
            && File.Exists(oldPath))
        {
            File.Delete(oldPath);
        }
    }

    // GET: api/users/me/roles
    public async Task<UserRolesResponseDto> GetRolesAsync(Guid userId)
    {
        var user = await GetUserAsync(userId);

        var userRoles = await users.GetRolesAsync(user);

        var result = new UserRolesResponseDto
        {
            ActiveRole = user.ActiveRole,
            Roles = new List<RoleResponseDto>()
        };

        var availableRoles = new[] { "Student", "Teacher", "Admin" };

        foreach (var role in availableRoles)
        {
            var enabled = userRoles.Contains(
                role,
                StringComparer.OrdinalIgnoreCase);

            string? verificationStatus = null;

            if (role == "Teacher" && enabled)
            {
                var tutorProfile = await db.Set<TutorProfile>()
                    .FirstOrDefaultAsync(x => x.UserId == userId);

                verificationStatus = tutorProfile?
                    .VerificationStatus.ToString()
                    .ToUpperInvariant();
            }

            result.Roles.Add(new RoleResponseDto
            {
                Role = role.ToUpperInvariant(),
                Enabled = enabled,
                VerificationStatus = verificationStatus
            });
        }

        return result;
    }

    // POST: api/users/me/roles
    public async Task<object> ActivateRoleAsync(
        Guid userId,
        ActivateRoleRequestDto dto)
    {
        var user = await GetUserAsync(userId);

        var requestedRole = NormalizeRole(dto.Role);

        if (requestedRole != "Teacher")
        {
            throw new InvalidOperationException(
                "Only the Teacher role can be activated through this API.");
        }

        if (!await roleManager.RoleExistsAsync(requestedRole))
        {
            throw new InvalidOperationException(
                "Teacher role does not exist. Please seed the role first.");
        }

        var existingRoles = await users.GetRolesAsync(user);

        if (existingRoles.Contains(
            requestedRole,
            StringComparer.OrdinalIgnoreCase))
        {
            return new
            {
                role = "TEACHER",
                profileStatus = "ALREADY_ACTIVATED"
            };
        }

        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var identityResult = await users.AddToRoleAsync(
            user,
            requestedRole);

        if (!identityResult.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ",
                    identityResult.Errors.Select(x => x.Description)));
        }

        var tutorProfile = await db.Set<TutorProfile>()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (tutorProfile == null)
        {
            tutorProfile = new TutorProfile
            {
                UserId = userId,
                Headline = string.Empty,
                Bio = string.Empty,
                HourlyRate = 0,
                TeachesOnline = true,
                VerificationStatus =
                    Domain.Enums.VerificationStatus.Pending
            };

            db.Set<TutorProfile>().Add(tutorProfile);

            await db.SaveChangesAsync();
        }

        await transaction.CommitAsync();

        return new
        {
            role = "TEACHER",
            profileStatus = "INCOMPLETE"
        };
    }

    // PUT: api/users/me/active-role
    public async Task<string> SwitchActiveRoleAsync(
        Guid userId,
        SwitchActiveRoleRequestDto dto)
    {
        var user = await GetUserAsync(userId);

        var requestedRole = NormalizeRole(dto.Role);

        var existingRoles = await users.GetRolesAsync(user);

        if (!existingRoles.Contains(
            requestedRole,
            StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"You do not have the {requestedRole} role activated.");
        }

        user.ActiveRole = requestedRole.ToUpperInvariant();

        var result = await users.UpdateAsync(user);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ",
                    result.Errors.Select(x => x.Description)));
        }

        return user.ActiveRole;
    }

    private static string NormalizeRole(string? role)
    {
        return role?.Trim().ToUpperInvariant() switch
        {
            "STUDENT" => "Student",
            "TEACHER" => "Teacher",
            _ => throw new InvalidOperationException(
                "Invalid role. Allowed roles: STUDENT, TEACHER.")
        };
    }

}
