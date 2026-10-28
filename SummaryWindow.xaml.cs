using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace NoteApp;

public partial class SummaryWindow : Window
{
    private readonly List<Note> _notes;
    private readonly StringBuilder _exportText = new();

    private static readonly (string Label, string Priority, string Color, string Desc)[] PriSections = {
        ("ต้องทำก่อน — สำคัญสูง", "สูง", "#e5484d", "งานเร่งด่วน ต้องจัดการทันที"),
        ("ควรทำต่อไป — ปานกลาง", "ปานกลาง", "#f5a623", "งานสำคัญ ควรวางแผนทำให้เสร็จ"),
        ("ทำเมื่อว่าง — ปกติ", "ปกติ", "#6b7280", "งานทั่วไป ไม่เร่ง"),
        ("ทำทีหลังได้ — ต่ำ", "ต่ำ", "#30a46c", "งานไม่ด่วน ทำเมื่อมีเวลา"),
    };

    public SummaryWindow(List<Note> notes)
    {
        InitializeComponent();
        _notes = notes;

        var total = notes.Count;
        var pinned = notes.Count(n => n.Pinned);
        var withReminder = notes.Count(n => !string.IsNullOrEmpty(n.Reminder) && !n.Notified);
        lblSubtitle.Text = $"ทั้งหมด {total} โน้ต  ·  📌 {pinned} ปักหมุด  ·  ⏰ {withReminder} รอแจ้งเตือน  ·  {DateTime.Now:dd/MM/yyyy HH:mm}";

        BuildAITab();
        BuildPriorityTab();
        BuildCategoryTabs();
    }

    // ====================================================
    // แท็บ 1: สรุปภาพรวม AI
    // ====================================================
    private void BuildAITab()
    {
        var now = DateTime.Now;
        _exportText.AppendLine("══════════════════════════════════════════");
        _exportText.AppendLine("  🤖 สรุปภาพรวม AI — วิเคราะห์โน้ตทั้งหมด");
        _exportText.AppendLine($"  วันที่: {now:dd/MM/yyyy HH:mm}");
        _exportText.AppendLine("══════════════════════════════════════════\n");

        AddAICard("สถิติภาพรวม", "#6c63ff", BuildOverviewStats());
        AddAICard("วิเคราะห์ความเร่งด่วน", "#e5484d", BuildUrgencyAnalysis());
        AddAICard("ภาระงานแต่ละหมวดหมู่", "#4f6df5", BuildWorkloadAnalysis());
        AddAICard("คำแนะนำ — สิ่งที่ควรทำ", "#30a46c", BuildRecommendations());
        AddAICard("สรุปโดยรวม", "#f5a623", BuildOneLiner());
    }

    private string BuildOverviewStats()
    {
        var total = _notes.Count;
        if (total == 0) return "ยังไม่มีโน้ต";

        var high = _notes.Count(n => n.Priority == "สูง");
        var med = _notes.Count(n => n.Priority == "ปานกลาง");
        var normal = _notes.Count(n => (n.Priority ?? "ปกติ") == "ปกติ");
        var low = _notes.Count(n => n.Priority == "ต่ำ");
        var pinned = _notes.Count(n => n.Pinned);
        var cats = _notes.Select(n => n.Category ?? "ทั่วไป").Distinct().Count();
        var withReminder = _notes.Count(n => !string.IsNullOrEmpty(n.Reminder));

        return $"📋 โน้ตทั้งหมด: {total} รายการ\n" +
               $"📂 จำนวนหมวดหมู่: {cats} หมวด\n" +
               $"📌 ปักหมุด: {pinned} รายการ\n" +
               $"⏰ มีแจ้งเตือน: {withReminder} รายการ\n\n" +
               $"🔴 สูง: {high}  ·  🟡 ปานกลาง: {med}  ·  ⚪ ปกติ: {normal}  ·  🟢 ต่ำ: {low}";
    }

