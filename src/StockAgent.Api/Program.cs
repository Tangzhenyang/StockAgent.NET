using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockAgent.Api.Features.Auth;
using StockAgent.Api.Domain;
using StockAgent.Api.Features.Evidence;
using StockAgent.Api.Features.Health;
using StockAgent.Api.Features.Pdf;
using StockAgent.Api.Features.Reports;
using StockAgent.Api.Features.ResearchTasks;
using StockAgent.Api.Features.Settings;
using StockAgent.Api.Features.UserSettings;
using StockAgent.Api.Infrastructure.Ai;
using StockAgent.Api.Infrastructure.Ai.Agents;
using StockAgent.Api.Infrastructure.Ai.Chat;
using StockAgent.Api.Infrastructure.DataSources;
using StockAgent.Api.Infrastructure.Documents;
using StockAgent.Api.Infrastructure.Pdf;
using StockAgent.Api.Infrastructure.Persistence;
using StockAgent.Api.Infrastructure.Queueing;
using StockAgent.Api.Infrastructure.Reports;
using StockAgent.Api.Infrastructure.Research;
using StockAgent.Api.Infrastructure.Security;
using StockAgent.Api.Infrastructure.Settings;

const string frontendCorsPolicy = "StockAgentFrontend";

var builder = WebApplication.CreateBuilder(args);

// Keep self-hosted logging independent from Windows Event Log permissions.
// 让自部署日志不依赖 Windows 事件日志权限。
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
// 将服务添加到容器中。
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// 了解更多关于配置 OpenAPI 的信息：https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendCorsPolicy, policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                             ?? new[]
                             {
                                 "http://localhost:5173",
                                 "http://localhost:5174",
                                 "http://127.0.0.1:5173",
                                 "http://127.0.0.1:5174"
                             };

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddDbContext<StockAgentDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("StockAgent");
    options.UseNpgsql(connectionString);
});
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = false;
        options.SignIn.RequireConfirmedAccount = false;
        options.SignIn.RequireConfirmedEmail = false;
        options.SignIn.RequireConfirmedPhoneNumber = false;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireDigit = false;
    })
    .AddEntityFrameworkStores<StockAgentDbContext>()
    .AddSignInManager();
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = "StockAgent.Auth";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthorization();
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"]
                             ?? Path.Combine(builder.Environment.ContentRootPath, ".data-protection-keys");
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services
    .AddDataProtection()
    .SetApplicationName("StockAgent.NET")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IApiKeyProtector, DataProtectionApiKeyProtector>();
builder.Services.AddScoped<UserSettingsService>();
builder.Services.AddSingleton<IResearchTaskQueue, ResearchTaskQueue>();
builder.Services.AddScoped<FakeMarketDataProvider>();
builder.Services.AddScoped<FakeWebResearchProvider>();
builder.Services.AddScoped<FakeIndustryResearchProvider>();
builder.Services.AddHttpClient<IMarketDataProvider, ConfiguredMarketDataProvider>(client =>
{
    // Keep the first research step responsive; the A-share data gateway uses fast single-stock quote calls.
    // 保持研究第一步快速失败；A 股数据网关使用快速单股行情接口。
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<IWebResearchProvider, ConfiguredWebResearchProvider>();
builder.Services.AddHttpClient<IIndustryResearchProvider, ConfiguredIndustryResearchProvider>();
builder.Services.AddScoped<DocumentChunker>();
builder.Services.AddScoped<ContextBudgetManager>();
builder.Services.AddScoped<ResearchOrchestrator>();
builder.Services.AddScoped<IModelChatClient, SemanticKernelModelChatClient>();
builder.Services.AddSingleton(new AgentContextBudgetOptions());
builder.Services.AddScoped<AgentContextBudgeter>();
builder.Services.AddScoped<IResearchAnalysisService, SemanticKernelResearchAnalysisService>();
builder.Services.AddScoped<ReportGenerator>();
builder.Services.AddScoped<IPdfExportService, PlaywrightPdfExportService>();
builder.Services.AddHostedService<ResearchWorker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
// 配置 HTTP 请求管道。
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<StockAgentDbContext>();
    if (db.Database.IsRelational())
    {
        await db.Database.MigrateAsync();
    }
    else
    {
        await db.Database.EnsureCreatedAsync();
    }
}

app.UseHttpsRedirection();
app.UseCors(frontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapResearchTaskEndpoints();
app.MapReportEndpoints();
app.MapEvidenceEndpoints();
app.MapPdfEndpoints();
app.MapSettingsEndpoints();
app.MapUserSettingsEndpoints();
app.MapDataSourceHealthEndpoints();

app.Run();

/// <summary>
/// Marker type used by WebApplicationFactory integration tests.
/// 用于 WebApplicationFactory 集成测试的标记类型。
/// </summary>
public partial class Program;
