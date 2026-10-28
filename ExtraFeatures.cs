using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NoteApp;

// ====================================================
// Telegram Bot
// ====================================================
public static class TelegramHelper
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly string ConfigFile = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "telegram.json");

    public static (string Token, string ChatId) LoadConfig()
    {
        if (!File.Exists(ConfigFile)) return ("", "");
        try
        {
            var doc = JsonDocument.Parse(File.ReadAllText(ConfigFile));
            return (doc.RootElement.GetProperty("token").GetString() ?? "",
                    doc.RootElement.GetProperty("chat_id").GetString() ?? "");
        }
        catch { return ("", ""); }
    }

    public static void SaveConfig(string token, string chatId) =>
        File.WriteAllText(ConfigFile, JsonSerializer.Serialize(new { token, chat_id = chatId }));

    public static async Task<bool> SendMessage(string token, string chatId, string text)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(chatId)) return false;
        var url = $"https://api.telegram.org/bot{token}/sendMessage";
        var body = JsonSerializer.Serialize(new { chat_id = chatId, text, parse_mode = "HTML" });
        var resp = await Http.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json"));
        return resp.IsSuccessStatusCode;
    }

    public static async Task<bool> SendNote(string token, string chatId, Note note)
    {
        var text = $"📝 <b>{note.Title}</b>\n" +
                   $"📂 {note.Category ?? "ทั่วไป"} | {note.PriorityIcon} {note.Priority}\n" +
                   (!string.IsNullOrEmpty(note.Reminder) ? $"⏰ {note.Reminder}\n" : "") +
                   $"─────────\n{note.Content ?? ""}";
        return await SendMessage(token, chatId, text);
    }
}

// ====================================================
// Quick Note Popup
// ====================================================
public class QuickNoteWindow : Window
{
    private readonly Action<Note> _addNote;
    private readonly TextBox _txtTitle;
    private readonly TextBox _txtContent;

    public QuickNoteWindow(Action<Note> addNote)
    {
        _addNote = addNote;
        Title = "Quick Note";
        Width = 380; Height = 230;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Topmost = true;

        var border = new Border
        {
            Background = B("#12141e"), CornerRadius = new CornerRadius(12),
            BorderBrush = B("#2a2d45"), BorderThickness = new Thickness(1),
            Padding = new Thickness(18)
        };

        var sp = new StackPanel();
        sp.Children.Add(new TextBlock { Text = "Quick Note", FontSize = 15, FontWeight = FontWeights.SemiBold,
            Foreground = B("#e8e9f0"), Margin = new Thickness(0, 0, 0, 4) });
        sp.Children.Add(new TextBlock { Text = "Ctrl+Enter บันทึก · Esc ปิด", FontSize = 10,
            Foreground = B("#5a5d78"), Margin = new Thickness(0, 0, 0, 10) });

        _txtTitle = new TextBox { FontSize = 13, Padding = new Thickness(10, 8, 10, 8),
            Background = B("#1a1c2c"), Foreground = B("#e0e2eb"), BorderBrush = B("#2a2d45"),
            BorderThickness = new Thickness(1), CaretBrush = B("#6c63ff") };
        sp.Children.Add(_txtTitle);

        _txtContent = new TextBox { FontSize = 12, Padding = new Thickness(10, 8, 10, 8), Height = 60,
            AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0),
            Background = B("#1a1c2c"), Foreground = B("#e0e2eb"), BorderBrush = B("#2a2d45"),
            BorderThickness = new Thickness(1), CaretBrush = B("#6c63ff") };
        sp.Children.Add(_txtContent);

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
        var closeBtn = new Button { Content = "ปิด", Padding = new Thickness(14, 6, 14, 6),
            Background = B("#1c1e30"), Foreground = B("#5a5d78"), BorderThickness = new Thickness(0), Cursor = Cursors.Hand };
        closeBtn.Click += (_, _) => Close();
        btnRow.Children.Add(closeBtn);

        var saveBtn = new Button { Content = "บันทึก", Padding = new Thickness(14, 6, 14, 6),
            Background = B("#6c63ff"), Foreground = Brushes.White, BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand, FontWeight = FontWeights.SemiBold, Margin = new Thickness(6, 0, 0, 0) };
        saveBtn.Click += (_, _) => Save();
        btnRow.Children.Add(saveBtn);
        sp.Children.Add(btnRow);

        border.Child = sp;
        Content = border;

        MouseLeftButtonDown += (_, _) => { try { DragMove(); } catch { } };
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) Close();
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control) Save();
        };
        Loaded += (_, _) => _txtTitle.Focus();
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(_txtTitle.Text)) return;
        _addNote(new Note { Title = _txtTitle.Text.Trim(), Content = _txtContent.Text.Trim() });
        Close();
    }

    private static SolidColorBrush B(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
}

// ====================================================
// AI Spending Alert
// ====================================================
public static class SpendingAlert
{
    public static List<string> Analyze(FinanceData data)
    {
        var alerts = new List<string>();
        var now = DateTime.Now;
        var thisMonth = now.ToString("yyyy-MM");
        var lastMonth = now.AddMonths(-1).ToString("yyyy-MM");

        var thisExpense = data.Transactions.Where(t => t.Date.StartsWith(thisMonth) && t.Type == "expense").Sum(t => t.Amount);
        var lastExpense = data.Transactions.Where(t => t.Date.StartsWith(lastMonth) && t.Type == "expense").Sum(t => t.Amount);

        if (lastExpense > 0 && thisExpense > lastExpense * 1.3)
            alerts.Add($"⚠️ รายจ่ายเดือนนี้ ฿{thisExpense:N0} สูงกว่าเดือนก่อน {((thisExpense / lastExpense - 1) * 100):F0}%");

        // Category spike
        var thisCats = data.Transactions.Where(t => t.Date.StartsWith(thisMonth) && t.Type == "expense")
            .GroupBy(t => t.Category).ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
        var lastCats = data.Transactions.Where(t => t.Date.StartsWith(lastMonth) && t.Type == "expense")
            .GroupBy(t => t.Category).ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        foreach (var (cat, amt) in thisCats)
        {
            if (lastCats.TryGetValue(cat, out var lastAmt) && lastAmt > 0 && amt > lastAmt * 1.5)
                alerts.Add($"📊 หมวด \"{cat}\" จ่ายเพิ่มขึ้น {((amt / lastAmt - 1) * 100):F0}% (฿{lastAmt:N0} → ฿{amt:N0})");
        }

        // Overdue bills
        var overdue = data.Bills.Count(b => b.IsOverdue);
        if (overdue > 0) alerts.Add($"🔴 มีบิลเลยกำหนด {overdue} รายการ!");

        // Low balance
        var balance = data.Accounts.Sum(a => a.Balance);
        if (balance > 0 && balance < thisExpense * 2)
            alerts.Add($"💰 ยอดเงินเหลือน้อย (฿{balance:N0}) — เพียงพอแค่ ~{(balance / Math.Max(thisExpense, 1)):F1} เดือน");

        if (!alerts.Any()) alerts.Add("✅ การใช้จ่ายปกติ ไม่มีสิ่งผิดปกติ");
        return alerts;
    }
}
