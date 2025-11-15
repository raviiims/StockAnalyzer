using System.Net.Http.Headers;
using System.Text.Json;
namespace IntradayUpstox.Services;

public class UpstoxService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _cfg;
    private readonly TokenStore _tokens;
    public FeatureService FeatureService { get; }

    public UpstoxService(IHttpClientFactory httpFactory, IConfiguration cfg, TokenStore tokens, FeatureService feature)
    {
        _httpFactory = httpFactory;
        _cfg = cfg;
        _tokens = tokens;
        FeatureService = feature;
    }

    public string GetAuthorizeUrl()
    {
        var clientId = _cfg["Upstox:ClientId"] ?? "";
        var redirect = Uri.EscapeDataString(_cfg["Upstox:RedirectUri"] ?? "");
        var template = _cfg["Upstox:AuthorizeUrl"] ?? "https://api.upstox.com/v2/login/authorization?client_id={client_id}&response_type=code&redirect_uri={redirect_uri}";
        return template.Replace("{client_id}", clientId).Replace("{redirect_uri}", redirect);
    }

    public async Task<bool> ExchangeCodeForTokenAsync(string code)
    {
        var client = _httpFactory.CreateClient();
        var tokenUrl = (_cfg["Upstox:BaseUrl"] ?? "https://api.upstox.com/v2/") + "login/authorization/token";
        var form = new Dictionary<string,string>
        {
            ["code"] = code,
            ["client_id"] = _cfg["Upstox:ClientId"] ?? "",
            ["client_secret"] = _cfg["Upstox:ClientSecret"] ?? "",
            ["redirect_uri"] = _cfg["Upstox:RedirectUri"] ?? "",
            ["grant_type"] = "authorization_code"
        };
        var resp = await client.PostAsync(tokenUrl, new FormUrlEncodedContent(form));
        if (!resp.IsSuccessStatusCode) return false;
        using var s = await resp.Content.ReadAsStreamAsync();
        var doc = await JsonDocument.ParseAsync(s);
        if (doc.RootElement.TryGetProperty("access_token", out var t))
        {
            var token = t.GetString() ?? "";
            _tokens.SetToken(token);
            return true;
        }
        return false;
    }

    // Fetch intraday candles (requires access token)
    //public async Task<List<CandleDto>> GetIntradayCandlesAsync(string instrumentRaw, string interval = "1minute")
    //{
    //    var token = _tokens.GetToken();
    //    if (string.IsNullOrEmpty(token)) return new List<CandleDto>();
    //    var client = _httpFactory.CreateClient();
    //    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    //    var ik = Uri.EscapeDataString(instrumentRaw);

    //    // Fix URL construction - remove double slashes
    //    var baseUrl = (_cfg["Upstox:BaseUrl"] ?? "https://api.upstox.com/v2/").TrimEnd('/');
    //    var url = $"{baseUrl}/historical-candle/intraday/{ik}/{interval}";

    //    // Add date parameters (Upstox API requires to and from dates)
    //    var toDate = DateTime.UtcNow;
    //    var fromDate = toDate.AddDays(-3); // Get last 24 hours of data
    //    var fromDateStr = fromDate.ToString("yyyy-MM-dd");
    //    var toDateStr = toDate.ToString("yyyy-MM-dd");
    //    url += $"?to_date={toDateStr}&from_date={fromDateStr}";

    //    var resp = await client.GetAsync(url);
    //    if (!resp.IsSuccessStatusCode)
    //    {
    //        // Log error details for debugging
    //        var errorContent = await resp.Content.ReadAsStringAsync();
    //        Console.WriteLine($"Upstox API Error: Status={resp.StatusCode}, URL={url}, Response={errorContent}");
    //        return new List<CandleDto>();
    //    }

    //    using var s = await resp.Content.ReadAsStreamAsync();
    //    var doc = await JsonDocument.ParseAsync(s);
    //    var list = new List<CandleDto>();

    //    if (doc.RootElement.TryGetProperty("data", out var data))
    //    {
    //        // Handle different response formats
    //        JsonElement candlesArray = default;

    //        if (data.ValueKind == JsonValueKind.Array)
    //        {
    //            // data is directly an array
    //            candlesArray = data;
    //        }
    //        else if (data.ValueKind == JsonValueKind.Object)
    //        {
    //            // data is an object, try to find the candles array
    //            if (data.TryGetProperty("candles", out var candles))
    //            {
    //                candlesArray = candles;
    //            }
    //            else if (data.TryGetProperty("data", out var nestedData) && nestedData.ValueKind == JsonValueKind.Array)
    //            {
    //                candlesArray = nestedData;
    //            }
    //            else
    //            {
    //                // Log the structure for debugging
    //                Console.WriteLine("Data is an object. Available properties:");
    //                foreach (var prop in data.EnumerateObject())
    //                {
    //                    Console.WriteLine($"  - {prop.Name}: {prop.Value.ValueKind}");
    //                    if (prop.Value.ValueKind == JsonValueKind.Array)
    //                    {
    //                        candlesArray = prop.Value;
    //                        Console.WriteLine($"    Using '{prop.Name}' as candles array");
    //                        break;
    //                    }
    //                }
    //            }
    //        }

    //        // Parse the candles array
    //        if (candlesArray.ValueKind == JsonValueKind.Array)
    //        {
    //            foreach (var item in candlesArray.EnumerateArray())
    //            {
    //                try
    //                {
    //                    if (item.ValueKind == JsonValueKind.Array)
    //                    {
    //                        // Format: [timestamp, open, high, low, close, volume]
    //                        var ts = item[0].GetInt64();
    //                        var dt = DateTimeOffset.FromUnixTimeMilliseconds(ts).UtcDateTime;
    //                        var o = item[1].GetDecimal();
    //                        var h = item[2].GetDecimal();
    //                        var l = item[3].GetDecimal();
    //                        var c = item[4].GetDecimal();
    //                        var v = item[5].GetInt64();
    //                        list.Add(new CandleDto(dt, o, h, l, c, v));
    //                    }
    //                    else if (item.ValueKind == JsonValueKind.Object)
    //                    {
    //                        // Format: {timestamp, open, high, low, close, volume}
    //                        long ts = 0;
    //                        if (item.TryGetProperty("timestamp", out var p)) ts = p.GetInt64();
    //                        else if (item.TryGetProperty("time", out var p2)) ts = p2.GetInt64();
    //                        else if (item.TryGetProperty("t", out var p3)) ts = p3.GetInt64();

    //                        var dt = DateTimeOffset.FromUnixTimeMilliseconds(ts).UtcDateTime;
    //                        var o = item.GetProperty("open").GetDecimal();
    //                        var h = item.GetProperty("high").GetDecimal();
    //                        var l = item.GetProperty("low").GetDecimal();
    //                        var c = item.GetProperty("close").GetDecimal();
    //                        var v = item.GetProperty("volume").GetInt64();
    //                        list.Add(new CandleDto(dt, o, h, l, c, v));
    //                    }
    //                }
    //                catch (Exception ex)
    //                {
    //                    Console.WriteLine($"Error parsing candle: {ex.Message}");
    //                    // ignore parse errors
    //                }
    //            }
    //        }
    //    }

    //    return list.OrderBy(x=>x.Time).ToList();
    //}

    public async Task<List<CandleDto>> GetIntradayCandlesAsync(string instrumentRaw, string interval = "1minute")
    {
        var token = _tokens.GetToken();
        if (string.IsNullOrEmpty(token)) return new List<CandleDto>();

        var client = _httpFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var baseUrl = (_cfg["Upstox:BaseUrl"] ?? "https://api.upstox.com/v2/").TrimEnd('/');
        var ik = Uri.EscapeDataString(instrumentRaw);

        var url = $"{baseUrl}/historical-candle/intraday/{ik}/{interval}";

        var resp = await client.GetAsync(url);
        var json = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            Console.WriteLine("Upstox error: " + json);
            return new List<CandleDto>();
        }

        using var doc = JsonDocument.Parse(json);
        var list = new List<CandleDto>();

        if (doc.RootElement.TryGetProperty("data", out var dataObj))
        {
            if (dataObj.TryGetProperty("candles", out var candles))
            {
                foreach (var row in candles.EnumerateArray())
                {
                    try
                    {
                        // row = [ "2025-11-14T00:00:00+05:30", open, high, low, close, volume, open_interest ]

                        // 1. DATE STRING → DateTime
                        var dateStr = row[0].GetString();
                        DateTime dt = DateTime.Parse(dateStr);

                        // 2. Numbers
                        decimal o = row[1].GetDecimal();
                        decimal h = row[2].GetDecimal();
                        decimal l = row[3].GetDecimal();
                        decimal c = row[4].GetDecimal();
                        long v = row[5].GetInt64();   // volume

                        list.Add(new CandleDto(dt, o, h, l, c, v));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Parse error: {ex.Message}");
                    }
                }
            }
        }

        return list.OrderBy(x => x.Time).ToList();
    }


    public async Task<List<CandleDto>> GetDailyCandlesAsync(string instrumentKey, DateTime from, DateTime to)
    {
        var token = _tokens.GetToken();
        if (string.IsNullOrEmpty(token))
            return new List<CandleDto>();

        var client = _httpFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("Accept", "application/json");

        var baseUrl = (_cfg["Upstox:BaseUrl"] ?? "https://api.upstox.com/v2/").TrimEnd('/');
        var ik = Uri.EscapeDataString(instrumentKey);

        string fromDate = from.ToString("yyyy-MM-dd");
        string toDate = to.ToString("yyyy-MM-dd");

        var url = $"{baseUrl}/historical-candle/{ik}/day/{toDate}/{fromDate}";

        var resp = await client.GetAsync(url);
        var json = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            Console.WriteLine("Upstox Error: " + json);
            return new List<CandleDto>();
        }

        using var doc = JsonDocument.Parse(json);
        var list = new List<CandleDto>();

        if (doc.RootElement.TryGetProperty("data", out var dataObj) &&
            dataObj.TryGetProperty("candles", out var candles))
        {
            foreach (var row in candles.EnumerateArray())
            {
                try
                {
                    // row = [ "2025-11-14T00:00:00+05:30", open, high, low, close, volume, open_interest ]

                    // 1. DATE STRING → DateTime
                    var dateStr = row[0].GetString();
                    DateTime dt = DateTime.Parse(dateStr);

                    // 2. Numbers
                    decimal o = row[1].GetDecimal();
                    decimal h = row[2].GetDecimal();
                    decimal l = row[3].GetDecimal();
                    decimal c = row[4].GetDecimal();
                    long v = row[5].GetInt64();   // volume

                    list.Add(new CandleDto(dt, o, h, l, c, v));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Parse error: {ex.Message}");
                }
            }
        }

        return list.OrderBy(x => x.Time).ToList();
    }

}
