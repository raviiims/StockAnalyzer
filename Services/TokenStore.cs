using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace IntradayUpstox.Services;
public class TokenStore
{
    private string? _accessToken;
    private readonly string _tokenFilePath;
    private static readonly object _lock = new object();

    public TokenStore(IHostEnvironment hostEnvironment)
    {
        // Store token file in the content root directory
        _tokenFilePath = Path.Combine(hostEnvironment.ContentRootPath, "tokens.json");
        LoadToken();
    }

    public bool HasToken => !string.IsNullOrEmpty(_accessToken);
    public string? GetToken() => _accessToken;

    public void SetToken(string token)
    {
        _accessToken = token;
        SaveToken();
    }

    private void LoadToken()
    {
        try
        {
            if (File.Exists(_tokenFilePath))
            {
                lock (_lock)
                {
                    var json = File.ReadAllText(_tokenFilePath);
                    var tokenData = JsonSerializer.Deserialize<TokenData>(json);
                    _accessToken = tokenData?.AccessToken;
                    Console.WriteLine($"Token loaded from: {_tokenFilePath}");
                }
            }
            else
            {
                Console.WriteLine($"Token file not found at: {_tokenFilePath} (will be created on first authentication)");
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail initialization
            Console.WriteLine($"Error loading token: {ex.Message}");
        }
    }

    private void SaveToken()
    {
        try
        {
            lock (_lock)
            {
                var tokenData = new TokenData { AccessToken = _accessToken };
                var json = JsonSerializer.Serialize(tokenData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_tokenFilePath, json);
                Console.WriteLine($"Token saved to: {_tokenFilePath}");
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail token setting
            Console.WriteLine($"Error saving token: {ex.Message}");
        }
    }

    private class TokenData
    {
        public string? AccessToken { get; set; }
    }
}
