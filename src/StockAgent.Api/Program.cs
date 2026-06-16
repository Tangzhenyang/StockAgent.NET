using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using StockAgent.Api.Features.ResearchTasks;
using StockAgent.Api.Infrastructure.DataSources;
using StockAgent.Api.Infrastructure.Persistence;
using StockAgent.Api.Infrastructure.Queueing;
using StockAgent.Api.Infrastructure.Research;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
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
builder.Services.AddScoped<ResearchOrchestrator>();
builder.Services.AddHostedService<ResearchWorker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapResearchTaskEndpoints();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

/// <summary>Marker type used by WebApplicationFactory integration tests.</summary>
public partial class Program;
