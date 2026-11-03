using Microsoft.AspNetCore.Identity;
using TutorMamu.Application.Common;
using TutorMamu.Application.DTOs;
using TutorMamu.Application.Interfaces;
using TutorMamu.Domain.Entities;

namespace TutorMamu.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly ITokenService _tokens;
    private readonly IRepository<TutorProfile> _profiles;

    public AuthService(UserManager<ApplicationUser> users, ITokenService tokens, IRepository<TutorProfile> profiles)
    {
        _users = users;
        _tokens = tokens;
        _profiles = profiles;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        // Self-registration is limited to Student and Teacher. Admins are seeded.
        var role = dto.Role?.Trim();
        if (!string.Equals(role, Roles.Student, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(role, Roles.Teacher, StringComparison.OrdinalIgnoreCase))
            throw new AppException("Role must be Student or Teacher.");
        role = string.Equals(role, Roles.Teacher, StringComparison.OrdinalIgnoreCase) ? Roles.Teacher : Roles.Student;

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName.Trim(),
            PhoneNumber = dto.PhoneNumber
        };

        var result = await _users.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            throw new AppException(string.Join("; ", result.Errors.Select(e => e.Description)));

        await _users.AddToRoleAsync(user, role);

        if (role == Roles.Teacher)
        {
            // Pending until an admin verifies documents and approves the profile.
            await _profiles.AddAsync(new TutorProfile { UserId = user.Id });
            await _profiles.SaveChangesAsync();
        }

        return _tokens.CreateToken(user, new List<string> { role });
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _users.FindByEmailAsync(dto.Email);
        if (user == null || !await _users.CheckPasswordAsync(user, dto.Password))
            throw new AppException("Invalid email or password.", 401);

        var roles = await _users.GetRolesAsync(user);
        return _tokens.CreateToken(user, roles);
    }
}
