using StockAgent.Api.Infrastructure.Security;
using StockAgent.Api.Infrastructure.Settings;

namespace StockAgent.Api.Features.UserSettings;

/// <summary>
/// Endpoints for reading and updating the current user's model and research settings.
/// 用于读取和更新当前用户模型及研究设置的端点。
/// </summary>
public static class UserSettingsEndpoints
{
    /// <summary>Maps current-user settings endpoints. 映射当前用户设置端点。</summary>
    public static IEndpointRouteBuilder MapUserSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/user-settings").WithTags("User Settings").RequireAuthorization();

        group.MapGet("/", async (
            ICurrentUser currentUser,
            UserSettingsService settingsService,
            CancellationToken cancellationToken) =>
        {
            var settings = await settingsService.GetSettingsAsync(currentUser.RequireUserId(), cancellationToken);
            return Results.Ok(settings);
        });

        group.MapPut("/model", async (
            SaveModelSettingsRequest request,
            ICurrentUser currentUser,
            UserSettingsService settingsService,
            CancellationToken cancellationToken) =>
        {
            var validationError = ValidateModelRequest(request);
            if (validationError is not null)
            {
                return Results.BadRequest(new { error = validationError });
            }

            var settings = await settingsService.SaveModelSettingsAsync(
                currentUser.RequireUserId(),
                request,
                cancellationToken);
            return Results.Ok(settings);
        });

        group.MapPut("/research", async (
            SaveResearchSettingsRequest request,
            ICurrentUser currentUser,
            UserSettingsService settingsService,
            CancellationToken cancellationToken) =>
        {
            var validationError = ValidateResearchRequest(request);
            if (validationError is not null)
            {
                return Results.BadRequest(new { error = validationError });
            }

            var settings = await settingsService.SaveResearchSettingsAsync(
                currentUser.RequireUserId(),
                request,
                cancellationToken);
            return Results.Ok(settings);
        });

        group.MapPut("/data-sources", async (
            SaveDataSourceSettingsRequest request,
            ICurrentUser currentUser,
            UserSettingsService settingsService,
            CancellationToken cancellationToken) =>
        {
            var validationError = ValidateDataSourceRequest(request);
            if (validationError is not null)
            {
                return Results.BadRequest(new { error = validationError });
            }

            var settings = await settingsService.SaveDataSourceSettingsAsync(
                currentUser.RequireUserId(),
                request,
                cancellationToken);
            return Results.Ok(settings);
        });

        group.MapPost("/data-sources/test", async (
            ICurrentUser currentUser,
            UserSettingsService settingsService,
            CancellationToken cancellationToken) =>
        {
            var settings = await settingsService.GetDataSourceSettingsAsync(currentUser.RequireUserId(), cancellationToken);
            var complete = IsConfiguredProviderComplete(settings.MarketDataProvider, settings.MarketDataBaseUrl) &&
                           IsConfiguredProviderComplete(settings.WebResearchProvider, settings.WebResearchBaseUrl);
            return Results.Ok(new DataSourceSettingsTestResponse(
                complete,
                complete ? "数据源配置完整。" : "CustomHttp 数据源需要填写 Base URL。"));
        });

        group.MapPost("/model/test", async (
            ICurrentUser currentUser,
            UserSettingsService settingsService,
            CancellationToken cancellationToken) =>
        {
            var model = await settingsService.GetModelSettingsAsync(currentUser.RequireUserId(), cancellationToken);
            var complete = !string.IsNullOrWhiteSpace(model.Provider) &&
                           !string.IsNullOrWhiteSpace(model.BaseUrl) &&
                           !string.IsNullOrWhiteSpace(model.Model) &&
                           model.ApiKeyConfigured;
            return Results.Ok(new ModelSettingsTestResponse(
                complete,
                complete ? "模型配置完整。" : "请先填写提供商、Base URL、模型名称和 API Key。"));
        });

        return app;
    }

    private static string? ValidateModelRequest(SaveModelSettingsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Provider))
        {
            return "Provider is required.";
        }

        if (string.IsNullOrWhiteSpace(request.BaseUrl))
        {
            return "Base URL is required.";
        }

        return string.IsNullOrWhiteSpace(request.Model) ? "Model is required." : null;
    }

    private static string? ValidateResearchRequest(SaveResearchSettingsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DefaultLanguage))
        {
            return "Default language is required.";
        }

        if (request.MaxEvidenceCards is < 1 or > 200)
        {
            return "Max evidence cards must be between 1 and 200.";
        }

        if (request.MaxDocumentChunks is < 1 or > 5000)
        {
            return "Max document chunks must be between 1 and 5000.";
        }

        return request.MaxRetrievedChunks is < 1 or > 1000
            ? "Max retrieved chunks must be between 1 and 1000."
            : null;
    }

    private static string? ValidateDataSourceRequest(SaveDataSourceSettingsRequest request)
    {
        var marketProviderError = ValidateProvider("Market data provider", request.MarketDataProvider);
        if (marketProviderError is not null)
        {
            return marketProviderError;
        }

        var webProviderError = ValidateProvider("Web research provider", request.WebResearchProvider);
        if (webProviderError is not null)
        {
            return webProviderError;
        }

        if (IsCustomHttp(request.MarketDataProvider) && !IsValidHttpUrl(request.MarketDataBaseUrl))
        {
            return "Market data Base URL must be a valid HTTP or HTTPS URL when CustomHttp is selected.";
        }

        if (IsCustomHttp(request.WebResearchProvider) && !IsValidHttpUrl(request.WebResearchBaseUrl))
        {
            return "Web research Base URL must be a valid HTTP or HTTPS URL when CustomHttp is selected.";
        }

        if (request.MaxRequestsPerMinute is < 1 or > 600)
        {
            return "Max requests per minute must be between 1 and 600.";
        }

        return request.RetryCount is < 0 or > 5 ? "Retry count must be between 0 and 5." : null;
    }

    private static string? ValidateProvider(string name, string provider)
    {
        return IsMock(provider) || IsCustomHttp(provider) ? null : $"{name} must be Mock or CustomHttp.";
    }

    private static bool IsConfiguredProviderComplete(string provider, string baseUrl)
    {
        return IsMock(provider) || IsValidHttpUrl(baseUrl);
    }

    private static bool IsMock(string provider)
    {
        return string.Equals(provider, "Mock", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCustomHttp(string provider)
    {
        return string.Equals(provider, "CustomHttp", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
