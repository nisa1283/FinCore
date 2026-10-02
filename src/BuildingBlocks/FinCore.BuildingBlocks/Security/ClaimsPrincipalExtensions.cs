using System.Security.Claims;
using FinCore.BuildingBlocks.Exceptions;

namespace FinCore.BuildingBlocks.Security;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst("sub")?.Value;
        return Guid.TryParse(value, out var id)
            ? id
            : throw new UnauthorizedAppException("Invalid token.");
    }
}