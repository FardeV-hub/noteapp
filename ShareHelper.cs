using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NoteApp;

public static class ShareHelper
{
    private static readonly string SettingsFile = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "settings.json");

    // ====================================================
    // Copy to Clipboard
    // ====================================================
    public static void CopyToClipboard(Note note)
    {
        var text = FormatNote(note);
        Clipboard.SetText(text);
    }

    // ====================================================
    // Email — เปิดแอป Email เริ่มต้นของ Windows
    // ====================================================
    public static void SendEmail(Note note)
    {
        var subject = Uri.EscapeDataString($"[โน้ต] {note.Title}");
        var body = Uri.EscapeDataString(FormatNote(note));
        var uri = $"mailto:?subject={subject}&body={body}";
        Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
    }

    // ====================================================
    // Calendar — สร้างไฟล์ .ics เปิดได้ทั้ง Google Calendar / Outlook
    // ====================================================
    public static void ExportToCalendar(Note note, string filePath)
    {
        var now = DateTime.Now;
        DateTime start;
        if (!string.IsNullOrEmpty(note.Reminder) && DateTime.TryParse(note.Reminder, out var rdt))
            start = rdt;
        else
            start = now.AddHours(1);

        var end = start.AddHours(1);
        var uid = Guid.NewGuid().ToString();

        var sb = new StringBuilder();
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//NoteApp//TH//EN");
        sb.AppendLine("BEGIN:VEVENT");
        sb.AppendLine($"UID:{uid}");
        sb.AppendLine($"DTSTART:{start:yyyyMMddTHHmmss}");
        sb.AppendLine($"DTEND:{end:yyyyMMddTHHmmss}");
        sb.AppendLine($"SUMMARY:{EscapeIcs(note.Title)}");
        sb.AppendLine($"DESCRIPTION:{EscapeIcs(note.Content ?? "")}");
        sb.AppendLine($"CATEGORIES:{EscapeIcs(note.Category ?? "ทั่วไป")}");

        if (note.RemindMinutes >= 0)
        {
            sb.AppendLine("BEGIN:VALARM");
            sb.AppendLine("TRIGGER:-PT" + (note.RemindMinutes > 0 ? note.RemindMinutes : 0) + "M");
            sb.AppendLine("ACTION:DISPLAY");
            sb.AppendLine($"DESCRIPTION:{EscapeIcs(note.Title)}");
            sb.AppendLine("END:VALARM");
        }

        sb.AppendLine("END:VEVENT");
        sb.AppendLine("END:VCALENDAR");

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    public static void OpenCalendarFile(string filePath)
    {
        Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
    }

    // ====================================================
    // LINE Notify — ส่งข้อความไป LINE ผ่าน API
    // ====================================================
    public static async Task<bool> SendLineNotify(Note note, string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;

        var message = $"\n📝 {note.Title}\n" +
                      $"📂 หมวด: {note.Category ?? "ทั่วไป"}\n" +
                      $"{note.PriorityIcon} ความสำคัญ: {note.Priority ?? "ปกติ"}\n";

        if (!string.IsNullOrEmpty(note.Reminder))
            message += $"⏰ แจ้งเตือน: {note.Reminder}\n";
        if (!string.IsNullOrEmpty(note.Content))
            message += $"─────────────\n{note.Content}\n";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var content = new FormUrlEncodedContent(new[] {
            new KeyValuePair<string, string>("message", message)
        });

        var response = await client.PostAsync("https://notify-api.line.me/api/notify", content);
        return response.IsSuccessStatusCode;
    }

    // ====================================================
    // LINE Token Settings
    // ====================================================
    public static string LoadLineToken()
    {
        if (!File.Exists(SettingsFile)) return "";
        try
        {
            var json = File.ReadAllText(SettingsFile);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("line_token", out var val) ? val.GetString() ?? "" : "";
        }
        catch { return ""; }
    }

    public static void SaveLineToken(string token)
    {
        var json = $"{{\"line_token\":\"{token.Replace("\"", "\\\"")}\"}}";
        File.WriteAllText(SettingsFile, json);
    }

    // ====================================================
    // Helpers
    // ====================================================
    private static string FormatNote(Note n) =>
        $"📝 {n.Title}\n" +
        $"📂 หมวด: {n.Category ?? "ทั่วไป"}\n" +
        $"{n.PriorityIcon} ความสำคัญ: {n.Priority ?? "ปกติ"}\n" +
        $"📅 สร้าง: {n.Created}\n" +
        (!string.IsNullOrEmpty(n.Reminder) ? $"⏰ แจ้งเตือน: {n.Reminder}\n" : "") +
        $"─────────────────────\n{n.Content ?? ""}";

    private static string EscapeIcs(string s) =>
        s.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\n", "\\n").Replace("\r", "");
}
