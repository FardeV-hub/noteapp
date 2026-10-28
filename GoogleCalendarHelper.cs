using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NoteApp;

public class CalendarEvent
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Start { get; set; } = "";
    public string End { get; set; } = "";
    public string Location { get; set; } = "";
    public string Status { get; set; } = "";
    public string Link { get; set; } = "";
}

public static class GoogleCalendarConfig
{
    private static readonly string ConfigFile = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "gcal_config.json");

    public static (string ApiKey, string CalendarId) Load()
    {
        if (!File.Exists(ConfigFile)) return ("", "primary");
        try
        {
            var doc = JsonDocument.Parse(File.ReadAllText(ConfigFile));
            return (doc.RootElement.GetProperty("api_key").GetString() ?? "",
                    doc.RootElement.GetProperty("calendar_id").GetString() ?? "primary");
        }
        catch { return ("", "primary"); }
    }

    public static void Save(string apiKey, string calendarId) =>
        File.WriteAllText(ConfigFile, JsonSerializer.Serialize(
            new { api_key = apiKey, calendar_id = calendarId },
            new JsonSerializerOptions { WriteIndented = true }));
}

public static class GoogleCalendarHelper
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    // ดึง Events จาก Google Calendar
    public static async Task<List<CalendarEvent>> FetchEvents(string apiKey, string calendarId, DateTime from, DateTime to)
    {
        var events = new List<CalendarEvent>();
        var timeMin = from.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        var timeMax = to.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        var url = $"https://www.googleapis.com/calendar/v3/calendars/{Uri.EscapeDataString(calendarId)}/events" +
                  $"?key={apiKey}&timeMin={timeMin}&timeMax={timeMax}&singleEvents=true&orderBy=startTime&maxResults=50";

        var resp = await Http.GetAsync(url);
        var json = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) throw new Exception($"Google Calendar Error: {json}");

        var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("items", out var items)) return events;

        foreach (var item in items.EnumerateArray())
        {
            var ev = new CalendarEvent
            {
                Id = item.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                Title = item.TryGetProperty("summary", out var sum) ? sum.GetString() ?? "(ไม่มีชื่อ)" : "(ไม่มีชื่อ)",
                Description = item.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                Location = item.TryGetProperty("location", out var loc) ? loc.GetString() ?? "" : "",
                Status = item.TryGetProperty("status", out var st) ? st.GetString() ?? "" : "",
                Link = item.TryGetProperty("htmlLink", out var link) ? link.GetString() ?? "" : "",
            };

            if (item.TryGetProperty("start", out var startEl))
            {
                ev.Start = startEl.TryGetProperty("dateTime", out var dt) ? dt.GetString() ?? "" :
                           startEl.TryGetProperty("date", out var d) ? d.GetString() ?? "" : "";
            }
            if (item.TryGetProperty("end", out var endEl))
            {
                ev.End = endEl.TryGetProperty("dateTime", out var dt) ? dt.GetString() ?? "" :
                         endEl.TryGetProperty("date", out var d) ? d.GetString() ?? "" : "";
            }

            events.Add(ev);
        }
        return events;
    }

    // ดึง Events วันนี้
    public static async Task<List<CalendarEvent>> FetchToday(string apiKey, string calendarId)
    {
        var today = DateTime.Today;
        return await FetchEvents(apiKey, calendarId, today, today.AddDays(1));
    }

    // ดึง Events สัปดาห์นี้
    public static async Task<List<CalendarEvent>> FetchThisWeek(string apiKey, string calendarId)
    {
        var today = DateTime.Today;
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
        return await FetchEvents(apiKey, calendarId, startOfWeek, startOfWeek.AddDays(7));
    }

    // ดึง Events เดือนนี้
    public static async Task<List<CalendarEvent>> FetchThisMonth(string apiKey, string calendarId)
    {
        var first = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        return await FetchEvents(apiKey, calendarId, first, first.AddMonths(1));
    }

    // แปลง Note → Calendar Event (สร้างไฟล์ .ics)
    public static void ExportNoteToICS(Note note, string filePath)
    {
        ShareHelper.ExportToCalendar(note, filePath);
    }

    // แปลง CalendarEvent → Note
    public static Note EventToNote(CalendarEvent ev)
    {
        var start = "";
        if (DateTime.TryParse(ev.Start, out var dt))
            start = dt.ToString("yyyy-MM-dd HH:mm");

        return new Note
        {
            Title = $"[📅 Calendar] {ev.Title}",
            Content = $"📅 เวลา: {ev.Start} → {ev.End}\n" +
                      (!string.IsNullOrEmpty(ev.Location) ? $"📍 สถานที่: {ev.Location}\n" : "") +
                      (!string.IsNullOrEmpty(ev.Description) ? $"\n{ev.Description}" : ""),
            Category = "งาน",
            Priority = "ปานกลาง",
            Reminder = start,
            RemindMinutes = 15,
        };
    }

    // Format เวลาให้อ่านง่าย
    public static string FormatTime(string isoTime)
    {
        if (DateTime.TryParse(isoTime, out var dt))
            return dt.ToString("HH:mm");
        return isoTime;
    }

    public static string FormatDate(string isoTime)
    {
        if (DateTime.TryParse(isoTime, out var dt))
            return dt.ToString("dd/MM/yyyy");
        return isoTime;
    }
}
