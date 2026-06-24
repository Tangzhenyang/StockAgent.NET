using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using StockAgent.Api.Domain;

namespace StockAgent.Api.Features.Auth;

/// <summary>
/// Endpoints for local account registration, login, logout, and session restoration.
/// 用于本地账号注册、登录、退出和会话恢复的端点。
/// </summary>
public static class AuthEndpoints
{
    /// <summary>Maps local authentication endpoints. 映射本地认证端点。</summary>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (
            RegisterRequest request,
            UserManager<ApplicationUser> userManager) =>
        {
            if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new AuthErrorResponse("User name and password are required."));
            }

            var userName = request.UserName.Trim();
            var existingUser = await userManager.FindByNameAsync(userName);
            if (existingUser is not null)
            {
                return Results.Conflict(new AuthErrorResponse("User name is already registered."));
            }

            var user = new ApplicationUser
            {
                UserName = userName,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var error = string.Join("; ", result.Errors.Select(x => x.Description));
                return Results.BadRequest(new AuthErrorResponse(error));
            }

            return Results.Created("/api/auth/me", ToResponse(user));
        }).AllowAnonymous();

        group.MapPost("/login", async (
            LoginRequest request,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager) =>
        {
            if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new AuthErrorResponse("User name and password are required."));
            }

            var user = await userManager.FindByNameAsync(request.UserName.Trim());
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var passwordIsValid = await userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordIsValid)
            {
                return Results.Unauthorized();
            }

            await signInManager.SignInAsync(
                user,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                });
            return Results.Ok(ToResponse(user));
        }).AllowAnonymous();

        group.MapPost("/logout", async (SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.NoContent();
        }).AllowAnonymous();

        group.MapGet("/me", async (
            HttpContext httpContext,
            UserManager<ApplicationUser> userManager) =>
        {
            if (httpContext.User.Identity?.IsAuthenticated != true)
            {
                return Results.Unauthorized();
            }

            var user = await userManager.GetUserAsync(httpContext.User);
            return user is null ? Results.Unauthorized() : Results.Ok(ToResponse(user));
        }).AllowAnonymous();

        return app;
    }

    private static CurrentUserResponse ToResponse(ApplicationUser user)
    {
        return new CurrentUserResponse(user.Id, user.UserName ?? string.Empty, true);
    }
}
