using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NoteApp;

public class MarketPrice
{
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
    public double Price { get; set; }
    public string Currency { get; set; } = "THB";
    public double Change24h { get; set; }
    public string Category { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}

public static class MarketDataService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static List<MarketPrice>? _cache;
    private static DateTime _lastFetch = DateTime.MinValue;

    public static async Task<List<MarketPrice>> FetchAll()
    {
        if (_cache != null && (DateTime.Now - _lastFetch).TotalMinutes < 5)
            return _cache;

        var prices = new List<MarketPrice>();
        var now = DateTime.Now.ToString("HH:mm");

        // 1. Crypto (CoinGecko — free, no key)
        try
        {
            var url = "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin,ethereum,binancecoin,ripple,solana,dogecoin&vs_currencies=thb,usd&include_24hr_change=true";
            var json = await Http.GetStringAsync(url);
            var doc = JsonDocument.Parse(json);

            var cryptos = new (string id, string name, string icon)[]
            {
                ("bitcoin", "Bitcoin", "₿"),
                ("ethereum", "Ethereum", "Ξ"),
                ("binancecoin", "BNB", "🔶"),
                ("ripple", "XRP", "💧"),
                ("solana", "Solana", "◎"),
                ("dogecoin", "Dogecoin", "🐕"),
            };

            foreach (var (id, name, icon) in cryptos)
            {
                if (doc.RootElement.TryGetProperty(id, out var coin))
                {
                    prices.Add(new MarketPrice
                    {
                        Name = name, Icon = icon, Category = "คริปโต",
                        Price = coin.TryGetProperty("thb", out var thb) ? thb.GetDouble() : 0,
                        Change24h = coin.TryGetProperty("thb_24h_change", out var chg) ? chg.GetDouble() : 0,
                        Currency = "THB", UpdatedAt = now
                    });
                }
            }
        }
        catch { }

        // 2. Exchange rates (free API)
        try
        {
            var json = await Http.GetStringAsync("https://open.er-api.com/v6/latest/USD");
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("rates", out var rates))
            {
                var pairs = new (string code, string name, string icon)[]
                {
                    ("THB", "USD/THB", "🇺🇸"),
                    ("EUR", "USD/EUR", "🇪🇺"),
                    ("JPY", "USD/JPY", "🇯🇵"),
                    ("CNY", "USD/CNY", "🇨🇳"),
                    ("GBP", "USD/GBP", "🇬🇧"),
                };

                foreach (var (code, name, icon) in pairs)
                {
                    if (rates.TryGetProperty(code, out var rate))
                    {
                        prices.Add(new MarketPrice
                        {
                            Name = name, Icon = icon, Category = "อัตราแลกเปลี่ยน",
                            Price = rate.GetDouble(), Currency = code, UpdatedAt = now
                        });
                    }
                }
            }
        }
        catch { }

        // 3. Gold price estimate (from USD price ~2,400 USD/oz * THB rate)
        try
        {
            var thbRate = prices.Find(p => p.Name == "USD/THB")?.Price ?? 35.0;
            var json = await Http.GetStringAsync("https://api.coingecko.com/api/v3/simple/price?ids=tether-gold&vs_currencies=usd&include_24hr_change=true");
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("tether-gold", out var gold))
            {
                var usdPrice = gold.TryGetProperty("usd", out var p) ? p.GetDouble() : 2400;
                var change = gold.TryGetProperty("usd_24h_change", out var c) ? c.GetDouble() : 0;
                prices.Add(new MarketPrice
                {
                    Name = "ทองคำ (oz)", Icon = "🥇", Category = "สินค้าโภคภัณฑ์",
                    Price = usdPrice * thbRate, Currency = "THB", Change24h = change, UpdatedAt = now
                });
                prices.Add(new MarketPrice
                {
                    Name = "ทองคำ บาทละ", Icon = "🥇", Category = "สินค้าโภคภัณฑ์",
                    Price = usdPrice * thbRate / 31.1035 * 15.244,
                    Currency = "THB", Change24h = change, UpdatedAt = now
                });
            }
        }
        catch { }

        // 4. Thai interest rate info (static but useful)
        prices.Add(new MarketPrice { Name = "ดอกเบี้ยนโยบาย ธปท.", Icon = "🏛️", Category = "อัตราดอกเบี้ย",
            Price = 2.25, Currency = "%", UpdatedAt = now });
        prices.Add(new MarketPrice { Name = "เงินฝากออมทรัพย์", Icon = "🏦", Category = "อัตราดอกเบี้ย",
            Price = 0.5, Currency = "%", UpdatedAt = now });
        prices.Add(new MarketPrice { Name = "เงินฝากประจำ 12 เดือน", Icon = "🏦", Category = "อัตราดอกเบี้ย",
            Price = 1.5, Currency = "%", UpdatedAt = now });
        prices.Add(new MarketPrice { Name = "เงินเฟ้อไทย (ล่าสุด)", Icon = "📊", Category = "เศรษฐกิจ",
            Price = 1.1, Currency = "%", UpdatedAt = now });

        _cache = prices;
        _lastFetch = DateTime.Now;
        return prices;
    }

    public static void ClearCache() { _cache = null; }
}
