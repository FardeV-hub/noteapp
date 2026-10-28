using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace NoteApp;

public class ConnectedAccount
{
    public string Service { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Color { get; set; } = "";
    public bool Connected { get; set; }
    public Dictionary<string, string> Credentials { get; set; } = new();
    public string ConnectedAt { get; set; } = "";
}

public class AllAccounts
{
    public Dictionary<string, ConnectedAccount> Services { get; set; } = new();
}

public static class AccountsStore
{
    private static readonly string FilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "accounts.json");
    private static readonly JsonSerializerOptions Opts = new()
    { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static readonly (string Key, string Name, string Icon, string Color, (string Id, string Label, string Hint)[] Fields)[] ServiceDefs =
    {
        ("line", "LINE Notify", "💬", "#00b900", new[] {
            ("token", "LINE Token", "ขอที่ notify-bot.line.me → Generate Token")
        }),
        ("telegram", "Telegram Bot", "✈️", "#0088cc", new[] {
            ("token", "Bot Token", "คุยกับ @BotFather → /newbot → คัดลอก Token"),
            ("chat_id", "Chat ID", "คุยกับ @userinfobot → จะบอก ID ให้")
        }),
        ("discord", "Discord", "🎮", "#5865F2", new[] {
            ("webhook", "Webhook URL", "Server Settings → Integrations → Webhooks → Copy URL")
        }),
        ("gcal", "Google Calendar", "📅", "#4285f4", new[] {
            ("api_key", "API Key", "console.cloud.google.com → Credentials → API Key"),
            ("calendar_id", "Calendar ID", "ใส่ 'primary' สำหรับปฏิทินหลัก")
        }),
        ("classroom", "Google Classroom", "📚", "#0f9d58", new[] {
            ("api_key", "API Key", "console.cloud.google.com → Credentials"),
            ("course_id", "Course ID", "รหัสวิชาจาก Classroom")
        }),
        ("claude", "Claude AI", "🟣", "#8b5cf6", new[] {
            ("api_key", "API Key", "console.anthropic.com → API Keys")
        }),
        ("openai", "OpenAI GPT", "🟢", "#22c55e", new[] {
            ("api_key", "API Key", "platform.openai.com → API Keys")
        }),
        ("gemini", "Google Gemini", "🔵", "#3b82f6", new[] {
            ("api_key", "API Key", "aistudio.google.com → Get API Key (ฟรี!)")
        }),
        ("huggingface", "Hugging Face", "🟡", "#eab308", new[] {
            ("api_key", "Token", "huggingface.co/settings/tokens (ฟรี!)")
        }),
        ("homeassistant", "Home Assistant", "🏠", "#0984e3", new[] {
            ("url", "URL", "เช่น http://192.168.1.100:8123"),
            ("token", "Long-Lived Token", "Profile → Long-Lived Access Tokens → Create")
        }),
        ("tuya", "Tuya Smart", "📱", "#ff6b00", new[] {
            ("client_id", "Client ID", "iot.tuya.com → Cloud → Project"),
            ("secret", "Client Secret", "iot.tuya.com → Cloud → Project")
        }),
    };

    public static AllAccounts Load()
    {
        if (!File.Exists(FilePath)) return new AllAccounts();
        try { return JsonSerializer.Deserialize<AllAccounts>(File.ReadAllText(FilePath), Opts) ?? new(); }
        catch { return new(); }
    }

    public static void Save(AllAccounts data) =>
        File.WriteAllText(FilePath, JsonSerializer.Serialize(data, Opts));

    public static string? GetCredential(string service, string key)
    {
        var all = Load();
        if (all.Services.TryGetValue(service, out var acc) && acc.Credentials.TryGetValue(key, out var val))
            return val;
        return null;
    }

    public static void SyncToOldConfigs(AllAccounts data)
    {
        // LINE
        if (data.Services.TryGetValue("line", out var line) && line.Connected)
            ShareHelper.SaveLineToken(line.Credentials.GetValueOrDefault("token", ""));

        // Telegram
        if (data.Services.TryGetValue("telegram", out var tg) && tg.Connected)
            TelegramHelper.SaveConfig(tg.Credentials.GetValueOrDefault("token", ""),
                tg.Credentials.GetValueOrDefault("chat_id", ""));

        // Google Calendar
        if (data.Services.TryGetValue("gcal", out var gcal) && gcal.Connected)
            GoogleCalendarConfig.Save(gcal.Credentials.GetValueOrDefault("api_key", ""),
                gcal.Credentials.GetValueOrDefault("calendar_id", "primary"));

        // AI
        var aiConfig = AIConfigStore.Load();
        if (data.Services.TryGetValue("claude", out var cl)) aiConfig.ClaudeKey = cl.Credentials.GetValueOrDefault("api_key", "");
        if (data.Services.TryGetValue("openai", out var oa)) aiConfig.OpenAIKey = oa.Credentials.GetValueOrDefault("api_key", "");
        if (data.Services.TryGetValue("gemini", out var gm)) aiConfig.GeminiKey = gm.Credentials.GetValueOrDefault("api_key", "");
        if (data.Services.TryGetValue("huggingface", out var hf)) aiConfig.HuggingFaceKey = hf.Credentials.GetValueOrDefault("api_key", "");
        AIConfigStore.Save(aiConfig);

        // Discord + Classroom + Smart Home
        var integ = IntegrationStore.Load();
        if (data.Services.TryGetValue("discord", out var dc)) integ.DiscordWebhook = dc.Credentials.GetValueOrDefault("webhook", "");
        if (data.Services.TryGetValue("classroom", out var cr))
        {
            integ.GoogleApiKey = cr.Credentials.GetValueOrDefault("api_key", "");
            integ.CourseId = cr.Credentials.GetValueOrDefault("course_id", "");
        }
        IntegrationStore.Save(integ);

        var sh = SmartHomeStore.Load();
        if (data.Services.TryGetValue("homeassistant", out var ha))
        {
            sh.HomeAssistantUrl = ha.Credentials.GetValueOrDefault("url", "");
            sh.HomeAssistantToken = ha.Credentials.GetValueOrDefault("token", "");
        }
        if (data.Services.TryGetValue("tuya", out var ty))
        {
            sh.TuyaClientId = ty.Credentials.GetValueOrDefault("client_id", "");
            sh.TuyaSecret = ty.Credentials.GetValueOrDefault("secret", "");
        }
        SmartHomeStore.Save(sh);
    }
}
