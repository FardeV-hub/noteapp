using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NoteApp;

public class IntegrationSettings
{
    public string DiscordWebhook { get; set; } = "";
    public string DiscordBotToken { get; set; } = "";
    public string DiscordChannelId { get; set; } = "";
    public string GoogleApiKey { get; set; } = "";
    public string CourseId { get; set; } = "";
}

public static class IntegrationStore
{
    private static readonly string FilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "integrations.json");

    public static IntegrationSettings Load()
    {
        if (!File.Exists(FilePath)) return new IntegrationSettings();
        try
        {
            return JsonSerializer.Deserialize<IntegrationSettings>(File.ReadAllText(FilePath)) ?? new();
        }
        catch { return new(); }
    }

    public static void Save(IntegrationSettings s)
    {
        File.WriteAllText(FilePath, JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true }));
    }
}

// ====================================================
// Discord Integration
// ====================================================
public static class DiscordHelper
{
    public static async Task<bool> SendWebhook(string webhookUrl, string content, string? username = null)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl)) return false;
        using var client = new HttpClient();
        var payload = new Dictionary<string, object?>
        {
            ["content"] = content,
            ["username"] = username ?? "Note App 📝"
        };
        var json = JsonSerializer.Serialize(payload);
        var response = await client.PostAsync(webhookUrl,
            new StringContent(json, Encoding.UTF8, "application/json"));
        return response.IsSuccessStatusCode;
    }

    public static async Task<bool> SendBookingNotification(string webhookUrl, Booking booking, string roomName)
    {
        var msg = $"📢 **แจ้งเตือนการประชุม**\n\n" +
                  $"📋 **{booking.Topic}**\n" +
                  $"🏢 ห้อง: {roomName}\n" +
                  $"📅 วันที่: {booking.Date}\n" +
                  $"🕐 เวลา: {booking.StartTime} – {booking.EndTime}\n" +
                  $"👤 ผู้จัด: {booking.Organizer}\n";
        if (!string.IsNullOrEmpty(booking.Participants))
            msg += $"👥 ผู้เข้าร่วม: {booking.Participants}\n";

        return await SendWebhook(webhookUrl, msg);
    }

    public static async Task<bool> SendBotMessage(string botToken, string channelId, string content)
    {
        if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(channelId)) return false;
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bot", botToken);

        var payload = JsonSerializer.Serialize(new { content });
        var response = await client.PostAsync(
            $"https://discord.com/api/v10/channels/{channelId}/messages",
            new StringContent(payload, Encoding.UTF8, "application/json"));
        return response.IsSuccessStatusCode;
    }
}

// ====================================================
// Google Classroom Integration
// ====================================================
public class ClassroomItem
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Type { get; set; } = "";
    public string DueDate { get; set; } = "";
    public string Link { get; set; } = "";
}

public static class ClassroomHelper
{
    public static async Task<List<ClassroomItem>> FetchAnnouncements(string apiKey, string courseId)
    {
        var items = new List<ClassroomItem>();
        using var client = new HttpClient();

        var url = $"https://classroom.googleapis.com/v1/courses/{courseId}/announcements?key={apiKey}&pageSize=10";
        var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode) return items;

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("announcements", out var arr)) return items;

        foreach (var el in arr.EnumerateArray())
        {
            items.Add(new ClassroomItem
            {
                Id = el.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                Title = "ประกาศ",
                Description = el.TryGetProperty("text", out var text) ? text.GetString() ?? "" : "",
                Type = "announcement",
                Link = el.TryGetProperty("alternateLink", out var link) ? link.GetString() ?? "" : "",
            });
        }
        return items;
    }

    public static async Task<List<ClassroomItem>> FetchCourseWork(string apiKey, string courseId)
    {
        var items = new List<ClassroomItem>();
        using var client = new HttpClient();

        var url = $"https://classroom.googleapis.com/v1/courses/{courseId}/courseWork?key={apiKey}&pageSize=10&orderBy=dueDate desc";
        var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode) return items;

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("courseWork", out var arr)) return items;

        foreach (var el in arr.EnumerateArray())
        {
            var due = "";
            if (el.TryGetProperty("dueDate", out var dueProp))
            {
                var y = dueProp.TryGetProperty("year", out var yp) ? yp.GetInt32() : 0;
                var m = dueProp.TryGetProperty("month", out var mp) ? mp.GetInt32() : 0;
                var d = dueProp.TryGetProperty("day", out var dp) ? dp.GetInt32() : 0;
                if (y > 0) due = $"{y:D4}-{m:D2}-{d:D2}";
            }

            items.Add(new ClassroomItem
            {
                Id = el.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                Title = el.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
                Description = el.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                Type = "coursework",
                DueDate = due,
                Link = el.TryGetProperty("alternateLink", out var link) ? link.GetString() ?? "" : "",
            });
        }
        return items;
    }

    public static async Task<bool> PostAnnouncement(string apiKey, string courseId, string text)
    {
        using var client = new HttpClient();
        var url = $"https://classroom.googleapis.com/v1/courses/{courseId}/announcements?key={apiKey}";
        var payload = JsonSerializer.Serialize(new { text });
        var response = await client.PostAsync(url,
            new StringContent(payload, Encoding.UTF8, "application/json"));
        return response.IsSuccessStatusCode;
    }
}