    private string BuildUrgencyAnalysis()
    {
        var now = DateTime.Now;
        var lines = new List<string>();

        // Overdue reminders
        var overdue = _notes.Where(n =>
            !string.IsNullOrEmpty(n.Reminder) && DateTime.TryParse(n.Reminder, out var dt) && dt < now
        ).ToList();
        if (overdue.Any())
        {
            lines.Add($"⚠️ เลยกำหนดแล้ว {overdue.Count} รายการ:");
            foreach (var n in overdue)
                lines.Add($"   • {n.PriorityIcon} {n.Title} (กำหนด: {n.Reminder})");
            lines.Add("");
        }

        // Due today
        var today = _notes.Where(n =>
            !string.IsNullOrEmpty(n.Reminder) && DateTime.TryParse(n.Reminder, out var dt)
            && dt.Date == now.Date && dt >= now
        ).ToList();
        if (today.Any())
        {
            lines.Add($"🔔 ต้องทำวันนี้ {today.Count} รายการ:");
            foreach (var n in today.OrderBy(n => n.Reminder))
                lines.Add($"   • {n.PriorityIcon} {n.Title} (เวลา: {n.Reminder.Split(' ').LastOrDefault()})");
            lines.Add("");
        }

        // Due this week
        var thisWeek = _notes.Where(n =>
            !string.IsNullOrEmpty(n.Reminder) && DateTime.TryParse(n.Reminder, out var dt)
            && dt.Date > now.Date && dt.Date <= now.Date.AddDays(7)
        ).ToList();
        if (thisWeek.Any())
        {
            lines.Add($"📅 ภายใน 7 วัน {thisWeek.Count} รายการ:");
            foreach (var n in thisWeek.OrderBy(n => n.Reminder))
                lines.Add($"   • {n.PriorityIcon} {n.Title} ({n.Reminder})");
            lines.Add("");
        }

        // High priority without reminder
        var highNoReminder = _notes.Where(n => n.Priority == "สูง" && string.IsNullOrEmpty(n.Reminder)).ToList();
        if (highNoReminder.Any())
        {
            lines.Add($"⚡ ความสำคัญสูงแต่ไม่มีกำหนดเวลา {highNoReminder.Count} รายการ:");
            foreach (var n in highNoReminder)
                lines.Add($"   • {n.Title}");
            lines.Add("   → แนะนำ: ควรตั้งกำหนดเวลาเพื่อไม่ให้ลืม");
        }

        if (!lines.Any()) lines.Add("✅ ไม่มีงานเร่งด่วนหรือเลยกำหนด — สถานการณ์ดี!");
        return string.Join("\n", lines);
    }

    private string BuildWorkloadAnalysis()
    {
        var cats = _notes.GroupBy(n => n.Category ?? "ทั่วไป").OrderByDescending(g => g.Count()).ToList();
        if (!cats.Any()) return "ยังไม่มีโน้ต";

        var lines = new List<string>();
        var maxCount = cats.First().Count();

        foreach (var g in cats)
        {
            var count = g.Count();
            var high = g.Count(n => n.Priority == "สูง");
            var barLen = maxCount > 0 ? (int)Math.Ceiling((double)count / maxCount * 15) : 0;
            var bar = new string('█', barLen) + new string('░', 15 - barLen);

            var label = $"📂 {g.Key}: {count} รายการ";
            if (high > 0) label += $" (🔴 {high} เร่งด่วน)";
            lines.Add(label);
            lines.Add($"   {bar}  {count}");

            // mini summary per category
            var pinnedCount = g.Count(n => n.Pinned);
            var reminderCount = g.Count(n => !string.IsNullOrEmpty(n.Reminder));
            var details = new List<string>();
            if (pinnedCount > 0) details.Add($"📌 {pinnedCount}");
            if (reminderCount > 0) details.Add($"⏰ {reminderCount}");
            if (details.Any()) lines.Add($"   {string.Join("  ", details)}");
            lines.Add("");
        }

        // Highlight busiest
        var busiest = cats.First();
        if (busiest.Count() > 1)
            lines.Add($"📊 หมวด \"{busiest.Key}\" มีงานมากที่สุด ({busiest.Count()} รายการ)");

        return string.Join("\n", lines);
    }

