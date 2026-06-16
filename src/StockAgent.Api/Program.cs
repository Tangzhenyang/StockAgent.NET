using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using StockAgent.Api.Features.Evidence;
using StockAgent.Api.Features.Health;
using StockAgent.Api.Features.Pdf;
using StockAgent.Api.Features.Reports;
using StockAgent.Api.Features.ResearchTasks;
using StockAgent.Api.Features.Settings;
using StockAgent.Api.Infrastructure.Ai;
using StockAgent.Api.Infrastructure.DataSources;
using StockAgent.Api.Infrastructure.Documents;
using StockAgent.Api.Infrastructure.Pdf;
using StockAgent.Api.Infrastructure.Persistence;
using StockAgent.Api.Infrastructure.Queueing;
using StockAgent.Api.Infrastructure.Reports;
using StockAgent.Api.Infrastructure.Research;

const string frontendCorsPolicy = "StockAgentFrontend";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:5174",
                "http://127.0.0.1:5173",
                "http://127.0.0.1:5174")
            .AllowAnyHeader()
            .AllowAnyMethod();
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
builder.Services.AddSingleton<IResearchTaskQueue, ResearchTaskQueue>();
builder.Services.AddScoped<IMarketDataProvider, FakeMarketDataProvider>();
builder.Services.AddScoped<IWebResearchProvider, FakeWebResearchProvider>();
builder.Services.AddScoped<DocumentChunker>();
builder.Services.AddScoped<ContextBudgetManager>();
builder.Services.AddScoped<ResearchOrchestrator>();
builder.Services.AddSingleton(_ => Kernel.CreateBuilder().Build());
builder.Services.AddScoped<IResearchAnalysisService, SemanticKernelResearchAnalysisService>();
builder.Services.AddScoped<ReportGenerator>();
builder.Services.AddScoped<IPdfExportService, PlaywrightPdfExportService>();
builder.Services.AddHostedService<ResearchWorker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<StockAgentDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseHttpsRedirection();
app.UseCors(frontendCorsPolicy);

app.MapResearchTaskEndpoints();
app.MapReportEndpoints();
app.MapEvidenceEndpoints();
app.MapPdfEndpoints();
app.MapSettingsEndpoints();
app.MapDataSourceHealthEndpoints();

app.Run();

/// <summary>Marker type used by WebApplicationFactory integration tests.</summary>
public partial class Program;
