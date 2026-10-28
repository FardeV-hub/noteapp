using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace NoteApp;

// ====================================================
// AI Auto-Categorize
// ====================================================
public static class AIHelper
{
    private static readonly Dictionary<string, string[]> CategoryKeywords = new()
    {
        ["งาน"] = new[] { "ประชุม", "โปรเจกต์", "deadline", "ส่งงาน", "รายงาน", "ลูกค้า", "อีเมล",
            "นำเสนอ", "presentation", "meeting", "ทีม", "บริษัท", "สัญญา", "เอกสาร", "งบ" },
        ["สุขภาพ"] = new[] { "หมอ", "โรงพยาบาล", "ยา", "ออกกำลังกาย", "สุขภาพ", "ตรวจ", "นัด",
            "เลือด", "ฉีดวัคซีน", "อาการ", "วิตามิน", "น้ำหนัก", "อาหาร", "พักผ่อน" },
        ["การเงิน"] = new[] { "เงิน", "บิล", "จ่าย", "โอน", "ค่า", "ธนาคาร", "บัญชี", "ภาษี",
            "เงินเดือน", "ออม", "ลงทุน", "หนี้", "บัตรเครดิต", "ผ่อน", "ค่าเช่า" },
        ["การเรียน"] = new[] { "เรียน", "สอบ", "การบ้าน", "อ่านหนังสือ", "วิชา", "ครู", "อาจารย์",
            "มหาวิทยาลัย", "โรงเรียน", "คอร์ส", "เกรด", "วิจัย", "thesis" },
        ["ส่วนตัว"] = new[] { "ซื้อ", "ช้อปปิ้ง", "ท่องเที่ยว", "วันเกิด", "นัดเจอ", "เพื่อน",
            "ครอบครัว", "หยุด", "พักร้อน", "งานเลี้ยง", "ของขวัญ", "เสื้อผ้า" },
        ["ไอเดีย"] = new[] { "ไอเดีย", "idea", "คิด", "ลอง", "แผน", "อยากจะ", "น่าจะ",
            "ถ้า", "อนาคต", "โปรเจกต์ส่วนตัว", "สร้าง", "ออกแบบ", "พัฒนา" },
        ["สำคัญ"] = new[] { "ด่วน", "สำคัญมาก", "ห้ามลืม", "urgent", "important", "ต้องทำ",
            "อันตราย", "เร่ง", "วันสุดท้าย", "จำเป็น" },
    };

    public static string SuggestCategory(string title, string content)
    {
        var text = (title + " " + content).ToLower();
        var scores = new Dictionary<string, int>();

        foreach (var (cat, keywords) in CategoryKeywords)
        {
            var score = keywords.Count(kw => text.Contains(kw.ToLower()));
            if (score > 0) scores[cat] = score;
        }

        return scores.Any() ? scores.OrderByDescending(x => x.Value).First().Key : "ทั่วไป";
    }

    public static string SuggestPriority(string title, string content)
    {
        var text = (title + " " + content).ToLower();
        var urgentWords = new[] { "ด่วน", "urgent", "สำคัญมาก", "ห้ามลืม", "เร่ง", "ต้องทำเลย", "วันสุดท้าย" };
        var medWords = new[] { "สำคัญ", "ควร", "important", "อย่าลืม", "เตรียม" };

        if (urgentWords.Any(w => text.Contains(w))) return "สูง";
        if (medWords.Any(w => text.Contains(w))) return "ปานกลาง";
        return "ปกติ";
    }
}

// ====================================================
// Note Templates
// ====================================================
public static class NoteTemplates
{
    public static readonly (string Name, string Icon, string Title, string Content, string Category)[] Templates = {
        ("ประชุม", "🏢", "ประชุม — [หัวข้อ]",
            "📅 วันที่: \n👥 ผู้เข้าร่วม: \n\n📋 วาระการประชุม:\n1. \n2. \n3. \n\n📝 สรุปการประชุม:\n- \n\n✅ สิ่งที่ต้องทำต่อ:\n☐ \n☐ ", "งาน"),

        ("To-Do List", "✅", "สิ่งที่ต้องทำ — [วันที่]",
            "☐ \n☐ \n☐ \n☐ \n☐ \n\n📝 หมายเหตุ:\n", "ทั่วไป"),

        ("ไดอารี่", "📔", "บันทึกประจำวัน — [วันที่]",
            "🌤️ อารมณ์วันนี้: \n\n📝 เรื่องที่เกิดขึ้น:\n\n\n💡 สิ่งที่เรียนรู้:\n\n\n🙏 ขอบคุณสำหรับ:\n", "ส่วนตัว"),

        ("รายรับรายจ่าย", "💰", "รายรับรายจ่าย — [เดือน/ปี]",
            "💵 รายรับ:\n- เงินเดือน: \n- อื่นๆ: \n\n💸 รายจ่าย:\n- ค่าอาหาร: \n- ค่าเดินทาง: \n- ค่าที่พัก: \n- อื่นๆ: \n\n📊 สรุป:\n- รวมรายรับ: \n- รวมรายจ่าย: \n- คงเหลือ: ", "การเงิน"),

        ("เป้าหมาย", "🎯", "เป้าหมาย — [ระยะเวลา]",
            "🎯 เป้าหมายหลัก:\n\n\n📋 ขั้นตอน:\n1. \n2. \n3. \n\n📅 กำหนดเสร็จ: \n\n📈 วัดผลโดย:\n\n💪 แรงจูงใจ:\n", "ไอเดีย"),

        ("บันทึกสุขภาพ", "🏥", "บันทึกสุขภาพ — [วันที่]",
            "🏥 แพทย์: \n📍 สถานที่: \n\n💊 อาการ:\n\n\n💉 การรักษา/ยา:\n- \n\n📅 นัดครั้งต่อไป: \n\n📝 หมายเหตุ:\n", "สุขภาพ"),

        ("ไอเดียโปรเจกต์", "💡", "ไอเดีย — [ชื่อโปรเจกต์]",
            "💡 แนวคิด:\n\n\n🎯 เป้าหมาย:\n\n\n👥 กลุ่มเป้าหมาย:\n\n\n🛠️ เครื่องมือ/ทรัพยากร:\n- \n\n📋 ขั้นตอนแรกที่ต้องทำ:\n1. \n2. \n3. ", "ไอเดีย"),

        ("Checklist", "☑️", "Checklist — [หัวข้อ]",
            "☐ รายการ 1\n☐ รายการ 2\n☐ รายการ 3\n☐ รายการ 4\n☐ รายการ 5", "ทั่วไป"),
    };
}

