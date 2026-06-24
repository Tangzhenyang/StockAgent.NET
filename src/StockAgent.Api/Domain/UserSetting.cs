namespace StockAgent.Api.Domain;

/// <summary>
/// Per-user JSON-backed configuration entry for model, research, and data-source settings.
/// 按用户保存的 JSON 配置项，用于模型、研究和数据源设置。
/// </summary>
public sealed class UserSetting
{
    /// <summary>Unique setting identifier. 唯一设置标识符。</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Owner user identifier from ASP.NET Core Identity. ASP.NET Core Identity 中的所属用户标识符。</summary>
    public string UserId { get; set; } = string.Empty;
    /// <summary>Owner user navigation property. 所属用户导航属性。</summary>
    public ApplicationUser? User { get; set; }
    /// <summary>Stable setting key, such as model or research. 稳定设置键，例如 model 或 research。</summary>
    public string SettingKey { get; set; } = string.Empty;
    /// <summary>Serialized JSON setting value. 序列化后的 JSON 设置值。</summary>
    public string SettingValueJson { get; set; } = "{}";
    /// <summary>UTC timestamp when the setting was last changed. 设置最后变更的 UTC 时间戳。</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
