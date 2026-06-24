using Microsoft.AspNetCore.Identity;

namespace StockAgent.Api.Domain;

/// <summary>
/// Application account used to isolate settings and research history per user.
/// 应用账号，用于按用户隔离配置和研究历史。
/// </summary>
public sealed class ApplicationUser : IdentityUser
{
    /// <summary>UTC timestamp when the account was created. 账号创建时的 UTC 时间戳。</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>UTC timestamp when the account was last updated. 账号最后更新时的 UTC 时间戳。</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
