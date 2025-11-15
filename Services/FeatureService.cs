using System.Linq;
namespace IntradayUpstox.Services;
public class FeatureService
{
    public decimal ComputeVwap(IEnumerable<CandleDto> candles)
    {
        decimal pv = 0; long vol = 0;
        foreach(var c in candles)
        {
            var typical = (c.High + c.Low + c.Close)/3;
            pv += typical * c.Volume;
            vol += c.Volume;
        }
        return vol == 0 ? 0 : pv / vol;
    }

    public List<decimal> RSI(List<decimal> closes, int n = 14)
    {
        var rsis = new List<decimal>();
        if (closes == null || closes.Count < 2) return rsis;
        decimal gain = 0, loss = 0;
        for (int i = 1; i < closes.Count; i++)
        {
            var diff = closes[i] - closes[i-1];
            gain += Math.Max(0, diff);
            loss += Math.Max(0, -diff);
            if (i < n) { rsis.Add(50); continue; }
            if (i == n) { gain /= n; loss /= n; }
            else { gain = (gain*(n-1) + Math.Max(0, closes[i]-closes[i-1]))/n; loss = (loss*(n-1) + Math.Max(0, closes[i-1]-closes[i]))/n; }
            var rs = loss == 0 ? 100 : gain/loss;
            var rsi = 100 - (100/(1+rs));
            rsis.Add((decimal)rsi);
        }
        return rsis;
    }

    public int ScoreStock(List<CandleDto> recent)
    {
        if (recent==null || recent.Count==0) return 0;
        var closes = recent.Select(c=>c.Close).ToList();
        var vwap = ComputeVwap(recent);
        var rsi = RSI(closes).LastOrDefault();
        int score = 50;
        if (closes.Last() > vwap) score += 15;
        if (rsi > 55 && rsi < 75) score += 10;
        var avgVol = recent.Take(Math.Min(20, recent.Count)).Select(c => c.Volume).DefaultIfEmpty(0).Average();
        if (recent.Last().Volume > 2 * avgVol) score += 20;
        var highs = recent.Select(c=>c.High).Max();
        var lows = recent.Select(c=>c.Low).Min();
        var atr = (highs - lows) / (recent.Last().Close == 0 ? 1 : recent.Last().Close);
        if (atr > 0.05m) score -= 25;
        return Math.Clamp(score, 0, 100);
    }
}
