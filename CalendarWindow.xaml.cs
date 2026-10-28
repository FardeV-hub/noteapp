using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NoteApp;

public partial class CalendarWindow : Window
{
    private readonly List<Note> _notes;
    private readonly List<Booking> _bookings;
    private readonly List<Bill> _bills;
    private readonly Action<Note> _addNote;
    private List<CalendarEvent> _gcalEvents = new();
    private DateTime _viewMonth;
    private bool _gcalLoaded;

    public CalendarWindow(List<Note> notes, Action<Note> addNote)
    {
        InitializeComponent();
        _notes = notes;
        _addNote = addNote;
        var meetingData = MeetingStore.Load();
        _bookings = meetingData.Bookings;
        var finData = FinanceStore.Load();
        _bills = finData.Bills;
        _viewMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        RenderCalendar();
        _ = LoadGCal();
    }

    private async Task LoadGCal()
    {
        var (key, calId) = GoogleCalendarConfig.Load();
        if (string.IsNullOrEmpty(key)) return;
        try
        {
            var first = new DateTime(_viewMonth.Year, _viewMonth.Month, 1);
            _gcalEvents = await GoogleCalendarHelper.FetchEvents(key, calId, first, first.AddMonths(1));
            _gcalLoaded = true;
            RenderCalendar();
        }
        catch { }
    }

