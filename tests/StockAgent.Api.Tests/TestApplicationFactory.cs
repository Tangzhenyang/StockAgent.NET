using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StockAgent.Api.Infrastructure.Ai.Chat;
using StockAgent.Api.Infrastructure.Persistence;

namespace StockAgent.Api.Tests;

/// <summary>
/// Creates isolated in-memory API hosts for integration tests.
/// 为集成测试创建隔离的内存 API 主机。
/// </summary>
internal static class TestApplicationFactory
{
    /// <summary>
    /// Creates a test host with one stable in-memory database name.
    /// 使用一个稳定内存数据库名称创建测试主机。
    /// </summary>
    public static WebApplicationFactory<Program> Create()
    {
        return Create(_ => { });
    }

    /// <summary>
    /// Creates a test host and allows callers to override services.
    /// 创建测试主机，并允许调用方覆盖服务。
    /// </summary>
    public static WebApplicationFactory<Program> Create(Action<IServiceCollection> configureServices)
    {
        var databaseName = $"stockagent-test-{Guid.NewGuid()}";
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<DbContextOptions>();
                    services.RemoveAll<DbContextOptions<StockAgentDbContext>>();
                    services.RemoveAll<IDbContextOptionsConfiguration<StockAgentDbContext>>();
                    services.AddDbContext<StockAgentDbContext>(options =>
                        options.UseInMemoryDatabase(databaseName));
                    configureServices(services);
                });
            });
    }

    /// <summary>
    /// Creates a test host that uses a fake model chat client.
    /// 创建使用假模型聊天客户端的测试主机。
    /// </summary>
    public static WebApplicationFactory<Program> CreateWithModelClient(IModelChatClient modelChatClient)
    {
        return Create(services =>
        {
            services.RemoveAll<IModelChatClient>();
            services.AddSingleton(modelChatClient);
        });
    }

    /// <summary>
    /// Registers and logs in a test user on the provided cookie-enabled client.
    /// 在提供的启用 Cookie 的客户端上注册并登录测试用户。
    /// </summary>
    public static async Task RegisterAndLoginAsync(HttpClient client, string userName, string password = "password123")
    {
        await client.PostAsJsonAsync("/api/auth/register", new { userName, password });
        await client.PostAsJsonAsync("/api/auth/login", new { userName, password });
    }
}
