namespace StockAgent.Api.Domain;

/// <summary>JSON-backed application setting for provider and research configuration.</summary>
public sealed class AppSetting
{
    /// <summary>Unique setting identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Stable setting key.</summary>
    public string SettingKey { get; set; } = string.Empty;
    /// <summary>Serialized JSON setting value.</summary>
    public string SettingValueJson { get; set; } = "{}";
    /// <summary>UTC timestamp when the setting was last changed.</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
