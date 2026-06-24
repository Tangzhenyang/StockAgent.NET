namespace StockAgent.Api.Domain;

/// <summary>JSON-backed application setting for provider and research configuration. 用于提供器和研究配置的 JSON 应用设置。</summary>
public sealed class AppSetting
{
    /// <summary>Unique setting identifier. 唯一设置标识符。</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Stable setting key. 稳定的设置键。</summary>
    public string SettingKey { get; set; } = string.Empty;
    /// <summary>Serialized JSON setting value. 序列化后的 JSON 设置值。</summary>
    public string SettingValueJson { get; set; } = "{}";
    /// <summary>UTC timestamp when the setting was last changed. 设置最后变更的 UTC 时间戳。</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
