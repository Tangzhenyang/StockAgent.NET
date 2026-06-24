using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace StockAgent.Api.Tests;

/// <summary>
/// Verifies built-in account authentication and persistent browser login.
/// 验证内置账号认证和浏览器持久登录。
/// </summary>
public sealed class AuthApiTests
{
    /// <summary>
    /// Registers, logs in, restores the current user from the auth cookie, and logs out.
    /// 注册、登录、通过认证 Cookie 恢复当前用户并退出登录。
    /// </summary>
    [Fact]
    public async Task LoginFlow_PersistsCurrentUserUntilLogout()
    {
        await using var factory = TestApplicationFactory.Create();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { userName = "alice", password = "password123" });

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var anonymousMeResponse = await factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        }).GetAsync("/api/auth/me");
        anonymousMeResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { userName = "alice", password = "password123" });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Should().Contain(cookie => cookie.Contains("StockAgent.Auth", StringComparison.Ordinal));

        var meResponse = await client.GetAsync("/api/auth/me");
        var meJson = await meResponse.Content.ReadAsStringAsync();
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK, meJson);
        using (var document = JsonDocument.Parse(meJson))
        {
            document.RootElement.GetProperty("userName").GetString().Should().Be("alice");
            document.RootElement.GetProperty("isAuthenticated").GetBoolean().Should().BeTrue();
        }

        var logoutResponse = await client.PostAsync("/api/auth/logout", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var meAfterLogoutResponse = await client.GetAsync("/api/auth/me");
        meAfterLogoutResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
