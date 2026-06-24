namespace StockAgent.Api.Infrastructure.Security;

/// <summary>
/// Protects user model API keys before they are stored in the database.
/// 在用户模型 API Key 写入数据库前对其进行保护。
/// </summary>
public interface IApiKeyProtector
{
    /// <summary>Encrypts an API key for database storage. 加密 API Key 以便数据库存储。</summary>
    string Protect(string apiKey);
    /// <summary>Decrypts an API key for backend-only provider calls. 解密 API Key 供后端调用提供商使用。</summary>
    string Unprotect(string protectedApiKey);
}
