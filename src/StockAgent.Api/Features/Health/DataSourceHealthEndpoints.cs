namespace StockAgent.Api.Features.Health;

/// <summary>
/// Endpoints that expose first-version data source health.
/// 暴露首个版本数据源健康状态的端点。
/// </summary>
public static class DataSourceHealthEndpoints
{
    /// <summary>Maps data-source health endpoints. 映射数据源健康端点。</summary>
    public static IEndpointRouteBuilder MapDataSourceHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health/data-sources", () => Results.Ok(new[] { new { name = "FakeProvider", status = "Healthy" } }))
            .WithTags("Health")
            .RequireAuthorization();
        return app;
    }
}
