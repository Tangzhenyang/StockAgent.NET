using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StockAgent.Api.Domain;
using StockAgent.Api.Features.ResearchTasks;
using StockAgent.Api.Infrastructure.Pdf;
using StockAgent.Api.Infrastructure.Persistence;

namespace StockAgent.Api.Tests;

/// <summary>
/// Verifies PDF export can be downloaded by the owning user.
/// 验证 PDF 导出可由所属用户下载。
/// </summary>
public sealed class PdfExportApiTests
{
    /// <summary>
    /// Exporting a completed report returns a browser-download URL and that URL streams PDF bytes.
    /// 导出完成报告会返回浏览器下载 URL，且该 URL 会流式返回 PDF 字节。
    /// </summary>
    [Fact]
    public async Task ExportPdf_ReturnsDownloadUrlAndStreamsPdf()
    {
        await using var factory = TestApplicationFactory.Create().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPdfExportService>();
                services.AddSingleton<IPdfExportService, FakePdfExportService>();
            });
        });
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        await TestApplicationFactory.RegisterAndLoginAsync(client, "pdf-user");
        var task = await CreateTaskAsync(client);
        await SeedReportAsync(factory.Services, task.Id);

        var exportResponse = await client.PostAsync($"/api/research-tasks/{task.Id}/pdf", null);
        var exportJson = await exportResponse.Content.ReadAsStringAsync();

        exportResponse.StatusCode.Should().Be(HttpStatusCode.OK, exportJson);
        exportJson.Should().NotContain(Path.GetTempPath());
        using var document = JsonDocument.Parse(exportJson);
        document.RootElement.GetProperty("status").GetString().Should().Be("Completed");
        var downloadUrl = document.RootElement.GetProperty("downloadUrl").GetString();
        downloadUrl.Should().Be($"/api/research-tasks/{task.Id}/pdf/download");

        var downloadResponse = await client.GetAsync(downloadUrl);
        var bytes = await downloadResponse.Content.ReadAsByteArrayAsync();

        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        downloadResponse.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        bytes.Should().StartWith("%PDF"u8.ToArray());
    }

    /// <summary>
    /// PDF renderer failures return a readable problem payload for the Web UI.
    /// PDF 渲染器失败时会返回可供 Web UI 展示的问题详情。
    /// </summary>
    [Fact]
    public async Task ExportPdf_ReturnsProblemDetailsWhenRendererFails()
    {
        await using var factory = TestApplicationFactory.Create().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPdfExportService>();
                services.AddSingleton<IPdfExportService, FailingPdfExportService>();
            });
        });
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        await TestApplicationFactory.RegisterAndLoginAsync(client, "pdf-failure-user");
        var task = await CreateTaskAsync(client);
        await SeedReportAsync(factory.Services, task.Id);

        var response = await client.PostAsync($"/api/research-tasks/{task.Id}/pdf", null);
        var json = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError, json);
        json.Should().Contain("PDF export failed");
        json.Should().Contain("Chromium missing");
    }

    private static async Task<ResearchTaskResponse> CreateTaskAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/research-tasks",
            new CreateResearchTaskRequest("700", Market.HongKong, "zh-CN"));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ResearchTaskResponse>(json, CreateJsonSerializerOptions())!;
    }

    private static async Task SeedReportAsync(IServiceProvider serviceProvider, Guid taskId)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StockAgentDbContext>();
        var task = await db.ResearchTasks.FindAsync(taskId);
        task!.Status = ResearchTaskStatus.Ready;
        task.ProgressPercent = 100;
        db.ResearchReports.Add(new ResearchReport
        {
            ResearchTaskId = taskId,
            Markdown = "# report",
            Html = "<h1>report</h1>",
            RatingJson = "{}"
        });
        await db.SaveChangesAsync();
    }

    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private sealed class FakePdfExportService : IPdfExportService
    {
        public async Task<string> ExportAsync(Guid researchTaskId, string html, CancellationToken cancellationToken)
        {
            var directory = Path.Combine(Path.GetTempPath(), "stockagent-test-pdf");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, $"{researchTaskId}.pdf");
            await File.WriteAllBytesAsync(path, "%PDF-1.4 fake pdf"u8.ToArray(), cancellationToken);
            return path;
        }
    }

    private sealed class FailingPdfExportService : IPdfExportService
    {
        public Task<string> ExportAsync(Guid researchTaskId, string html, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Chromium missing");
        }
    }
}
