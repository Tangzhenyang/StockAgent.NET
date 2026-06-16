namespace StockAgent.Api.Features.Health;

/// <summary>
/// Endpoints that expose first-version data source health.
/// </summary>
public static class DataSourceHealthEndpoints
{
    /// <summary>Maps data-source health endpoints.</summary>
    public static IEndpointRouteBuilder MapDataSourceHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health/data-sources", () => Results.Ok(new[] { new { name = "FakeProvider", status = "Healthy" } }))
            .WithTags("Health");
        return app;
    }
}
