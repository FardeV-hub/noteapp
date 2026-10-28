using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace NoteApp;

public class CloudConfig
{
    public string FirebaseUrl { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string LastSync { get; set; } = "";
    public string GistToken { get; set; } = "";
    public string GistId { get; set; } = "";
    public string SyncMode { get; set; } = "gist";
}

public static class CloudConfigStore
{
    private static readonly string FilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "cloud_config.json");
    private static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static CloudConfig Load()
    {
        if (!File.Exists(FilePath)) return new();
        try { return JsonSerializer.Deserialize<CloudConfig>(File.ReadAllText(FilePath), Opts) ?? new(); }
        catch { return new(); }
    }

    public static void Save(CloudConfig config) =>
        File.WriteAllText(FilePath, JsonSerializer.Serialize(config, Opts));
}

public static class CloudSyncService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static string BuildUrl(string baseUrl, string path, string apiKey)
    {
        var url = baseUrl.TrimEnd('/');
        if (!url.EndsWith(".firebaseio.com")) url += ".firebaseio.com";
        if (!url.StartsWith("http")) url = "https://" + url;
        var auth = string.IsNullOrEmpty(apiKey) ? "" : $"?auth={apiKey}";
        return $"{url}/{path}.json{auth}";
    }

    public static async Task<bool> PushNotes(CloudConfig config, List<Note> notes, string user)
    {
        var url = BuildUrl(config.FirebaseUrl, $"users/{SanitizeKey(user)}/notes", config.ApiKey);
        var json = JsonSerializer.Serialize(notes, Opts);
        var resp = await Http.PutAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
        return resp.IsSuccessStatusCode;
    }

    public static async Task<List<Note>?> PullNotes(CloudConfig config, string user)
    {
        var url = BuildUrl(config.FirebaseUrl, $"users/{SanitizeKey(user)}/notes", config.ApiKey);
        var resp = await Http.GetAsync(url);
        if (!resp.IsSuccessStatusCode) return null;
        var json = await resp.Content.ReadAsStringAsync();
        if (json == "null" || string.IsNullOrWhiteSpace(json)) return new();
        return JsonSerializer.Deserialize<List<Note>>(json, Opts);
    }

    public static async Task<bool> PushCommunity(CloudConfig config, List<CommunityPost> posts)
    {
        var url = BuildUrl(config.FirebaseUrl, "community", config.ApiKey);
        var json = JsonSerializer.Serialize(posts, Opts);
        var resp = await Http.PutAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
        return resp.IsSuccessStatusCode;
    }

    public static async Task<List<CommunityPost>?> PullCommunity(CloudConfig config)
    {
        var url = BuildUrl(config.FirebaseUrl, "community", config.ApiKey);
        var resp = await Http.GetAsync(url);
        if (!resp.IsSuccessStatusCode) return null;
        var json = await resp.Content.ReadAsStringAsync();
        if (json == "null" || string.IsNullOrWhiteSpace(json)) return new();
        return JsonSerializer.Deserialize<List<CommunityPost>>(json, Opts);
    }

    public static async Task<bool> TestConnection(CloudConfig config)
    {
        try
        {
            var url = BuildUrl(config.FirebaseUrl, "ping", config.ApiKey);
            var resp = await Http.PutAsync(url,
                new StringContent($"\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\"", Encoding.UTF8, "application/json"));
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private static string SanitizeKey(string key) =>
        key.Replace(".", "_").Replace("#", "_").Replace("$", "_")
           .Replace("[", "_").Replace("]", "_").Replace("/", "_");
}

public static class GistSyncService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static HttpRequestMessage AuthRequest(HttpMethod method, string url, string token, HttpContent? content = null)
    {
        var req = new HttpRequestMessage(method, url) { Content = content };
        req.Headers.Add("Authorization", $"Bearer {token}");
        req.Headers.Add("User-Agent", "NoteApp");
        req.Headers.Add("Accept", "application/vnd.github+json");
        return req;
    }

    public static async Task<string?> CreateGist(string token, string description = "NoteApp Cloud Data")
    {
        var payload = new
        {
            description,
            @public = false,
            files = new Dictionary<string, object>
            {
                ["noteapp_notes.json"] = new { content = "[]" },
                ["noteapp_community.json"] = new { content = "[]" },
            }
        };
        var json = JsonSerializer.Serialize(payload, Opts);
        var req = AuthRequest(HttpMethod.Post, "https://api.github.com/gists", token,
            new StringContent(json, Encoding.UTF8, "application/json"));
        var resp = await Http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return null;
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetString();
    }

    public static async Task<bool> TestConnection(string token, string gistId)
    {
        try
        {
            var req = AuthRequest(HttpMethod.Get, $"https://api.github.com/gists/{gistId}", token);
            var resp = await Http.SendAsync(req);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public static async Task<bool> PushData(string token, string gistId, string filename, string jsonContent)
    {
        var payload = new
        {
            files = new Dictionary<string, object>
            {
                [filename] = new { content = jsonContent }
            }
        };
        var json = JsonSerializer.Serialize(payload, Opts);
        var req = AuthRequest(new HttpMethod("PATCH"), $"https://api.github.com/gists/{gistId}", token,
            new StringContent(json, Encoding.UTF8, "application/json"));
        var resp = await Http.SendAsync(req);
        return resp.IsSuccessStatusCode;
    }

    public static async Task<string?> PullData(string token, string gistId, string filename)
    {
        var req = AuthRequest(HttpMethod.Get, $"https://api.github.com/gists/{gistId}", token);
        var resp = await Http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return null;
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        if (doc.RootElement.TryGetProperty("files", out var files) &&
            files.TryGetProperty(filename, out var file) &&
            file.TryGetProperty("content", out var content))
        {
            return content.GetString();
        }
        return null;
    }

    public static async Task<bool> PushNotes(string token, string gistId, List<Note> notes)
    {
        var json = JsonSerializer.Serialize(notes, Opts);
        return await PushData(token, gistId, "noteapp_notes.json", json);
    }

    public static async Task<List<Note>?> PullNotes(string token, string gistId)
    {
        var json = await PullData(token, gistId, "noteapp_notes.json");
        if (json == null) return null;
        try { return JsonSerializer.Deserialize<List<Note>>(json, Opts) ?? new(); }
        catch { return new(); }
    }

    public static async Task<bool> PushCommunity(string token, string gistId, List<CommunityPost> posts)
    {
        var json = JsonSerializer.Serialize(posts, Opts);
        return await PushData(token, gistId, "noteapp_community.json", json);
    }

    public static async Task<List<CommunityPost>?> PullCommunity(string token, string gistId)
    {
        var json = await PullData(token, gistId, "noteapp_community.json");
        if (json == null) return null;
        try { return JsonSerializer.Deserialize<List<CommunityPost>>(json, Opts) ?? new(); }
        catch { return new(); }
    }
}
