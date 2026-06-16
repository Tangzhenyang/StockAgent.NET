namespace StockAgent.Api.Features.Settings;

/// <summary>
/// Endpoints for first-version provider and research settings.
/// </summary>
public static class SettingsEndpoints
{
    /// <summary>Maps settings endpoints.</summary>
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/settings/providers", () => Results.Ok(new { openAiEnabled = false, compatibleEnabled = true }))
            .WithTags("Settings");
        app.MapGet("/api/settings/research", () => Results.Ok(new { defaultLanguage = "zh-CN", maxEvidenceCards = 30 }))
            .WithTags("Settings");
        return app;
    }
}
