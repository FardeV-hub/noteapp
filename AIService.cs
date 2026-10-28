using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace NoteApp;

public class AIConfig
{
    public string ClaudeKey { get; set; } = "";
    public string OpenAIKey { get; set; } = "";
    public string GeminiKey { get; set; } = "";
    public string HuggingFaceKey { get; set; } = "";
    public string DefaultProvider { get; set; } = "claude";
}

public static class AIConfigStore
{
    private static readonly string FilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "ai_config.json");
    private static readonly JsonSerializerOptions Opts = new()
    { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static AIConfig Load()
    {
        if (!File.Exists(FilePath)) return new AIConfig();
        try { return JsonSerializer.Deserialize<AIConfig>(File.ReadAllText(FilePath), Opts) ?? new(); }
        catch { return new(); }
    }
    public static void Save(AIConfig c) => File.WriteAllText(FilePath, JsonSerializer.Serialize(c, Opts));
}

public static class AIService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

    // ====================================================
    // Claude (Anthropic)
    // ====================================================
    public static async Task<string> AskClaude(string apiKey, string prompt, string system = "")
    {
        var url = "https://api.anthropic.com/v1/messages";
        var body = new
        {
            model = "claude-sonnet-4-20250514",
            max_tokens = 1024,
            system = string.IsNullOrEmpty(system) ? "คุณเป็นผู้ช่วย AI ที่เก่งมาก ตอบเป็นภาษาไทย กระชับ ได้ใจความ" : system,
            messages = new[] { new { role = "user", content = prompt } }
        };
        var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Add("x-api-key", apiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var resp = await Http.SendAsync(req);
        var json = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) return $"❌ Error: {json}";

        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "";
    }

    // ====================================================
    // OpenAI (GPT)
    // ====================================================
    public static async Task<string> AskOpenAI(string apiKey, string prompt, string system = "")
    {
        var url = "https://api.openai.com/v1/chat/completions";
        var messages = new List<object>();
        if (!string.IsNullOrEmpty(system))
            messages.Add(new { role = "system", content = system });
        else
            messages.Add(new { role = "system", content = "คุณเป็นผู้ช่วย AI ตอบภาษาไทย กระชับ ได้ใจความ" });
        messages.Add(new { role = "user", content = prompt });

        var body = new { model = "gpt-4o-mini", messages, max_tokens = 1024 };
        var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var resp = await Http.SendAsync(req);
        var json = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) return $"❌ Error: {json}";

        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }

    // ====================================================
    // Google Gemini
    // ====================================================
    public static async Task<string> AskGemini(string apiKey, string prompt)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}";
        var body = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
        var resp = await Http.PostAsync(url, new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
        var json = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) return $"❌ Error: {json}";

        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("candidates")[0].GetProperty("content")
            .GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
    }

    // ====================================================
    // Hugging Face (Free)
    // ====================================================
    public static async Task<string> AskHuggingFace(string apiKey, string prompt, string model = "mistralai/Mistral-7B-Instruct-v0.3")
    {
        var url = $"https://api-inference.huggingface.co/models/{model}";
        var body = JsonSerializer.Serialize(new { inputs = prompt, parameters = new { max_new_tokens = 512 } });
        var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");

        var resp = await Http.SendAsync(req);
        var json = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) return $"❌ Error: {json}";

        var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
            return doc.RootElement[0].GetProperty("generated_text").GetString() ?? "";
        return json;
    }

    // ====================================================
    // Unified: ส่งไปยัง AI ตัวที่เลือก
    // ====================================================
    public static async Task<string> Ask(AIConfig config, string prompt, string system = "")
    {
        return config.DefaultProvider switch
        {
            "claude" when !string.IsNullOrEmpty(config.ClaudeKey) => await AskClaude(config.ClaudeKey, prompt, system),
            "openai" when !string.IsNullOrEmpty(config.OpenAIKey) => await AskOpenAI(config.OpenAIKey, prompt, system),
            "gemini" when !string.IsNullOrEmpty(config.GeminiKey) => await AskGemini(config.GeminiKey, prompt),
            "huggingface" when !string.IsNullOrEmpty(config.HuggingFaceKey) => await AskHuggingFace(config.HuggingFaceKey, prompt),
            _ => "❌ กรุณาตั้งค่า API Key ก่อนใช้งาน"
        };
    }

    public static string GetProviderName(string provider) => provider switch
    {
        "claude" => "🟣 Claude (Anthropic)",
        "openai" => "🟢 GPT (OpenAI)",
        "gemini" => "🔵 Gemini (Google)",
        "huggingface" => "🟡 Hugging Face (Free)",
        _ => provider
    };
}
