namespace StockAgent.Api.Infrastructure.Security;

/// <summary>
/// Accessor for the authenticated user bound to the current HTTP request.
/// 当前 HTTP 请求中已认证用户的访问器。
/// </summary>
public interface ICurrentUser
{
    /// <summary>Current user identifier, or null when anonymous. 当前用户标识符；匿名时为空。</summary>
    string? UserId { get; }
    /// <summary>Returns the current user identifier or throws when no user is authenticated. 返回当前用户标识符；未认证时抛出异常。</summary>
    string RequireUserId();
}
