using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NoteApp;

public partial class DashboardWindow : Window
{
    private readonly List<Note> _notes;
    private readonly AppTheme _theme;

    private static readonly string[] ChartColors =
        { "#6c63ff", "#e84393", "#00b894", "#fdcb6e", "#e17055", "#0984e3", "#00cec9", "#d63031", "#a29bfe", "#fab1a0" };

    public DashboardWindow(List<Note> notes, AppTheme theme)
    {
        InitializeComponent();
        _notes = notes;
        _theme = theme;
        Background = B(theme.Bg);
        BuildDashboard();
    }

    private void BuildDashboard()
    {
        panelMain.Children.Add(new TextBlock
        {
            Text = "สถิติโน้ต",
            FontSize = 20, FontWeight = FontWeights.Bold,
            Foreground = B(_theme.Text), Margin = new Thickness(0, 0, 0, 4)
        });
        panelMain.Children.Add(new TextBlock
        {
            Text = $"อัพเดท: {DateTime.Now:dd/MM/yyyy HH:mm}  ·  ทั้งหมด {_notes.Count} โน้ต",
            FontSize = 12, Foreground = B(_theme.Text2), Margin = new Thickness(0, 0, 0, 16)
        });

        // KPI Cards
        BuildKPICards();

        // Charts row
        var chartsRow = new Grid { Margin = new Thickness(0, 16, 0, 0) };
        chartsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        chartsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        chartsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var pieCard = BuildCard("สัดส่วนตามหมวดหมู่");
        BuildPieChart(pieCard);
        Grid.SetColumn(pieCard, 0);
        chartsRow.Children.Add(pieCard);

        var priCard = BuildCard("จำนวนตามความสำคัญ");
        BuildPriorityBarChart(priCard);
        Grid.SetColumn(priCard, 2);
        chartsRow.Children.Add(priCard);

        panelMain.Children.Add(chartsRow);

        var actCard = BuildCard("โน้ตที่สร้างใน 7 วันล่าสุด");
        BuildActivityChart(actCard);
        actCard.Margin = new Thickness(0, 12, 0, 0);
        panelMain.Children.Add(actCard);

        var catBarCard = BuildCard("จำนวนโน้ตแต่ละหมวดหมู่");
        BuildCategoryBarChart(catBarCard);
        catBarCard.Margin = new Thickness(0, 12, 0, 0);
        panelMain.Children.Add(catBarCard);

        var remCard = BuildCard("สถานะแจ้งเตือน");
        BuildReminderStats(remCard);
        remCard.Margin = new Thickness(0, 12, 0, 0);
        panelMain.Children.Add(remCard);
    }

    // ====================================================
    // KPI Cards
    // ====================================================
    private void BuildKPICards()
    {
        var grid = new Grid();
        for (int i = 0; i < 5; i++)
        {
            if (i > 0) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        var now = DateTime.Now;
        var total = _notes.Count;
        var pinned = _notes.Count(n => n.Pinned);
        var high = _notes.Count(n => n.Priority == "สูง");
        var withReminder = _notes.Count(n => !string.IsNullOrEmpty(n.Reminder) && !n.Notified);
        var locked = _notes.Count(n => n.Locked);

        AddKPI(grid, 0, "📋", total.ToString(), "โน้ตทั้งหมด", "#6c63ff");
        AddKPI(grid, 2, "📌", pinned.ToString(), "ปักหมุด", "#e67e22");
        AddKPI(grid, 4, "🔴", high.ToString(), "ความสำคัญสูง", "#e74c3c");
        AddKPI(grid, 6, "⏰", withReminder.ToString(), "รอแจ้งเตือน", "#0984e3");
        AddKPI(grid, 8, "🔒", locked.ToString(), "ล็อค", "#636e72");

        panelMain.Children.Add(grid);
    }

    private void AddKPI(Grid grid, int col, string icon, string value, string label, string color)
    {
        var card = new Border
        {
            Background = B(_theme.Surface), CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14, 12, 14, 12),
            BorderBrush = B(color), BorderThickness = new Thickness(0, 0, 0, 3)
        };
        var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        sp.Children.Add(new TextBlock { Text = icon, FontSize = 22, HorizontalAlignment = HorizontalAlignment.Center });
        sp.Children.Add(new TextBlock
        {
            Text = value, FontSize = 28, FontWeight = FontWeights.Bold,
            Foreground = B(color), HorizontalAlignment = HorizontalAlignment.Center
        });
        sp.Children.Add(new TextBlock
        {
            Text = label, FontSize = 11, Foreground = B(_theme.Text2),
            HorizontalAlignment = HorizontalAlignment.Center
        });
        card.Child = sp;
        Grid.SetColumn(card, col);
        grid.Children.Add(card);
    }

