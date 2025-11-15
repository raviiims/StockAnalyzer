namespace IntradayUpstox.Services;
public class TokenStore
{
    private string? _accessToken;
    public bool HasToken => !string.IsNullOrEmpty(_accessToken);
    public string? GetToken() => _accessToken;
    public void SetToken(string token) => _accessToken = token;
}
