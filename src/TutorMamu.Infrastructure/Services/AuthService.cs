using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TutorMamu.Application.Common;
using TutorMamu.Application.DTOs;
using TutorMamu.Application.DTOs.Auth;
using TutorMamu.Application.Interfaces;
using TutorMamu.Domain.Entities;
using TutorMamu.Infrastructure.Data;

namespace TutorMamu.Infrastructure.Services;

public class AuthService(
    UserManager<ApplicationUser> users,
    ITokenService tokens,
    IRepository<TutorProfile> profiles,
    ApplicationDbContext db,
    ISmsService sms) : IAuthService
{
    private const int OtpExpiryMinutes = 5;
    private const int ResendCooldownSeconds = 60;
    private const int MaxOtpAttempts = 5;
    private const int RefreshTokenExpiryDays = 30;

    // =========================================
    // REGISTER
    // =========================================

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var role = dto.Role?.Trim();

        if (!string.Equals(role, Roles.Student,
                StringComparison.OrdinalIgnoreCase)
            && !string.Equals(role, Roles.Teacher,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException("Role must be Student or Teacher.");
        }

        role = string.Equals(role, Roles.Teacher,
            StringComparison.OrdinalIgnoreCase)
            ? Roles.Teacher
            : Roles.Student;

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName.Trim(),
            PhoneNumber = dto.PhoneNumber
        };

        var result = await users.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
        {
            throw new AppException(
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        var roleResult = await users.AddToRoleAsync(user, role);

        if (!roleResult.Succeeded)
        {
            await users.DeleteAsync(user);

            throw new AppException(
                string.Join("; ", roleResult.Errors.Select(e => e.Description)));
        }

        // Create separate user profile
        db.UserProfiles.Add(new UserProfile
        {
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // Create tutor-specific profile
        if (role == Roles.Teacher)
        {
            await profiles.AddAsync(new TutorProfile
            {
                UserId = user.Id
            });

            await profiles.SaveChangesAsync();
        }

        await db.SaveChangesAsync();

        return await CreateAuthenticatedResponseAsync(
            user, await users.GetRolesAsync(user));
    }

    // =========================================
    // LOGIN
    // =========================================

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await users.FindByEmailAsync(dto.Email);

        if (user == null ||
            !await users.CheckPasswordAsync(user, dto.Password))
        {
            throw new AppException("Invalid email or password.", 401);
        }

        var roles = await users.GetRolesAsync(user);

        return await CreateAuthenticatedResponseAsync(user, roles);
    }

    // =========================================
    // SEND OTP
    // =========================================

    public async Task<SendOtpResponseDto> SendOtpAsync(
        SendOtpRequestDto dto)
    {
        var phone = NormalizePhone(dto.PhoneNumber);

        if (!string.Equals(dto.Purpose, "LOGIN",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException("Only LOGIN OTP is currently supported.");
        }

        var user = await users.Users
            .FirstOrDefaultAsync(x => x.PhoneNumber == phone
                || x.PhoneNumber == dto.PhoneNumber);

        if (user == null)
        {
            throw new AppException(
                "No account found with this phone number.", 404);
        }

        var now = DateTime.UtcNow;
        var otp = GenerateOtp();

        var verification = new OtpVerification
        {
            VerificationId = Guid.NewGuid().ToString("N"),
            PhoneNumber = phone,
            OtpHash = Hash(otp),
            Purpose = "LOGIN",
            ExpiresAt = now.AddMinutes(OtpExpiryMinutes),
            ResendAvailableAt = now.AddSeconds(ResendCooldownSeconds),
            Attempts = 0,
            IsVerified = false,
            CreatedAt = now
        };

        await sms.SendSmsAsync(
            phone,
            $"Your TutorMamu verification code is {otp}. It expires in 5 minutes.");

        db.OtpVerifications.Add(verification);
        await db.SaveChangesAsync();

        return new SendOtpResponseDto
        {
            VerificationId = verification.VerificationId,
            MaskedPhone = MaskPhone(phone),
            ExpiresIn = OtpExpiryMinutes * 60,
            ResendAvailableIn = ResendCooldownSeconds
        };
    }

    // =========================================
    // VERIFY OTP
    // =========================================

    public async Task<AuthResponseDto> VerifyOtpAsync(
        VerifyOtpRequestDto dto)
    {
        var verification = await db.OtpVerifications
            .FirstOrDefaultAsync(x =>
                x.VerificationId == dto.VerificationId);

        if (verification == null)
            throw new AppException("Invalid verification request.", 400);

        if (verification.IsVerified)
            throw new AppException("OTP has already been used.", 400);

        if (verification.ExpiresAt <= DateTime.UtcNow)
            throw new AppException("OTP has expired.", 400);

        if (verification.Attempts >= MaxOtpAttempts)
            throw new AppException("Maximum OTP attempts exceeded.", 400);

        verification.Attempts++;

        var suppliedHash = Hash(dto.Otp);

        if (!FixedTimeEquals(verification.OtpHash, suppliedHash))
        {
            await db.SaveChangesAsync();
            throw new AppException("Invalid OTP.", 400);
        }

        verification.IsVerified = true;

        var user = await users.Users.FirstOrDefaultAsync(
            x => x.PhoneNumber == verification.PhoneNumber
                 || x.PhoneNumber == FormatLocalPhone(verification.PhoneNumber));

        if (user == null)
        {
            await db.SaveChangesAsync();
            throw new AppException("User account not found.", 404);
        }

        user.PhoneNumberConfirmed = true;

        await users.UpdateAsync(user);
        await db.SaveChangesAsync();

        var roles = await users.GetRolesAsync(user);

        return await CreateAuthenticatedResponseAsync(
            user, roles, dto.Device);
    }

    // =========================================
    // RESEND OTP
    // =========================================

    public async Task<SendOtpResponseDto> ResendOtpAsync(
        ResendOtpRequestDto dto)
    {
        var verification = await db.OtpVerifications
            .FirstOrDefaultAsync(x =>
                x.VerificationId == dto.VerificationId);

        if (verification == null)
            throw new AppException("Invalid verification request.", 400);

        if (verification.IsVerified)
            throw new AppException("OTP has already been verified.", 400);

        var now = DateTime.UtcNow;

        if (verification.ExpiresAt <= now)
            throw new AppException("OTP has expired. Please request a new OTP.", 400);

        if (verification.ResendAvailableAt > now)
        {
            var remaining = (int)Math.Ceiling(
                (verification.ResendAvailableAt - now).TotalSeconds);

            throw new AppException(
                $"Please wait {remaining} seconds before requesting another OTP.");
        }

        var otp = GenerateOtp();

        await sms.SendSmsAsync(
            verification.PhoneNumber,
            $"Your new TutorMamu verification code is {otp}. It expires in 5 minutes.");

        verification.OtpHash = Hash(otp);
        verification.Attempts = 0;
        verification.ExpiresAt = now.AddMinutes(OtpExpiryMinutes);
        verification.ResendAvailableAt =
            now.AddSeconds(ResendCooldownSeconds);

        await db.SaveChangesAsync();

        return new SendOtpResponseDto
        {
            VerificationId = verification.VerificationId,
            MaskedPhone = MaskPhone(verification.PhoneNumber),
            ExpiresIn = OtpExpiryMinutes * 60,
            ResendAvailableIn = ResendCooldownSeconds
        };
    }

    // =========================================
    // REFRESH TOKEN
    // =========================================

    public async Task<AuthResponseDto> RefreshTokenAsync(
        RefreshTokenRequestDto dto)
    {
        var oldHash = Hash(dto.RefreshToken);

        var storedToken = await db.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == oldHash);

        if (storedToken == null ||
            storedToken.RevokedAt != null ||
            storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new AppException("Invalid or expired refresh token.", 401);
        }

        var user = await users.FindByIdAsync(
            storedToken.UserId.ToString());

        if (user == null)
            throw new AppException("User not found.", 404);

        var roles = await users.GetRolesAsync(user);

        var newRawToken = GenerateRefreshToken();
        var newHash = Hash(newRawToken);

        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByTokenHash = newHash;

        var replacement = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = newHash,
            DeviceId = storedToken.DeviceId,
            DeviceName = storedToken.DeviceName,
            Platform = storedToken.Platform,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays)
        };

        db.RefreshTokens.Add(replacement);
        await db.SaveChangesAsync();

        var response = tokens.CreateToken(user, roles);
        response.RefreshToken = newRawToken;

        return response;
    }

    // =========================================
    // LOGOUT
    // =========================================

    public async Task LogoutAsync(LogoutRequestDto dto)
    {
        var hash = Hash(dto.RefreshToken);

        var storedToken = await db.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == hash);

        if (storedToken == null)
            throw new AppException("Refresh token not found.", 404);

        if (storedToken.RevokedAt == null)
        {
            storedToken.RevokedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    // =========================================
    // CURRENT USER
    // =========================================

    public async Task<AuthenticatedUserDto> GetCurrentUserAsync(
    Guid userId)
    {
        var user = await users.FindByIdAsync(userId.ToString());

        if (user == null)
            throw new AppException("User not found.", 404);

        var profile = await db.UserProfiles
            .FirstOrDefaultAsync(x => x.UserId == userId);

        var roles = await users.GetRolesAsync(user);

        var isProfileComplete = profile != null
            && !string.IsNullOrWhiteSpace(user.FullName)
            && profile.DateOfBirth.HasValue
            && !string.IsNullOrWhiteSpace(profile.Gender)
            && !string.IsNullOrWhiteSpace(profile.Division)
            && !string.IsNullOrWhiteSpace(profile.District)
            && !string.IsNullOrWhiteSpace(profile.Area);

        return new AuthenticatedUserDto
        {
            Id = user.Id,
            Name = user.FullName,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Roles = roles.ToList(),
            ActiveRole = roles.FirstOrDefault() ?? string.Empty,
            PhoneVerified = user.PhoneNumberConfirmed,
            IsProfileComplete = isProfileComplete,
            ProfileImage = profile?.ProfileImageUrl
        };
    }

    // =========================================
    // REVOKE OTHER SESSIONS
    // =========================================

    public async Task RevokeOtherSessionsAsync(
        Guid userId,
        RevokeSessionsRequestDto dto)
    {
        var currentHash = Hash(dto.CurrentRefreshToken);

        var currentToken = await db.RefreshTokens
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.TokenHash == currentHash &&
                x.RevokedAt == null &&
                x.ExpiresAt > DateTime.UtcNow);

        if (currentToken == null)
            throw new AppException("Current session is invalid.", 401);

        var otherTokens = await db.RefreshTokens
            .Where(x => x.UserId == userId
                && x.Id != currentToken.Id
                && x.RevokedAt == null)
            .ToListAsync();

        foreach (var token in otherTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }

    // =========================================
    // TOKEN HELPERS
    // =========================================

    private async Task<AuthResponseDto> CreateAuthenticatedResponseAsync(
        ApplicationUser user,
        IList<string> roles,
        DeviceDto? device = null)
    {
        var rawRefreshToken = GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = Hash(rawRefreshToken),
            DeviceId = Limit(device?.DeviceId, 255),
            DeviceName = Limit(device?.DeviceName, 255),
            Platform = Limit(device?.Platform, 30),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays)
        };

        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync();

        var response = tokens.CreateToken(user, roles);
        response.RefreshToken = rawRefreshToken;

        return response;
    }

    private static string GenerateOtp()
    {
        return RandomNumberGenerator
            .GetInt32(100000, 1000000)
            .ToString();
    }

    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(64));
    }

    private static string Hash(string value)
    {
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static bool FixedTimeEquals(string first, string second)
    {
        var firstBytes = Convert.FromHexString(first);
        var secondBytes = Convert.FromHexString(second);

        return CryptographicOperations.FixedTimeEquals(
            firstBytes, secondBytes);
    }

    private static string NormalizePhone(string phone)
    {
        var number = phone.Trim()
            .Replace("+", "")
            .Replace(" ", "")
            .Replace("-", "");

        if (number.StartsWith("01") && number.Length == 11)
            return "88" + number;

        if (number.StartsWith("8801") && number.Length == 13)
            return number;

        throw new AppException("Invalid Bangladesh mobile number.");
    }

    private static string FormatLocalPhone(string phone)
    {
        return phone.StartsWith("880")
            ? phone[2..]
            : phone;
    }

    private static string MaskPhone(string phone)
    {
        var number = phone.Length > 11
            ? phone[^11..]
            : phone;

        return number.Length >= 11
            ? $"{number[..3]}****{number[^4..]}"
            : "********";
    }

    private static string? Limit(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Length > maxLength
            ? value[..maxLength]
            : value;
    }
}