    private string BuildRecommendations()
    {
        var now = DateTime.Now;
        var recs = new List<string>();
        int num = 1;

        // 1. Overdue
        var overdue = _notes.Where(n =>
            !string.IsNullOrEmpty(n.Reminder) && DateTime.TryParse(n.Reminder, out var dt) && dt < now).ToList();
        if (overdue.Any())
            recs.Add($"{num++}. 🚨 จัดการงานที่เลยกำหนด {overdue.Count} รายการก่อน เพราะอาจส่งผลกระทบ");

        // 2. High priority pinned
        var highPinned = _notes.Where(n => n.Pinned && n.Priority == "สูง").ToList();
        if (highPinned.Any())
            recs.Add($"{num++}. 📌 ทำงานปักหมุดความสำคัญสูง {highPinned.Count} รายการ: {string.Join(", ", highPinned.Select(n => $"\"{n.Title}\""))}");

        // 3. High priority
        var highNotPinned = _notes.Where(n => !n.Pinned && n.Priority == "สูง").ToList();
        if (highNotPinned.Any())
            recs.Add($"{num++}. 🔴 ต่อด้วยงานความสำคัญสูง {highNotPinned.Count} รายการ");

        // 4. Today reminders
        var todayItems = _notes.Where(n =>
            !string.IsNullOrEmpty(n.Reminder) && DateTime.TryParse(n.Reminder, out var dt)
            && dt.Date == now.Date && dt >= now).ToList();
        if (todayItems.Any())
            recs.Add($"{num++}. 🔔 อย่าลืมงานที่กำหนดวันนี้ {todayItems.Count} รายการ");

        // 5. Medium priority
        var medCount = _notes.Count(n => n.Priority == "ปานกลาง");
        if (medCount > 0)
            recs.Add($"{num++}. 🟡 วางแผนทำงานปานกลาง {medCount} รายการในช่วงนี้");

        // 6. Unorganized
        var noCat = _notes.Count(n => string.IsNullOrEmpty(n.Category) || n.Category == "ทั่วไป");
        if (noCat > 3)
            recs.Add($"{num++}. 📂 มีโน้ตหมวด \"ทั่วไป\" {noCat} รายการ — ลองจัดหมวดหมู่ให้ชัดเจนขึ้น");

        // 7. Low priority
        var lowCount = _notes.Count(n => n.Priority == "ต่ำ");
        if (lowCount > 0)
            recs.Add($"{num++}. 🟢 งานความสำคัญต่ำ {lowCount} รายการ ค่อยทำเมื่อว่าง ไม่ต้องรีบ");

        if (!recs.Any())
            recs.Add("✅ ทุกอย่างเรียบร้อยดี! ไม่มีคำแนะนำเพิ่มเติม");

        return string.Join("\n\n", recs);
    }

    private string BuildOneLiner()
    {
        var now = DateTime.Now;
        var total = _notes.Count;
        if (total == 0) return "📝 ยังไม่มีโน้ต — เริ่มจดสิ่งที่ต้องทำเลย!";

        var high = _notes.Count(n => n.Priority == "สูง");
        var overdue = _notes.Count(n =>
            !string.IsNullOrEmpty(n.Reminder) && DateTime.TryParse(n.Reminder, out var dt) && dt < now);
        var todayCount = _notes.Count(n =>
            !string.IsNullOrEmpty(n.Reminder) && DateTime.TryParse(n.Reminder, out var dt)
            && dt.Date == now.Date && dt >= now);

        if (overdue > 0)
            return $"⚠️ มีงานเลยกำหนด {overdue} รายการ — ควรจัดการโดยเร็วที่สุดก่อนจะสะสมมากขึ้น";
        if (high > 3)
            return $"🔴 มีงานเร่งด่วน {high} รายการ — ค่อนข้างหนัก ลองจัดลำดับและแบ่งทำทีละขั้น";
        if (high > 0 && todayCount > 0)
            return $"🔔 วันนี้มีงาน {todayCount} รายการ + งานสำคัญสูง {high} รายการ — โฟกัสงานด่วนก่อน";
        if (high > 0)
            return $"🔴 มีงานความสำคัญสูง {high} รายการ — ควรจัดการให้เสร็จก่อนงานอื่น";
        if (total <= 3)
            return $"✨ มีโน้ตแค่ {total} รายการ — สถานการณ์เบาสบาย จัดการได้ไม่ยาก!";
        return $"📋 มีโน้ต {total} รายการ — ภาพรวมโอเค ค่อยๆ ทำตามลำดับความสำคัญ";
    }

    private void AddAICard(string title, string color, string content)
    {
        var card = new Border
        {
            Background = Brush("#161828"), CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16, 14, 16, 14), Margin = new Thickness(0, 0, 0, 10),
            BorderBrush = Brush(color), BorderThickness = new Thickness(0, 0, 0, 3)
        };
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock { Text = title, FontSize = 15, FontWeight = FontWeights.Bold,
            Foreground = Brush(color), Margin = new Thickness(0, 0, 0, 8) });
        sp.Children.Add(new TextBlock { Text = content, FontSize = 12.5, Foreground = Brush("#b0b3c6"),
            TextWrapping = TextWrapping.Wrap, LineHeight = 21 });
        card.Child = sp;
        panelAI.Children.Add(card);

        _exportText.AppendLine($"[{title}]");
        _exportText.AppendLine(content);
        _exportText.AppendLine();
    }

    // ====================================================
    // แท็บ 2: ตามลำดับความสำคัญ (เหมือนเดิม)
    // ====================================================
    private void BuildPriorityTab()
    {
        _exportText.AppendLine("\n══════════════════════════════════════════");
        _exportText.AppendLine("  🔥 ตามลำดับความสำคัญ");
        _exportText.AppendLine("══════════════════════════════════════════\n");

        var pinnedNotes = _notes.Where(n => n.Pinned).OrderBy(n => n.PriorityRank).ToList();
        if (pinnedNotes.Any())
        {
            AddSectionHeader(panelPriority, "📌 ปักหมุด — ต้องดูก่อนเลย", "#ff6b6b",
                $"{pinnedNotes.Count} รายการ", pinnedNotes.Count);
            _exportText.AppendLine($"📌 ปักหมุด ({pinnedNotes.Count} รายการ)");
            int num = 1;
            foreach (var n in pinnedNotes) { AddNoteItem(panelPriority, n, num); AppendNoteText(n, num++); }
            _exportText.AppendLine();
        }

        var unpinned = _notes.Where(n => !n.Pinned).ToList();
        foreach (var (label, priority, color, desc) in PriSections)
        {
            var group = unpinned.Where(n => (n.Priority ?? "ปกติ") == priority)
                .OrderBy(n => string.IsNullOrEmpty(n.Reminder) ? DateTime.MaxValue :
                    DateTime.TryParse(n.Reminder, out var dt) ? dt : DateTime.MaxValue).ToList();
            if (!group.Any()) continue;

            AddSectionHeader(panelPriority, label, color, desc, group.Count);
            _exportText.AppendLine($"{label} ({group.Count} รายการ)");
            int num = 1;
            foreach (var n in group) { AddNoteItem(panelPriority, n, num); AppendNoteText(n, num++); }
            _exportText.AppendLine();
        }
    }

    // ====================================================
    // แท็บ 3+: แยกตามหมวดหมู่
    // ====================================================
    private void BuildCategoryTabs()
    {
        var cats = _notes.GroupBy(n => n.Category ?? "ทั่วไป")
            .OrderByDescending(g => g.Count()).ToList();

        _exportText.AppendLine("\n══════════════════════════════════════════");
        _exportText.AppendLine("  📂 สรุปแยกตามหมวดหมู่");
        _exportText.AppendLine("══════════════════════════════════════════\n");

        foreach (var catGroup in cats)
        {
            var catName = catGroup.Key;
            var items = catGroup.OrderBy(n => n.PriorityRank).ToList();

            var tab = new TabItem { Header = $"{catName} ({items.Count})", FontSize = 13, FontWeight = FontWeights.SemiBold };
            var sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Background = Brush("#0f1018") };
            var panel = new StackPanel { Margin = new Thickness(12) };

            // Category stats card
            var high = items.Count(n => n.Priority == "สูง");
            var med = items.Count(n => n.Priority == "ปานกลาง");
            var pinCount = items.Count(n => n.Pinned);
            var remCount = items.Count(n => !string.IsNullOrEmpty(n.Reminder));

            var statsCard = new Border
            {
                Background = Brush("#161828"), CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14), Margin = new Thickness(0, 0, 0, 10),
                BorderBrush = Brush("#3498db"), BorderThickness = new Thickness(0, 0, 0, 3)
            };
            var statsSp = new StackPanel();
            statsSp.Children.Add(new TextBlock
            {
                Text = $"สรุปหมวด \"{catName}\"", FontSize = 15, FontWeight = FontWeights.Bold,
                Foreground = Brush("#4f6df5"), Margin = new Thickness(0, 0, 0, 6)
            });
            var statsText = $"รวม: {items.Count} รายการ\n" +
                            $"สูง: {high}  ·  ปานกลาง: {med}  ·  ปักหมุด: {pinCount}  ·  แจ้งเตือน: {remCount}";
            if (high > 0) statsText += $"\n\nหมวดนี้มีงานเร่งด่วน {high} รายการ ควรจัดการก่อน";
            else statsText += "\n\nไม่มีงานเร่งด่วนในหมวดนี้";

            statsSp.Children.Add(new TextBlock { Text = statsText, FontSize = 12, Foreground = Brush("#b0b3c6"),
                TextWrapping = TextWrapping.Wrap, LineHeight = 21 });
            statsCard.Child = statsSp;
            panel.Children.Add(statsCard);

            _exportText.AppendLine($"📂 หมวด \"{catName}\" — {items.Count} รายการ (🔴{high} 📌{pinCount} ⏰{remCount})");
            _exportText.AppendLine(new string('─', 40));

            // Notes list
            int num = 1;
            foreach (var n in items)
            {
                AddNoteItem(panel, n, num);
                AppendNoteText(n, num++);
            }
            _exportText.AppendLine();

            sv.Content = panel;
            tab.Content = sv;
            tabMain.Items.Add(tab);
        }
    }

    // ====================================================
    // Shared UI helpers
    // ====================================================
    private void AddSectionHeader(StackPanel target, string title, string color, string desc, int count)
    {
        var header = new Border
        {
            Background = Brush("#2a2d45"), CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 10, 0, 6)
        };
        var sp = new StackPanel();
        var topRow = new DockPanel();
        topRow.Children.Add(new TextBlock { Text = title, FontSize = 14, FontWeight = FontWeights.Bold, Foreground = Brush(color) });
        var badge = new Border { Background = Brush("#2a2d45"), CornerRadius = new CornerRadius(10),
            Padding = new Thickness(8, 2, 8, 2), HorizontalAlignment = HorizontalAlignment.Right };
        badge.Child = new TextBlock { Text = count.ToString(), FontSize = 12, Foreground = Brush("#e8e9f0") };
        DockPanel.SetDock(badge, Dock.Right);
        topRow.Children.Insert(0, badge);
        sp.Children.Add(topRow);
        sp.Children.Add(new TextBlock { Text = desc, FontSize = 11, Foreground = Brush("#8b8fa7"), Margin = new Thickness(0, 3, 0, 0) });
        header.Child = sp;
        target.Children.Add(header);
    }

    private void AddNoteItem(StackPanel target, Note note, int num)
    {
        var card = new Border
        {
            Background = Brush("#161828"), CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 0, 0, 4)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var numBorder = new Border { Background = Brush("#2a2d45"), CornerRadius = new CornerRadius(12),
            Width = 24, Height = 24, VerticalAlignment = VerticalAlignment.Top };
        numBorder.Child = new TextBlock { Text = num.ToString(), FontSize = 11, Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(numBorder, 0);
        grid.Children.Add(numBorder);

        var sp = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
        if (note.Pinned) titleRow.Children.Add(Txt("📌 ", 13));
        titleRow.Children.Add(Txt(note.PriorityIcon + " ", 13));
        titleRow.Children.Add(new TextBlock { Text = note.Title, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = Brush("#e8e9f0") });
        if (!string.IsNullOrEmpty(note.Reminder)) titleRow.Children.Add(Txt(" ⏰", 13));
        sp.Children.Add(titleRow);

        var metaRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 0) };
        metaRow.Children.Add(SmallBadge(note.Category ?? "ทั่วไป"));
        metaRow.Children.Add(SmallBadge(note.Priority ?? "ปกติ"));
        if (!string.IsNullOrEmpty(note.Reminder) && DateTime.TryParse(note.Reminder, out var rdt))
            metaRow.Children.Add(new TextBlock { Text = $"⏰ {rdt:dd/MM HH:mm}", FontSize = 11,
                Foreground = Brush("#f39c12"), Margin = new Thickness(6, 0, 0, 0) });
        sp.Children.Add(metaRow);

        if (!string.IsNullOrEmpty(note.Content))
        {
            var preview = note.Content.Length > 60 ? note.Content[..60] + "..." : note.Content;
            sp.Children.Add(new TextBlock { Text = preview, FontSize = 11, Foreground = Brush("#5a5d78"),
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 0) });
        }

        Grid.SetColumn(sp, 1);
        grid.Children.Add(sp);
        grid.Children.Add(new TextBlock { Text = note.Created, FontSize = 10, Foreground = Brush("#5a5d78"),
            VerticalAlignment = VerticalAlignment.Top });
        Grid.SetColumn(grid.Children[^1], 2);

        card.Child = grid;
        target.Children.Add(card);
    }

    private void AppendNoteText(Note n, int num)
    {
        _exportText.Append($"  {num}. {n.PriorityIcon} {n.Title}");
        if (n.Pinned) _exportText.Append(" 📌");
        if (!string.IsNullOrEmpty(n.Reminder)) _exportText.Append($" ⏰ {n.Reminder}");
        _exportText.AppendLine();
        _exportText.AppendLine($"     หมวด: {n.Category ?? "ทั่วไป"}  |  สร้าง: {n.Created}");
        if (!string.IsNullOrEmpty(n.Content))
            _exportText.AppendLine($"     {(n.Content.Length > 80 ? n.Content[..80] + "..." : n.Content)}");
    }

    // ====================================================
    // Utilities
    // ====================================================
    private static SolidColorBrush Brush(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex));

    private static TextBlock Txt(string text, double size) =>
        new() { Text = text, FontSize = size };

    private static Border SmallBadge(string text) => new()
    {
        Background = Brush("#2a2d45"), CornerRadius = new CornerRadius(5),
        Padding = new Thickness(6, 1, 6, 1), Margin = new Thickness(0, 0, 6, 0),
        Child = new TextBlock { Text = text, FontSize = 10, Foreground = Brush("#8b8fa7") }
    };

    private void ExportSummary_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog { Filter = "Text|*.txt", FileName = $"summary_{DateTime.Now:yyyyMMdd}.txt" };
        if (dlg.ShowDialog() == true)
        {
            File.WriteAllText(dlg.FileName, _exportText.ToString());
            MessageBox.Show($"Export สรุปไปที่\n{dlg.FileName}", "สำเร็จ");
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
