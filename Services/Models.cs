namespace IntradayUpstox.Services;
public record CandleDto(System.DateTime Time, decimal Open, decimal High, decimal Low, decimal Close, long Volume);