    // ====================================================
    // Pie Chart — หมวดหมู่
    // ====================================================
    private void BuildPieChart(Border card)
    {
        var sp = (StackPanel)card.Child;
        if (_notes.Count == 0)
        {
            sp.Children.Add(new TextBlock { Text = "ไม่มีข้อมูล", Foreground = B(_theme.Text2), FontSize = 13, Margin = new Thickness(0, 20, 0, 0) });
            return;
        }

        var groups = _notes.GroupBy(n => n.Category ?? "ทั่วไป")
            .OrderByDescending(g => g.Count()).ToList();

        var canvas = new Canvas { Width = 160, Height = 160, Margin = new Thickness(0, 8, 0, 8), HorizontalAlignment = HorizontalAlignment.Center };
        double cx = 80, cy = 80, r = 75;
        double startAngle = 0;

        for (int i = 0; i < groups.Count; i++)
        {
            var fraction = (double)groups[i].Count() / _notes.Count;
            var sweep = fraction * 360;
            var color = ChartColors[i % ChartColors.Length];

            var path = CreatePieSlice(cx, cy, r, startAngle, startAngle + sweep, color);
            canvas.Children.Add(path);
            startAngle += sweep;
        }

        // Center circle (donut)
        canvas.Children.Add(new Ellipse
        {
            Width = 60, Height = 60, Fill = B(_theme.Surface),
            Margin = new Thickness(cx - 30, cy - 30, 0, 0)
        });
        var centerText = new TextBlock
        {
            Text = _notes.Count.ToString(), FontSize = 18, FontWeight = FontWeights.Bold,
            Foreground = B(_theme.Text), HorizontalAlignment = HorizontalAlignment.Center
        };
        Canvas.SetLeft(centerText, cx - 10);
        Canvas.SetTop(centerText, cy - 12);
        canvas.Children.Add(centerText);

        sp.Children.Add(canvas);

        // Legend
        for (int i = 0; i < groups.Count; i++)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };
            row.Children.Add(new Ellipse
            {
                Width = 10, Height = 10, Fill = B(ChartColors[i % ChartColors.Length]),
                Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center
            });
            var pct = (double)groups[i].Count() / _notes.Count * 100;
            row.Children.Add(new TextBlock
            {
                Text = $"{groups[i].Key}  ({groups[i].Count()}, {pct:F0}%)",
                FontSize = 11, Foreground = B(_theme.Text)
            });
            sp.Children.Add(row);
        }
    }

    private Path CreatePieSlice(double cx, double cy, double r, double startDeg, double endDeg, string color)
    {
        var startRad = startDeg * Math.PI / 180;
        var endRad = endDeg * Math.PI / 180;
        var x1 = cx + r * Math.Cos(startRad);
        var y1 = cy + r * Math.Sin(startRad);
        var x2 = cx + r * Math.Cos(endRad);
        var y2 = cy + r * Math.Sin(endRad);
        var large = (endDeg - startDeg) > 180 ? 1 : 0;

        var fig = new PathFigure { StartPoint = new Point(cx, cy), IsClosed = true };
        fig.Segments.Add(new LineSegment(new Point(x1, y1), true));
        fig.Segments.Add(new ArcSegment(new Point(x2, y2), new Size(r, r), 0, large == 1, SweepDirection.Clockwise, true));

        var geo = new PathGeometry();
        geo.Figures.Add(fig);
        return new Path { Data = geo, Fill = B(color) };
    }

    // ====================================================
    // Bar Chart — ความสำคัญ
    // ====================================================
    private void BuildPriorityBarChart(Border card)
    {
        var sp = (StackPanel)card.Child;
        var priorities = new[] {
            ("🔴 สูง", "สูง", "#e74c3c"),
            ("🟡 ปานกลาง", "ปานกลาง", "#f1c40f"),
            ("⚪ ปกติ", "ปกติ", "#6c757d"),
            ("🟢 ต่ำ", "ต่ำ", "#2ecc71"),
        };

        var max = _notes.Count > 0 ? priorities.Max(p => _notes.Count(n => (n.Priority ?? "ปกติ") == p.Item2)) : 1;
        if (max == 0) max = 1;

        foreach (var (label, pri, color) in priorities)
        {
            var count = _notes.Count(n => (n.Priority ?? "ปกติ") == pri);
            var pct = (double)count / max;

            var row = new Grid { Margin = new Thickness(0, 6, 0, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35) });

            var lbl = new TextBlock { Text = label, FontSize = 12, Foreground = B(_theme.Text), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(lbl, 0);
            row.Children.Add(lbl);

            var barBg = new Border
            {
                Background = B(_theme.Surface2), CornerRadius = new CornerRadius(4),
                Height = 20, HorizontalAlignment = HorizontalAlignment.Stretch
            };
            var barFill = new Border
            {
                Background = B(color), CornerRadius = new CornerRadius(4),
                Height = 20, HorizontalAlignment = HorizontalAlignment.Left,
                Width = Math.Max(pct * 200, count > 0 ? 8 : 0)
            };
            var barGrid = new Grid();
            barGrid.Children.Add(barBg);
            barGrid.Children.Add(barFill);
            Grid.SetColumn(barGrid, 1);
            row.Children.Add(barGrid);

            var countTb = new TextBlock
            {
                Text = count.ToString(), FontSize = 13, FontWeight = FontWeights.Bold,
                Foreground = B(color), HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(countTb, 2);
            row.Children.Add(countTb);

            sp.Children.Add(row);
        }
    }

    // ====================================================
    // Activity Chart — 7 วันล่าสุด
    // ====================================================
    private void BuildActivityChart(Border card)
    {
        var sp = (StackPanel)card.Child;
        var today = DateTime.Today;
        var days = Enumerable.Range(0, 7).Select(i => today.AddDays(-6 + i)).ToList();

        var counts = days.Select(d =>
            _notes.Count(n => DateTime.TryParse(n.Created, out var dt) && dt.Date == d)).ToList();
        var max = counts.Max();
        if (max == 0) max = 1;

        var grid = new Grid { Height = 140, Margin = new Thickness(0, 8, 0, 0) };
        for (int i = 0; i < 7; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        for (int i = 0; i < 7; i++)
        {
            var colSp = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Center };

            // Count label
            colSp.Children.Add(new TextBlock
            {
                Text = counts[i].ToString(), FontSize = 12, FontWeight = FontWeights.Bold,
                Foreground = B(counts[i] > 0 ? "#6c63ff" : _theme.Text2),
                HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 4)
            });

            // Bar
            var barHeight = Math.Max((double)counts[i] / max * 80, counts[i] > 0 ? 6 : 2);
            colSp.Children.Add(new Border
            {
                Width = 28, Height = barHeight,
                Background = B(counts[i] > 0 ? "#6c63ff" : _theme.Surface2),
                CornerRadius = new CornerRadius(4, 4, 0, 0)
            });

            // Day label
            var isToday = days[i] == today;
            colSp.Children.Add(new TextBlock
            {
                Text = days[i].ToString("dd/MM", CultureInfo.InvariantCulture),
                FontSize = 10, HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = B(isToday ? "#6c63ff" : _theme.Text2),
                FontWeight = isToday ? FontWeights.Bold : FontWeights.Normal,
                Margin = new Thickness(0, 4, 0, 0)
            });
            var dayName = isToday ? "วันนี้" : days[i].ToString("ddd", new CultureInfo("th-TH"));
            colSp.Children.Add(new TextBlock
            {
                Text = dayName, FontSize = 9, HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = B(isToday ? "#6c63ff" : _theme.Text2)
            });

            Grid.SetColumn(colSp, i);
            grid.Children.Add(colSp);
        }

        sp.Children.Add(grid);

        var total7 = counts.Sum();
        var avg = total7 / 7.0;
        sp.Children.Add(new TextBlock
        {
            Text = $"สร้างใน 7 วัน: {total7} โน้ต  ·  เฉลี่ย {avg:F1} ต่อวัน",
            FontSize = 11, Foreground = B(_theme.Text2), Margin = new Thickness(0, 8, 0, 0)
        });
    }

    // ====================================================
    // Horizontal Bar — หมวดหมู่
    // ====================================================
    private void BuildCategoryBarChart(Border card)
    {
        var sp = (StackPanel)card.Child;
        var groups = _notes.GroupBy(n => n.Category ?? "ทั่วไป")
            .OrderByDescending(g => g.Count()).ToList();

        if (!groups.Any())
        {
            sp.Children.Add(new TextBlock { Text = "ไม่มีข้อมูล", Foreground = B(_theme.Text2), FontSize = 13 });
            return;
        }

        var max = groups.First().Count();
        if (max == 0) max = 1;

        for (int i = 0; i < groups.Count; i++)
        {
            var g = groups[i];
            var color = ChartColors[i % ChartColors.Length];
            var pct = (double)g.Count() / max;

            var row = new Grid { Margin = new Thickness(0, 5, 0, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35) });

            row.Children.Add(new TextBlock
            {
                Text = g.Key, FontSize = 12, Foreground = B(_theme.Text),
                VerticalAlignment = VerticalAlignment.Center
            });

            var barGrid = new Grid();
            barGrid.Children.Add(new Border
            {
                Background = B(_theme.Surface2), CornerRadius = new CornerRadius(4), Height = 22
            });
            barGrid.Children.Add(new Border
            {
                Background = B(color), CornerRadius = new CornerRadius(4), Height = 22,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = Math.Max(pct * 300, 8)
            });
            Grid.SetColumn(barGrid, 1);
            row.Children.Add(barGrid);

            var countTb = new TextBlock
            {
                Text = g.Count().ToString(), FontSize = 13, FontWeight = FontWeights.Bold,
                Foreground = B(color), HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(countTb, 2);
            row.Children.Add(countTb);

            sp.Children.Add(row);
        }
    }

    // ====================================================
    // Reminder Stats
    // ====================================================
    private void BuildReminderStats(Border card)
    {
        var sp = (StackPanel)card.Child;
        var now = DateTime.Now;

        var withReminder = _notes.Where(n => !string.IsNullOrEmpty(n.Reminder)).ToList();
        var pending = withReminder.Where(n => !n.Notified && DateTime.TryParse(n.Reminder, out var dt) && dt >= now).ToList();
        var overdue = withReminder.Where(n => !n.Notified && DateTime.TryParse(n.Reminder, out var dt) && dt < now).ToList();
        var done = withReminder.Where(n => n.Notified).ToList();
        var noReminder = _notes.Count - withReminder.Count;

        var items = new[] {
            ("⏳ รอแจ้งเตือน", pending.Count, "#0984e3"),
            ("⚠️ เลยกำหนด", overdue.Count, "#e74c3c"),
            ("✅ แจ้งแล้ว", done.Count, "#27ae60"),
            ("📝 ไม่มีแจ้งเตือน", noReminder, "#636e72"),
        };

        var grid = new Grid { Margin = new Thickness(0, 4, 0, 0) };
        for (int i = 0; i < 4; i++)
        {
            if (i > 0) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        for (int i = 0; i < items.Length; i++)
        {
            var (label, count, color) = items[i];
            var mini = new Border
            {
                Background = B(_theme.Surface2), CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 8, 10, 8)
            };
            var miniSp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            miniSp.Children.Add(new TextBlock
            {
                Text = count.ToString(), FontSize = 20, FontWeight = FontWeights.Bold,
                Foreground = B(color), HorizontalAlignment = HorizontalAlignment.Center
            });
            miniSp.Children.Add(new TextBlock
            {
                Text = label, FontSize = 10, Foreground = B(_theme.Text2),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            mini.Child = miniSp;
            Grid.SetColumn(mini, i * 2);
            grid.Children.Add(mini);
        }

        sp.Children.Add(grid);

        // Next upcoming reminder
        var next = pending.Where(n => DateTime.TryParse(n.Reminder, out _))
            .OrderBy(n => DateTime.Parse(n.Reminder)).FirstOrDefault();
        if (next != null)
        {
            sp.Children.Add(new TextBlock
            {
                Text = $"⏰ แจ้งเตือนถัดไป: {next.Title} ({next.Reminder})",
                FontSize = 11, Foreground = B("#0984e3"), Margin = new Thickness(0, 10, 0, 0)
            });
        }
    }

    // ====================================================
    // Helpers
    // ====================================================
    private Border BuildCard(string title)
    {
        var card = new Border
        {
            Background = B(_theme.Surface), CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16, 14, 16, 14)
        };
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = title, FontSize = 14, FontWeight = FontWeights.Bold,
            Foreground = B(_theme.Text), Margin = new Thickness(0, 0, 0, 4)
        });
        card.Child = sp;
        return card;
    }

    private static SolidColorBrush B(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex));
}
