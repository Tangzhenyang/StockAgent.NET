namespace StockAgent.Api.Features.Auth;

/// <summary>
/// Request body for creating a local application account.
/// 创建本地应用账号的请求体。
/// </summary>
/// <param name="UserName">Unique login name entered by the user. 用户输入的唯一登录名。</param>
/// <param name="Password">Plain-text password submitted over HTTPS. 通过 HTTPS 提交的明文密码。</param>
public sealed record RegisterRequest(string UserName, string Password);

/// <summary>
/// Request body for signing in with a local account.
/// 使用本地账号登录的请求体。
/// </summary>
/// <param name="UserName">Login name. 登录名。</param>
/// <param name="Password">Plain-text password submitted over HTTPS. 通过 HTTPS 提交的明文密码。</param>
public sealed record LoginRequest(string UserName, string Password);

/// <summary>
/// Sanitized account payload returned to the browser.
/// 返回给浏览器的脱敏账号载荷。
/// </summary>
/// <param name="Id">Identity user identifier. Identity 用户标识符。</param>
/// <param name="UserName">Display/login name. 展示和登录名称。</param>
/// <param name="IsAuthenticated">Whether the request is authenticated. 请求是否已认证。</param>
public sealed record CurrentUserResponse(string Id, string UserName, bool IsAuthenticated);

/// <summary>
/// Small API error payload for authentication failures.
/// 认证失败时使用的小型 API 错误载荷。
/// </summary>
/// <param name="Error">Human-readable error summary. 人类可读的错误摘要。</param>
public sealed record AuthErrorResponse(string Error);
