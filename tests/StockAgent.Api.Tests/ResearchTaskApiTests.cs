using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StockAgent.Api.Domain;
using StockAgent.Api.Features.ResearchTasks;
using StockAgent.Api.Infrastructure.Persistence;

namespace StockAgent.Api.Tests;

/// <summary>
/// Verifies the research task API contract used by the React workbench.
/// </summary>
public sealed class ResearchTaskApiTests
{
    /// <summary>
    /// Creating a Hong Kong research task returns a string-enum response with normalized ticker.
    /// </summary>
    [Fact]
    public async Task CreateResearchTask_ReturnsCreatedTask()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<DbContextOptions>();
                    services.RemoveAll<DbContextOptions<StockAgentDbContext>>();
                    services.RemoveAll<IDbContextOptionsConfiguration<StockAgentDbContext>>();
                    services.AddDbContext<StockAgentDbContext>(options =>
                        options.UseInMemoryDatabase($"stockagent-{Guid.NewGuid()}"));
                });
            });

        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/research-tasks",
            new CreateResearchTaskRequest("700", Market.HongKong, "zh-CN"));
        var responseJson = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created, responseJson);
        responseJson.Should().Contain("\"market\":\"HongKong\"");
        responseJson.Should().Contain("\"status\":\"Queued\"");

        var body = JsonSerializer.Deserialize<ResearchTaskResponse>(responseJson, CreateJsonSerializerOptions());
        body.Should().NotBeNull();
        body!.Ticker.Should().Be("00700.HK");
        body.Status.Should().Be(ResearchTaskStatus.Queued);
    }

    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
