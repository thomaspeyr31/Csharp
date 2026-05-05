using System.Security.Claims;

namespace TaskBoard.Security;

public static class CurrentUserExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException("Missing 'sub' claim on the current principal.");

        return int.Parse(sub);
    }
}