    private void PrevMonth_Click(object s, RoutedEventArgs e) { _viewMonth = _viewMonth.AddMonths(-1); _gcalEvents.Clear(); RenderCalendar(); _ = LoadGCal(); }
    private void NextMonth_Click(object s, RoutedEventArgs e) { _viewMonth = _viewMonth.AddMonths(1); _gcalEvents.Clear(); RenderCalendar(); _ = LoadGCal(); }
    private void Today_Click(object s, RoutedEventArgs e) { _viewMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); _gcalEvents.Clear(); RenderCalendar(); _ = LoadGCal(); }

    private void RenderCalendar()
    {
        lblMonth.Text = $"{_viewMonth:MMMM yyyy}";

        gridDayHeaders.Children.Clear();
        gridDayHeaders.ColumnDefinitions.Clear();
        var dayNames = new[] { "อา", "จ", "อ", "พ", "พฤ", "ศ", "ส" };
        for (int i = 0; i < 7; i++)
        {
            gridDayHeaders.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var lbl = new TextBlock { Text = dayNames[i], FontSize = 12, FontWeight = FontWeights.Bold,
                Foreground = B(i == 0 ? "#e74c3c" : "#8b8fa7"), HorizontalAlignment = HorizontalAlignment.Center };
            Grid.SetColumn(lbl, i);
            gridDayHeaders.Children.Add(lbl);
        }

        gridCalendar.Children.Clear();
        gridCalendar.ColumnDefinitions.Clear();
        gridCalendar.RowDefinitions.Clear();
        for (int i = 0; i < 7; i++) gridCalendar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (int i = 0; i < 6; i++) gridCalendar.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var firstDay = _viewMonth;
        var startDow = (int)firstDay.DayOfWeek;
        var daysInMonth = DateTime.DaysInMonth(_viewMonth.Year, _viewMonth.Month);

        int noteCount = 0, bookingCount = 0, billCount = 0, gcalCount = 0;

        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(_viewMonth.Year, _viewMonth.Month, day);
            var dateStr = date.ToString("yyyy-MM-dd");
            var col = (int)date.DayOfWeek;
            var row = (day + startDow - 1) / 7;

            var events = GetDayEvents(dateStr);
            noteCount += events.Count(e => e.type == "note");
            bookingCount += events.Count(e => e.type == "meeting");
            billCount += events.Count(e => e.type == "bill");
            gcalCount += events.Count(e => e.type == "gcal");

            var isToday = date.Date == DateTime.Today;
            var hasEvents = events.Any();
            var cell = new Border
            {
                Background = B(isToday ? "#6c63ff22" : hasEvents ? "#161828" : "Transparent"),
                BorderBrush = B(isToday ? "#6c63ff" : "#2a2d45"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(2),
                Padding = new Thickness(4, 2, 4, 2),
                Cursor = hasEvents ? Cursors.Hand : Cursors.Arrow
            };

            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = day.ToString(), FontSize = 13,
                FontWeight = isToday ? FontWeights.Bold : FontWeights.Normal,
                Foreground = B(isToday ? "#6c63ff" : col == 0 ? "#e74c3c" : "#e8e9f0")
            });

            if (events.Any())
            {
                var dots = new WrapPanel();
                foreach (var ev in events.Take(4))
                {
                    var dotColor = ev.type switch
                    {
                        "note" => "#6c63ff",
                        "meeting" => "#00b894",
                        "bill" => "#e74c3c",
                        "gcal" => "#f39c12",
                        _ => "#8b8fa7"
                    };
                    dots.Children.Add(new Ellipse { Width = 6, Height = 6, Fill = B(dotColor), Margin = new Thickness(1, 2, 1, 0) });
                }
                if (events.Count > 4) dots.Children.Add(new TextBlock { Text = $"+{events.Count - 4}", FontSize = 7, Foreground = B("#8b8fa7") });
                sp.Children.Add(dots);
            }

            cell.Child = sp;
            var d = dateStr;
            cell.MouseLeftButtonUp += (_, _) => ShowDayDetail(d);
            Grid.SetColumn(cell, col);
            Grid.SetRow(cell, row);
            gridCalendar.Children.Add(cell);
        }

        var gcalText = _gcalLoaded ? $"  ·  📅 {gcalCount} Google Calendar" : "  ·  📅 GCal ไม่ได้เชื่อม";
        lblSummary.Text = $"📝 {noteCount} โน้ต  ·  🏢 {bookingCount} ประชุม  ·  📄 {billCount} บิล{gcalText}";
    }

    private List<(string type, string icon, string text, string color, CalendarEvent? gcalEvent)> GetDayEvents(string dateStr)
    {
        var events = new List<(string, string, string, string, CalendarEvent?)>();

        foreach (var n in _notes.Where(n => !string.IsNullOrEmpty(n.Reminder) && n.Reminder.StartsWith(dateStr)))
            events.Add(("note", n.PriorityIcon, $"📝 {n.Title} ({n.Reminder.Split(' ').LastOrDefault()})", "#6c63ff", null));

        foreach (var b in _bookings.Where(b => b.Date == dateStr))
            events.Add(("meeting", "🏢", $"🏢 {b.Topic} ({b.StartTime}-{b.EndTime})", "#00b894", null));

        foreach (var bill in _bills.Where(b => !b.Paid && b.DueDate == dateStr))
            events.Add(("bill", "📄", $"📄 {bill.Title} (฿{bill.Amount:N0})", "#e74c3c", null));

        foreach (var ev in _gcalEvents.Where(e => e.Start.StartsWith(dateStr) ||
            (DateTime.TryParse(e.Start, out var dt) && dt.ToString("yyyy-MM-dd") == dateStr)))
            events.Add(("gcal", "📅", $"📅 {ev.Title} ({GoogleCalendarHelper.FormatTime(ev.Start)}–{GoogleCalendarHelper.FormatTime(ev.End)})" +
                (!string.IsNullOrEmpty(ev.Location) ? $" 📍{ev.Location}" : ""), "#f39c12", ev));

        return events;
    }

    private void ShowDayDetail(string dateStr)
    {
        var events = GetDayEvents(dateStr);
        if (!events.Any()) { borderDetail.Visibility = Visibility.Collapsed; return; }

        borderDetail.Visibility = Visibility.Visible;
        panelDetail.Children.Clear();
        panelDetail.Children.Add(new TextBlock { Text = $"📅 {dateStr}", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = B("#e8e9f0"), Margin = new Thickness(0, 0, 0, 6) });

        // Legend
        var legend = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
        legend.Children.Add(LegendDot("#6c63ff", "โน้ต"));
        legend.Children.Add(LegendDot("#00b894", "ประชุม"));
        legend.Children.Add(LegendDot("#e74c3c", "บิล"));
        legend.Children.Add(LegendDot("#f39c12", "Google Cal"));
        panelDetail.Children.Add(legend);

        foreach (var (type, icon, text, color, gcalEvent) in events)
        {
            var row = new Border { Background = B("#2a2d45"), CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 6, 8, 6), Margin = new Thickness(0, 2, 0, 0) };
            var dp = new DockPanel();
            dp.Children.Add(new TextBlock { Text = text, FontSize = 12, Foreground = B(color), TextWrapping = TextWrapping.Wrap });

            if (gcalEvent != null)
            {
                var btnPanel = new StackPanel { Orientation = Orientation.Horizontal };
                var importBtn = new Button
                {
                    Content = "📝", Padding = new Thickness(4, 2, 4, 2), FontSize = 11,
                    Background = B("#27ae60"), Foreground = Brushes.White,
                    BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
                    ToolTip = "สร้างโน้ตจากนัดนี้"
                };
                var evRef = gcalEvent;
                importBtn.Click += (_, _) =>
                {
                    _addNote(GoogleCalendarHelper.EventToNote(evRef));
                    MessageBox.Show($"สร้างโน้ต \"{evRef.Title}\" สำเร็จ!", "📅");
                };
                btnPanel.Children.Add(importBtn);

                if (!string.IsNullOrEmpty(gcalEvent.Link))
                {
                    var linkBtn = new Button
                    {
                        Content = "🔗", Padding = new Thickness(4, 2, 4, 2), FontSize = 11,
                        Background = B("#4285f4"), Foreground = Brushes.White,
                        BorderThickness = new Thickness(0), Cursor = Cursors.Hand, Margin = new Thickness(4, 0, 0, 0),
                        ToolTip = "เปิดใน Google Calendar"
                    };
                    linkBtn.Click += (_, _) => System.Diagnostics.Process.Start(
                        new System.Diagnostics.ProcessStartInfo(evRef.Link) { UseShellExecute = true });
                    btnPanel.Children.Add(linkBtn);
                }

                DockPanel.SetDock(btnPanel, Dock.Right);
                dp.Children.Insert(0, btnPanel);
            }

            row.Child = dp;
            panelDetail.Children.Add(row);
        }
    }

    private static StackPanel LegendDot(string color, string label)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 12, 0) };
        sp.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = B(color), Margin = new Thickness(0, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center });
        sp.Children.Add(new TextBlock { Text = label, FontSize = 10, Foreground = B("#8b8fa7"), VerticalAlignment = VerticalAlignment.Center });
        return sp;
    }

    private static SolidColorBrush B(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
}
