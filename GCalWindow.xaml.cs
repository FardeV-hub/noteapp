using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NoteApp;

public partial class GCalWindow : Window
{
    private readonly Action<Note> _addNote;

    public GCalWindow(Action<Note> addNote)
    {
        InitializeComponent();
        _addNote = addNote;

        var (key, calId) = GoogleCalendarConfig.Load();
        txtGCalKey.Text = key;
        txtCalId.Text = calId;

        if (!string.IsNullOrEmpty(key)) _ = LoadEvents();
        else lblGCalStatus.Text = "⚙️ กรุณาตั้งค่า API Key ในแท็บ ⚙️ ตั้งค่า";
    }

    private void SaveGCalConfig_Click(object s, RoutedEventArgs e)
    {
        GoogleCalendarConfig.Save(txtGCalKey.Text.Trim(), txtCalId.Text.Trim());
        lblGCalStatus.Text = "✅ บันทึกสำเร็จ";
        MessageBox.Show("บันทึกการตั้งค่าสำเร็จ!\nกดปุ่ม 🔄 รีเฟรช เพื่อดึงข้อมูล", "💾");
    }

    private async void Refresh_Click(object s, RoutedEventArgs e) => await LoadEvents();

    private async System.Threading.Tasks.Task LoadEvents()
    {
        var (key, calId) = GoogleCalendarConfig.Load();
        if (string.IsNullOrEmpty(key)) { lblGCalStatus.Text = "❌ ยังไม่ได้ตั้งค่า API Key"; return; }

        lblGCalStatus.Text = "⏳ กำลังดึงข้อมูล...";
        try
        {
            var today = await GoogleCalendarHelper.FetchToday(key, calId);
            var week = await GoogleCalendarHelper.FetchThisWeek(key, calId);
            var month = await GoogleCalendarHelper.FetchThisMonth(key, calId);

            RenderEvents(panelToday, today, $"📋 วันนี้ ({DateTime.Today:dd/MM/yyyy})", true);
            RenderEvents(panelWeek, week, "📅 สัปดาห์นี้", false);
            RenderEvents(panelMonth, month, $"📆 {DateTime.Today:MMMM yyyy}", false);

            lblGCalStatus.Text = $"✅ อัพเดท {DateTime.Now:HH:mm} — วันนี้ {today.Count}, สัปดาห์ {week.Count}, เดือน {month.Count} นัด";
        }
        catch (Exception ex)
        {
            lblGCalStatus.Text = $"❌ โหลดไม่สำเร็จ";
            panelToday.Children.Clear();
            panelToday.Children.Add(Txt($"❌ เกิดข้อผิดพลาด:\n{ex.Message}\n\nตรวจสอบ:\n• API Key ถูกต้องไหม\n• เปิด Calendar API แล้วหรือยัง\n• ปฏิทินเป็น Public หรือไม่", 13, "#e74c3c"));
        }
    }

    private void RenderEvents(StackPanel panel, List<CalendarEvent> events, string title, bool showImport)
    {
        panel.Children.Clear();
        panel.Children.Add(Txt(title, 16, "#e8e9f0", true));
        panel.Children.Add(Txt($"{events.Count} รายการ", 11, "#8b8fa7"));

        if (!events.Any())
        {
            panel.Children.Add(Txt("📭 ไม่มีนัดหมาย", 14, "#8b8fa7"));
            return;
        }

        // Group by date
        var grouped = events.GroupBy(e => GoogleCalendarHelper.FormatDate(e.Start)).OrderBy(g => g.Key);

        foreach (var group in grouped)
        {
            panel.Children.Add(new Border
            {
                Height = 1, Background = B("#2a2d45"), Margin = new Thickness(0, 10, 0, 4)
            });
            panel.Children.Add(Txt($"📅 {group.Key}", 13, "#8b8fa7", true));

            foreach (var ev in group.OrderBy(e => e.Start))
            {
                var card = new Border
                {
                    Background = B("#161828"), CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(12), Margin = new Thickness(0, 4, 0, 0),
                    BorderBrush = B("#4285f4"), BorderThickness = new Thickness(2, 0, 0, 0)
                };

                var sp = new StackPanel();

                // Title + time
                var topRow = new DockPanel();
                topRow.Children.Add(Txt($"📌 {ev.Title}", 14, "#e8e9f0", true));
                var timeTxt = Txt($"🕐 {GoogleCalendarHelper.FormatTime(ev.Start)}–{GoogleCalendarHelper.FormatTime(ev.End)}", 12, "#4285f4");
                timeTxt.HorizontalAlignment = HorizontalAlignment.Right;
                DockPanel.SetDock(timeTxt, Dock.Right);
                topRow.Children.Insert(0, timeTxt);
                sp.Children.Add(topRow);

                // Location
                if (!string.IsNullOrEmpty(ev.Location))
                    sp.Children.Add(Txt($"📍 {ev.Location}", 11, "#8b8fa7"));

                // Description preview
                if (!string.IsNullOrEmpty(ev.Description))
                {
                    var preview = ev.Description.Length > 80 ? ev.Description[..80] + "..." : ev.Description;
                    sp.Children.Add(Txt(preview, 11, "#5a5d70"));
                }

                // Buttons
                if (showImport)
                {
                    var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };

                    var importBtn = MakeBtn("📝 สร้างโน้ต", "#27ae60");
                    var evRef = ev;
                    importBtn.Click += (_, _) =>
                    {
                        var note = GoogleCalendarHelper.EventToNote(evRef);
                        _addNote(note);
                        MessageBox.Show($"สร้างโน้ตจาก Calendar สำเร็จ!\n\n📝 {note.Title}", "📅");
                    };
                    btnRow.Children.Add(importBtn);

                    if (!string.IsNullOrEmpty(ev.Link))
                    {
                        var openBtn = MakeBtn("🔗 เปิดใน Google", "#4285f4");
                        openBtn.Click += (_, _) => System.Diagnostics.Process.Start(
                            new System.Diagnostics.ProcessStartInfo(evRef.Link) { UseShellExecute = true });
                        openBtn.Margin = new Thickness(6, 0, 0, 0);
                        btnRow.Children.Add(openBtn);
                    }

                    sp.Children.Add(btnRow);
                }

                card.Child = sp;
                panel.Children.Add(card);
            }
        }
    }

    private static TextBlock Txt(string text, double size, string color, bool bold = false) => new()
    {
        Text = text, FontSize = size, Foreground = B(color),
        FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
        TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0)
    };
    private static Button MakeBtn(string text, string bg) => new()
    {
        Content = text, Padding = new Thickness(10, 5, 10, 5), FontSize = 11,
        Background = B(bg), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand
    };
    private static SolidColorBrush B(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
}
