using System.Security.Claims;

namespace StockAgent.Api.Infrastructure.Security;

/// <summary>
/// HTTP-context backed current-user accessor.
/// 基于 HTTP 上下文的当前用户访问器。
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    /// <inheritdoc />
    public string? UserId => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <inheritdoc />
    public string RequireUserId()
    {
        return UserId ?? throw new InvalidOperationException("The current request is not authenticated.");
    }
}