// ====================================================
// Backup & Sync
// ====================================================
public static class BackupHelper
{
    private static readonly string BackupDir = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "backups");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string CreateBackup(List<Note> notes)
    {
        Directory.CreateDirectory(BackupDir);
        var filename = $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        var path = Path.Combine(BackupDir, filename);
        File.WriteAllText(path, JsonSerializer.Serialize(notes, JsonOpts));

        CleanOldBackups(10);
        return path;
    }

    public static void ExportJson(List<Note> notes, string path)
    {
        var data = new { version = "1.0", exported = DateTime.Now.ToString("yyyy-MM-dd HH:mm"), count = notes.Count, notes };
        File.WriteAllText(path, JsonSerializer.Serialize(data, JsonOpts));
    }

    public static List<Note>? ImportJson(string path)
    {
        var json = File.ReadAllText(path);
        var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("notes", out var notesEl))
            return JsonSerializer.Deserialize<List<Note>>(notesEl.GetRawText(), JsonOpts);

        if (doc.RootElement.ValueKind == JsonValueKind.Array)
            return JsonSerializer.Deserialize<List<Note>>(json, JsonOpts);

        return null;
    }

    private static void CleanOldBackups(int keepCount)
    {
        var files = Directory.GetFiles(BackupDir, "backup_*.json")
            .OrderByDescending(f => f).Skip(keepCount);
        foreach (var f in files) File.Delete(f);
    }

    public static int GetBackupCount()
    {
        if (!Directory.Exists(BackupDir)) return 0;
        return Directory.GetFiles(BackupDir, "backup_*.json").Length;
    }
}

// ====================================================
// Share as HTML
// ====================================================
public static class ShareHTMLHelper
{
    public static void GenerateHTML(List<Note> notes, string path, string title = "โน้ตที่แชร์")
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html lang='th'><head><meta charset='UTF-8'>");
        sb.AppendLine("<meta name='viewport' content='width=device-width,initial-scale=1'>");
        sb.AppendLine($"<title>{Esc(title)}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("*{margin:0;padding:0;box-sizing:border-box}");
        sb.AppendLine("body{font-family:'Segoe UI',sans-serif;background:#0f0f1a;color:#e0e0e0;padding:20px;max-width:700px;margin:0 auto}");
        sb.AppendLine("h1{font-size:22px;margin-bottom:4px}");
        sb.AppendLine(".sub{color:#8888aa;font-size:12px;margin-bottom:20px}");
        sb.AppendLine(".card{background:#1a1a2e;border-radius:12px;padding:16px;margin-bottom:10px}");
        sb.AppendLine(".card .title{font-size:15px;font-weight:bold;margin-bottom:6px}");
        sb.AppendLine(".badge{display:inline-block;background:#222240;padding:2px 8px;border-radius:6px;font-size:11px;color:#8888aa;margin-right:6px}");
        sb.AppendLine(".content{margin-top:8px;white-space:pre-wrap;color:#bbb;font-size:13px;line-height:1.6}");
        sb.AppendLine(".date{font-size:11px;color:#555577;float:right}");
        sb.AppendLine(".pri-high{color:#e74c3c}.pri-med{color:#f1c40f}.pri-low{color:#2ecc71}");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine($"<h1>📝 {Esc(title)}</h1>");
        sb.AppendLine($"<div class='sub'>แชร์เมื่อ: {DateTime.Now:dd/MM/yyyy HH:mm} · {notes.Count} โน้ต</div>");

        foreach (var n in notes.OrderBy(x => x.PriorityRank))
        {
            var priClass = n.Priority switch { "สูง" => "pri-high", "ปานกลาง" => "pri-med", "ต่ำ" => "pri-low", _ => "" };
            sb.AppendLine("<div class='card'>");
            sb.AppendLine($"<div class='title'>{(n.Pinned ? "📌 " : "")}{n.PriorityIcon} {Esc(n.Title)}{(!string.IsNullOrEmpty(n.Reminder) ? " ⏰" : "")}</div>");
            sb.AppendLine($"<span class='badge'>{Esc(n.Category ?? "ทั่วไป")}</span>");
            sb.AppendLine($"<span class='badge {priClass}'>{Esc(n.Priority ?? "ปกติ")}</span>");
            sb.AppendLine($"<span class='date'>{Esc(n.Created)}</span>");
            if (!string.IsNullOrEmpty(n.Content))
                sb.AppendLine($"<div class='content'>{Esc(n.Content).Replace("☐", "☐").Replace("☑", "☑")}</div>");
            sb.AppendLine("</div>");
        }

        sb.AppendLine("<div style='text-align:center;margin-top:20px;color:#555577;font-size:11px'>สร้างด้วย Note App</div>");
        sb.AppendLine("</body></html>");
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static string Esc(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("\n", "<br>");
}
