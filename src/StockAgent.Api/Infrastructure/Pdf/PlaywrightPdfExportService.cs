using Microsoft.Playwright;

namespace StockAgent.Api.Infrastructure.Pdf;

/// <summary>
/// PDF exporter that renders report HTML through Playwright Chromium.
/// </summary>
public sealed class PlaywrightPdfExportService(IWebHostEnvironment environment) : IPdfExportService
{
    /// <inheritdoc />
    public async Task<string> ExportAsync(Guid researchTaskId, string html, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var directory = Path.Combine(environment.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot"), "pdf");
        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, $"{researchTaskId}.pdf");

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();

        // The print wrapper gives the PDF stable typography and spacing independent of the web UI.
        var document = $$"""
        <!doctype html>
        <html lang="zh-CN">
        <head>
          <meta charset="utf-8" />
          <style>
            body { font-family: "Microsoft YaHei", "Noto Sans CJK SC", Arial, sans-serif; margin: 32px; line-height: 1.65; color: #17202a; }
            article { max-width: 820px; margin: 0 auto; }
          </style>
        </head>
        <body>{{html}}</body>
        </html>
        """;

        await page.SetContentAsync(document, new PageSetContentOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.PdfAsync(new PagePdfOptions
        {
            Path = filePath,
            Format = "A4",
            PrintBackground = true,
            Margin = new Margin { Top = "18mm", Right = "16mm", Bottom = "18mm", Left = "16mm" }
        });

        return filePath;
    }
}
