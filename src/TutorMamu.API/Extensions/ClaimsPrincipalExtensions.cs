using System.Security.Claims;
using TutorMamu.Application.Common;

namespace TutorMamu.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : throw new AppException("Invalid token.", 401);
    }
}
