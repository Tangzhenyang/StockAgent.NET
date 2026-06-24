using Microsoft.AspNetCore.DataProtection;

namespace StockAgent.Api.Infrastructure.Security;

/// <summary>
/// Data Protection based implementation for user model API key encryption.
/// 基于 Data Protection 的用户模型 API Key 加密实现。
/// </summary>
public sealed class DataProtectionApiKeyProtector(IDataProtectionProvider provider) : IApiKeyProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("StockAgent.UserModelApiKey.v1");

    /// <inheritdoc />
    public string Protect(string apiKey)
    {
        return _protector.Protect(apiKey);
    }

    /// <inheritdoc />
    public string Unprotect(string protectedApiKey)
    {
        return _protector.Unprotect(protectedApiKey);
    }
}
