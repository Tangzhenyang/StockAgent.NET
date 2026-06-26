using Microsoft.Playwright;

namespace StockAgent.Api.Infrastructure.Pdf;

/// <summary>
/// PDF exporter that renders report HTML through Playwright Chromium.
/// 通过 Playwright Chromium 渲染报告 HTML 的 PDF 导出器。
/// </summary>
public sealed class PlaywrightPdfExportService(IWebHostEnvironment environment) : IPdfExportService
{
    private static readonly string[] ContainerChromiumArgs =
    [
        "--no-sandbox",
        "--disable-setuid-sandbox",
        "--disable-dev-shm-usage"
    ];

    /// <inheritdoc />
    public async Task<string> ExportAsync(Guid researchTaskId, string html, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var directory = Path.Combine(environment.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot"), "pdf");
        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, $"{researchTaskId}.pdf");

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await LaunchBrowserAsync(playwright);
        var page = await browser.NewPageAsync();

        // The print wrapper gives the PDF stable typography and spacing independent of the web UI.
        // 打印包装层可使 PDF 的版式和间距独立于 Web UI 保持稳定。
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

    private static async Task<IBrowser> LaunchBrowserAsync(IPlaywright playwright)
    {
        var executablePath = Environment.GetEnvironmentVariable("PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH");
        if (!string.IsNullOrWhiteSpace(executablePath))
        {
            // Linux containers can point Playwright to the system Chromium package installed in the image.
            // Linux 容器可通过该环境变量使用镜像内安装的系统 Chromium。
            return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                ExecutablePath = executablePath,
                Args = ContainerChromiumArgs
            });
        }

        try
        {
            return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = ContainerChromiumArgs
            });
        }
        catch (PlaywrightException exception) when (
            exception.Message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase))
        {
            // Local Windows development can use the installed Edge browser when Playwright's bundled Chromium is absent.
            // 本地 Windows 开发环境缺少 Playwright 自带 Chromium 时，可回退使用已安装的 Edge 浏览器。
            return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Channel = "msedge"
            });
        }
    }
}
