using System;
using System.Collections.Generic;
using SysDrawing = System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using QRCoder;

namespace NoteApp;

public partial class MainWindow : Window
{
    private List<Note> _notes;
    private List<Note> _trash;
    private int _selectedIndex = -1;
    private string _filterCat = "ทั้งหมด";
    private bool _groupMode = true;
    private bool _darkMode = true;
    private bool _isReady;
    private AppTheme _theme = ThemeManager.GetTheme("Midnight");
    private string _currentThemeName = "Midnight";
    private readonly DispatcherTimer _reminderTimer;
    private static readonly string[] DefaultCats = { "ทั่วไป", "งาน", "ส่วนตัว", "ไอเดีย", "สำคัญ" };

    public MainWindow()
    {
        InitializeComponent();
        _notes = NoteStore.Load();
        _trash = TrashStore.Load();
        dpDate.SelectedDate = DateTime.Today;
        txtHour.Text = DateTime.Now.Hour.ToString("D2");
        txtMinute.Text = DateTime.Now.Minute.ToString("D2");

        lblUser.Text = $"👤 {App.CurrentUser}";

        _currentThemeName = ThemeManager.LoadSavedTheme();
        _theme = ThemeManager.GetTheme(_currentThemeName);
        _darkMode = _theme.IsDark;
        ApplyTheme();

        RefreshCategories();
        RefreshList();

        _reminderTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _reminderTimer.Tick += (_, _) => CheckReminders();
        _reminderTimer.Start();
        CheckReminders();
        _isReady = true;
    }

    // ====================================================
    // Categories
    // ====================================================
    private List<string> GetAllCategories()
    {
        var cats = new List<string>(DefaultCats);
        foreach (var n in _notes)
        {
            var c = string.IsNullOrEmpty(n.Category) ? "ทั่วไป" : n.Category;
            if (!cats.Contains(c)) cats.Add(c);
        }
        return cats;
    }

    private void RefreshCategories()
    {
        var cats = GetAllCategories();
        var selected = cmbCategory.SelectedItem as string ?? "ทั่วไป";
        cmbCategory.ItemsSource = cats;
        cmbCategory.SelectedItem = cats.Contains(selected) ? selected : "ทั่วไป";
        RenderCatTabs();
    }

    private void RenderCatTabs()
    {
        panelCatTabs.Children.Clear();
        var all = new List<string> { "ทั้งหมด" };
        all.AddRange(GetAllCategories());
        foreach (var cat in all)
        {
            var btn = new Button
            {
                Content = cat,
                FontSize = 12,
                Padding = new Thickness(12, 5, 12, 5),
                Margin = new Thickness(0, 0, 6, 0),
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(0),
            };
            btn.Template = CreateTabTemplate(cat == _filterCat);
            btn.Click += (_, _) => { _filterCat = cat; RenderCatTabs(); RefreshList(); };
            panelCatTabs.Children.Add(btn);
        }
    }

    private ControlTemplate CreateTabTemplate(bool active)
    {
        var bg = active ? _theme.Accent : _theme.Surface2;
        var fg = active ? "White" : _theme.Text2;
        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.BackgroundProperty, new BrushConverter().ConvertFromString(bg) as Brush);
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(12));
        border.SetValue(Border.PaddingProperty, new Thickness(10, 4, 10, 4));
        var cp = new FrameworkElementFactory(typeof(ContentPresenter));
        cp.SetValue(TextBlock.ForegroundProperty, new BrushConverter().ConvertFromString(fg) as Brush);
        cp.SetValue(TextBlock.FontSizeProperty, 11.5);
        cp.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        border.AppendChild(cp);
        template.VisualTree = border;
        return template;
    }

    private void AddCategory_Click(object sender, RoutedEventArgs e)
    {
        var dlgInput = new InputDialog("เพิ่มหมวดหมู่", "ชื่อหมวดหมู่ใหม่:");
        if (dlgInput.ShowDialog() != true) return;
        var name = dlgInput.Answer?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return;
        var cats = GetAllCategories();
        if (!cats.Contains(name)) cats.Add(name);
        cmbCategory.ItemsSource = cats;
        cmbCategory.SelectedItem = name;
        RenderCatTabs();
        SetStatus($"เพิ่มหมวดหมู่ \"{name}\" สำเร็จ");
    }

    // ====================================================
    // Note CRUD
    // ====================================================
    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var title = txtTitle.Text.Trim();
        if (string.IsNullOrEmpty(title)) { MessageBox.Show("กรุณาใส่หัวข้อโน้ต", "แจ้งเตือน"); return; }
        var note = new Note
        {
            Title = title,
            Content = txtContent.Text.Trim(),
            Category = cmbCategory.SelectedItem as string ?? "ทั่วไป",
            Priority = (cmbPriority.SelectedItem as ComboBoxItem)?.Tag as string ?? "ปกติ",
        };
        SetReminder(note);
        _notes.Add(note);
        NoteStore.Save(_notes);
        RefreshCategories();
        RefreshList();
        ClearForm();
        SetStatus($"เพิ่มโน้ต \"{title}\" สำเร็จ");
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedIndex < 0) { MessageBox.Show("กรุณาเลือกโน้ต", "แจ้งเตือน"); return; }
        var title = txtTitle.Text.Trim();
        if (string.IsNullOrEmpty(title)) { MessageBox.Show("กรุณาใส่หัวข้อ", "แจ้งเตือน"); return; }
        var n = _notes[_selectedIndex];
        n.Title = title;
        n.Content = txtContent.Text.Trim();
        n.Category = cmbCategory.SelectedItem as string ?? "ทั่วไป";
        n.Priority = (cmbPriority.SelectedItem as ComboBoxItem)?.Tag as string ?? "ปกติ";
        n.Notified = false;
        SetReminder(n);
        NoteStore.Save(_notes);
        RefreshCategories();
        RefreshList();
        ClearForm();
        SetStatus($"แก้ไขโน้ต \"{title}\" สำเร็จ");
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedIndex < 0) { MessageBox.Show("กรุณาเลือกโน้ต", "แจ้งเตือน"); return; }
        var note = _notes[_selectedIndex];
        if (MessageBox.Show($"ย้ายโน้ต \"{note.Title}\" ไปถังขยะ?", "ยืนยัน", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        note.Deleted = true;
        note.DeletedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        _trash.Add(note);
        TrashStore.Save(_trash);
        _notes.RemoveAt(_selectedIndex);
        NoteStore.Save(_notes);
        _selectedIndex = -1;
        RefreshList();
        ClearForm();
        SetStatus($"🗑️ ย้าย \"{note.Title}\" ไปถังขยะแล้ว (กู้คืนได้ภายใน 30 วัน)");
    }

    private void Pin_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedIndex < 0) { MessageBox.Show("กรุณาเลือกโน้ต", "แจ้งเตือน"); return; }
        _notes[_selectedIndex].Pinned = !_notes[_selectedIndex].Pinned;
        NoteStore.Save(_notes);
        var action = _notes[_selectedIndex].Pinned ? "ปักหมุด" : "เลิกปักหมุด";
        SetStatus($"📌 {action} \"{_notes[_selectedIndex].Title}\"");
        RefreshList();
    }

    private void Clear_Click(object sender, RoutedEventArgs e) => ClearForm();

    private void SetReminder(Note note)
    {
        var remItem = cmbRemind.SelectedItem as ComboBoxItem;
        int remMin = int.Parse(remItem?.Tag as string ?? "-1");
        if (remMin < 0 || dpDate.SelectedDate == null) { note.Reminder = ""; note.RemindMinutes = -1; return; }
        int.TryParse(txtHour.Text, out int h);
        int.TryParse(txtMinute.Text, out int m);
        note.Reminder = $"{dpDate.SelectedDate.Value:yyyy-MM-dd} {h:D2}:{m:D2}";
        note.RemindMinutes = remMin;
    }

    // ====================================================
    // Render list
    // ====================================================
    private void RefreshList()
    {
        panelNotes.Children.Clear();
        var query = txtSearch.Text?.Trim().ToLower() ?? "";

        var matched = _notes.Select((n, i) => (n, i)).Where(x =>
        {
            var cat = string.IsNullOrEmpty(x.n.Category) ? "ทั่วไป" : x.n.Category;
            if (_filterCat != "ทั้งหมด" && cat != _filterCat) return false;
            if (!string.IsNullOrEmpty(query) &&
                !x.n.Title.ToLower().Contains(query) &&
                !(x.n.Content ?? "").ToLower().Contains(query)) return false;
            return true;
        }).ToList();

        var pinned = matched.Where(x => x.n.Pinned).OrderBy(x => x.n.PriorityRank).ToList();
        var unpinned = matched.Where(x => !x.n.Pinned).OrderBy(x => x.n.PriorityRank).ToList();

        if (_groupMode && _filterCat == "ทั้งหมด")
        {
            if (pinned.Any())
            {
                AddGroupHeader("ปักหมุด", pinned.Count);
                foreach (var (n, i) in pinned) AddNoteCard(n, i);
            }
            var catGroups = unpinned.GroupBy(x => string.IsNullOrEmpty(x.n.Category) ? "ทั่วไป" : x.n.Category);
            var catOrder = GetAllCategories();
            foreach (var cat in catOrder)
            {
                var group = catGroups.FirstOrDefault(g => g.Key == cat);
                if (group == null) continue;
                AddGroupHeader(cat, group.Count());
                foreach (var (n, i) in group) AddNoteCard(n, i);
            }
        }
        else
        {
            foreach (var (n, i) in pinned.Concat(unpinned)) AddNoteCard(n, i);
        }

        if (!matched.Any())
        {
            panelNotes.Children.Add(new TextBlock
            {
                Text = "📝\nไม่พบโน้ต",
                FontSize = 16, TextAlignment = TextAlignment.Center,
                Foreground = FindBrush("Text2Brush"),
                Margin = new Thickness(0, 40, 0, 0)
            });
        }

        var catCount = matched.Select(x => x.n.Category ?? "ทั่วไป").Distinct().Count();
        var info = $"โน้ต {matched.Count} รายการ";
        if (pinned.Any()) info += $"  📌 {pinned.Count}";
        if (_groupMode && _filterCat == "ทั้งหมด") info += $"  {catCount} หมวด";
        SetStatus(info);
    }

    private void AddGroupHeader(string title, int count)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(14, 14, 14, 4) };
        sp.Children.Add(new TextBlock
        {
            Text = title, FontSize = 12, FontWeight = FontWeights.SemiBold,
            Foreground = FindBrush("Text2Brush"), VerticalAlignment = VerticalAlignment.Center,
        });
        sp.Children.Add(new Border
        {
            Background = FindBrush("Surface2Brush"), CornerRadius = new CornerRadius(8),
            Padding = new Thickness(6, 1, 6, 1), Margin = new Thickness(6, 0, 0, 0),
            Child = new TextBlock { Text = count.ToString(), FontSize = 10, Foreground = FindBrush("Text2Brush") }
        });
        panelNotes.Children.Add(sp);
    }

    private void AddNoteCard(Note note, int realIndex)
    {
        bool isSelected = realIndex == _selectedIndex;
        var accentColor = (Color)ColorConverter.ConvertFromString(_theme.Accent);

        var border = new Border
        {
            Background = isSelected
                ? new SolidColorBrush(Color.FromArgb(20, accentColor.R, accentColor.G, accentColor.B))
                : Brushes.Transparent,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 12, 14, 12),
            Margin = new Thickness(2, 1, 2, 1),
            Cursor = Cursors.Hand,
        };

        var hoverBrush = FindBrush("Surface2Brush");
        var idx = realIndex;
        border.MouseEnter += (_, _) => { if (idx != _selectedIndex) border.Background = hoverBrush; };
        border.MouseLeave += (_, _) =>
        {
            if (idx != _selectedIndex) border.Background = Brushes.Transparent;
        };

        var stack = new StackPanel();

        // Title row
        var titleRow = new DockPanel();
        if (note.Pinned)
        {
            var pinIcon = new TextBlock { Text = "📌", FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 0) };
            DockPanel.SetDock(pinIcon, Dock.Right);
            titleRow.Children.Add(pinIcon);
        }
        titleRow.Children.Add(new TextBlock
        {
            Text = (note.Locked ? "🔒 " : "") + note.Title,
            FontSize = 13.5, FontWeight = FontWeights.Medium,
            Foreground = FindBrush("TextBrush"),
            TextTrimming = TextTrimming.CharacterEllipsis,
        });
        stack.Children.Add(titleRow);

        // Content preview
        if (!string.IsNullOrEmpty(note.Content) && !note.Locked)
        {
            var preview = note.Content.Replace("\r", "").Replace("\n", " ");
            if (preview.Length > 70) preview = preview[..70] + "…";
            stack.Children.Add(new TextBlock
            {
                Text = preview, FontSize = 12,
                Foreground = FindBrush("Text2Brush"),
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxHeight = 34, TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0),
                Opacity = 0.8,
            });
        }

        // Meta row
        var meta = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
        meta.Children.Add(new Border
        {
            Background = FindBrush("Surface2Brush"), CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 1, 6, 1), Margin = new Thickness(0, 0, 6, 0),
            Child = new TextBlock { Text = note.Category ?? "ทั่วไป", FontSize = 10, Foreground = FindBrush("Text2Brush") }
        });

        var dotColor = note.Priority switch
        {
            "สูง" => "#e5484d", "ปานกลาง" => "#f5a623", "ต่ำ" => "#30a46c", _ => ""
        };
        if (!string.IsNullOrEmpty(dotColor))
        {
            meta.Children.Add(new Ellipse
            {
                Width = 6, Height = 6,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dotColor)),
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0)
            });
        }

        meta.Children.Add(new TextBlock
        {
            Text = note.Created, FontSize = 10,
            Foreground = FindBrush("Text2Brush"), VerticalAlignment = VerticalAlignment.Center,
        });

        if (!string.IsNullOrEmpty(note.Reminder))
            meta.Children.Add(new TextBlock { Text = " ⏰", FontSize = 10, VerticalAlignment = VerticalAlignment.Center });

        stack.Children.Add(meta);
        border.Child = stack;
        border.MouseLeftButtonUp += (_, _) => SelectNote(realIndex);
        panelNotes.Children.Add(border);
    }

    private Border MakeBadge(string text, string? priority)
    {
        var bg = FindBrush("Surface2Brush");
        var fg = FindBrush("Text2Brush");
        if (priority == "สูง") { bg = new SolidColorBrush(Color.FromArgb(40, 231, 76, 60)); fg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e74c3c")); }
        else if (priority == "ปานกลาง") { bg = new SolidColorBrush(Color.FromArgb(40, 241, 196, 15)); fg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#d4a800")); }
        else if (priority == "ต่ำ") { bg = new SolidColorBrush(Color.FromArgb(40, 46, 204, 113)); fg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ecc71")); }

        return new Border
        {
            Background = bg, CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 2, 8, 2), Margin = new Thickness(0, 0, 6, 0),
            Child = new TextBlock { Text = text, FontSize = 11, Foreground = fg }
        };
    }

    private void SelectNote(int idx)
    {
        _selectedIndex = idx;
        var n = _notes[idx];

        if (n.Locked)
        {
            var dlg = new InputDialog("🔒 โน้ตถูกล็อค", "ใส่รหัสผ่านเพื่อดูเนื้อหา:") { Owner = this };
            if (dlg.ShowDialog() != true || dlg.Answer != n.Password)
            {
                if (dlg.DialogResult == true)
                    MessageBox.Show("รหัสผ่านไม่ถูกต้อง", "❌", MessageBoxButton.OK, MessageBoxImage.Error);
                _selectedIndex = -1;
                return;
            }
        }

        txtTitle.Text = n.Title;
        txtContent.Text = n.Content;
        cmbCategory.SelectedItem = n.Category ?? "ทั่วไป";
        foreach (ComboBoxItem item in cmbPriority.Items)
            if ((string)item.Tag == (n.Priority ?? "ปกติ")) { cmbPriority.SelectedItem = item; break; }

        if (!string.IsNullOrEmpty(n.Reminder) && DateTime.TryParse(n.Reminder, out var rdt))
        {
            dpDate.SelectedDate = rdt.Date;
            txtHour.Text = rdt.Hour.ToString("D2");
            txtMinute.Text = rdt.Minute.ToString("D2");
            foreach (ComboBoxItem item in cmbRemind.Items)
                if ((string)item.Tag == n.RemindMinutes.ToString()) { cmbRemind.SelectedItem = item; break; }
        }
        else
        {
            cmbRemind.SelectedIndex = 0;
        }
        RefreshList();
    }

    private void ClearForm()
    {
        txtTitle.Text = "";
        txtContent.Text = "";
        cmbCategory.SelectedIndex = 0;
        cmbPriority.SelectedIndex = 0;
        cmbRemind.SelectedIndex = 0;
        dpDate.SelectedDate = DateTime.Today;
        txtHour.Text = DateTime.Now.Hour.ToString("D2");
        txtMinute.Text = DateTime.Now.Minute.ToString("D2");
        _selectedIndex = -1;
        RefreshList();
    }

    // ====================================================
    // Search, Filter, Theme
    // ====================================================
    private void Search_Changed(object sender, TextChangedEventArgs e) => RefreshList();

    private void AIHub_Click(object sender, RoutedEventArgs e)
    {
        var win = new AIHubWindow(_notes, n =>
        {
            _notes.Add(n);
            NoteStore.Save(_notes);
            RefreshCategories();
            RefreshList();
        }) { Owner = this };
        win.ShowDialog();
    }

    private void SmartHome_Click(object sender, RoutedEventArgs e)
    {
        if (!AuthGuard.RequireAuth("🏠 บ้านอัจฉริยะ")) return;
        var win = new SmartHomeWindow() { Owner = this };
        win.ShowDialog();
    }

    private void SetAuth_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new InputDialog("🔐 ตั้งรหัส Auth", "ตั้งรหัสผ่านสำหรับหมวดสำคัญ:\n(การเงิน, บ้านอัจฉริยะ)") { Owner = this };
        if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.Answer))
        {
            AuthGuard.SetupAuth(dlg.Answer.Trim());
            MessageBox.Show("ตั้งรหัส Auth สำเร็จ!\n\nจะถามยืนยันก่อนเข้า:\n• 💰 การเงิน\n• 🏠 บ้านอัจฉริยะ\n\n(Session 15 นาที ไม่ต้องกรอกซ้ำ)", "🔐 Auth");
        }
    }

    private void Calendar_Click(object sender, RoutedEventArgs e)
    {
        var win = new CalendarWindow(_notes, n =>
        {
            _notes.Add(n);
            NoteStore.Save(_notes);
            RefreshCategories();
            RefreshList();
        }) { Owner = this };
        win.ShowDialog();
    }

    private void GCal_Click(object sender, RoutedEventArgs e)
    {
        var win = new GCalWindow(n =>
        {
            _notes.Add(n);
            NoteStore.Save(_notes);
            RefreshCategories();
            RefreshList();
        }) { Owner = this };
        win.ShowDialog();
    }

    private void SetPIN_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new LoginWindow() { Owner = this };
        dlg.ShowDialog();
        if (dlg.Authenticated)
            MessageBox.Show("ตั้ง/เปลี่ยน PIN สำเร็จ!\nจะถามตอนเปิดแอปครั้งต่อไป", "🔑 PIN");
    }

    private void Finance_Click(object sender, RoutedEventArgs e)
    {
        if (!AuthGuard.RequireAuth("💰 การเงิน")) return;
        var win = new FinanceWindow() { Owner = this };
        win.ShowDialog();
    }

    private void Summary_Click(object sender, RoutedEventArgs e)
    {
        var win = new SummaryWindow(_notes) { Owner = this };
        win.ShowDialog();
    }

    private void Meeting_Click(object sender, RoutedEventArgs e)
    {
        var win = new MeetingRoomWindow(_notes, n =>
        {
            _notes.Add(n);
            NoteStore.Save(_notes);
            RefreshCategories();
            RefreshList();
        }) { Owner = this };
        win.ShowDialog();
    }

    private void Dashboard_Click(object sender, RoutedEventArgs e)
    {
        var win = new DashboardWindow(_notes, _theme) { Owner = this };
        win.ShowDialog();
    }

    private void GroupToggle_Click(object sender, RoutedEventArgs e)
    {
        _groupMode = !_groupMode;
        btnGroupToggle.Content = _groupMode ? "📂" : "📋";
        btnGroupToggle.ToolTip = _groupMode ? "แบ่งกลุ่ม (เปิด)" : "แบ่งกลุ่ม (ปิด)";
        RefreshList();
    }

    private void Theme_Click(object sender, RoutedEventArgs e)
    {
        var win = new ThemeWindow(_currentThemeName) { Owner = this };
        if (win.ShowDialog() == true && win.SelectedThemeName != null)
        {
            _currentThemeName = win.SelectedThemeName;
            _theme = ThemeManager.GetTheme(_currentThemeName);
            _darkMode = _theme.IsDark;
            ApplyTheme();
            RefreshList();
            RenderCatTabs();
            SetStatus($"🎨 เปลี่ยนธีมเป็น \"{_currentThemeName}\" แล้ว");
        }
    }

    private void ApplyTheme()
    {
        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_theme.Bg));

        Resources["BgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_theme.Bg));
        Resources["SurfaceBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_theme.Surface));
        Resources["Surface2Brush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_theme.Surface2));
        Resources["TextBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_theme.Text));
        Resources["Text2Brush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_theme.Text2));
        Resources["AccentBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_theme.Accent));
    }

    // ====================================================
    // Export
    // ====================================================
    private void ExportOne_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedIndex < 0) { MessageBox.Show("กรุณาเลือกโน้ต"); return; }
        var n = _notes[_selectedIndex];
        var dlg = new SaveFileDialog { Filter = "Text|*.txt", FileName = n.Title + ".txt" };
        if (dlg.ShowDialog() == true)
        {
            File.WriteAllText(dlg.FileName, FormatNote(n));
            SetStatus($"Export สำเร็จ → {dlg.FileName}");
        }
    }

    private void ExportAll_Click(object sender, RoutedEventArgs e)
    {
        if (!_notes.Any()) { MessageBox.Show("ไม่มีโน้ต"); return; }
        var dlg = new SaveFileDialog { Filter = "Text|*.txt", FileName = "all_notes.txt" };
        if (dlg.ShowDialog() == true)
        {
            var sep = "\n" + new string('═', 50) + "\n\n";
            var txt = $"บันทึกทั้งหมด ({_notes.Count} รายการ)\nExport เมื่อ: {DateTime.Now:yyyy-MM-dd HH:mm}\n{new string('═', 50)}\n\n"
                      + string.Join(sep, _notes.Select(FormatNote));
            File.WriteAllText(dlg.FileName, txt);
            SetStatus($"Export {_notes.Count} โน้ตสำเร็จ");
        }
    }

    private static string FormatNote(Note n) =>
        $"หัวข้อ      : {n.Title}\nหมวดหมู่    : {n.Category}\nความสำคัญ   : {n.Priority}\nวันที่      : {n.Created}\n{new string('─', 40)}\n{n.Content}";

    // ====================================================
    // Reminders
    // ====================================================
    private void CheckReminders()
    {
        var now = DateTime.Now;
        foreach (var n in _notes)
        {
            if (string.IsNullOrEmpty(n.Reminder) || n.Notified || n.RemindMinutes < 0) continue;
            if (!DateTime.TryParse(n.Reminder, out var rdt)) continue;
            var alertTime = rdt.AddMinutes(-n.RemindMinutes);
            if (now >= alertTime)
            {
                n.Notified = true;
                NoteStore.Save(_notes);
                var minText = n.RemindMinutes > 0 ? $" (ล่วงหน้า {n.RemindMinutes} นาที)" : "";
                MessageBox.Show(
                    $"⏰ {n.Title}\n\nหมวด: {n.Category}\nเวลา: {rdt:dd/MM/yyyy HH:mm}{minText}\n\n{(n.Content ?? "").Take(120)}",
                    "แจ้งเตือน!", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    // ====================================================
    // Share / Connect
    // ====================================================
    private void CopyClipboard_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedIndex < 0) { MessageBox.Show("กรุณาเลือกโน้ต"); return; }
        ShareHelper.CopyToClipboard(_notes[_selectedIndex]);
        SetStatus($"📋 คัดลอก \"{_notes[_selectedIndex].Title}\" ไปยัง Clipboard แล้ว");
    }

    private void SendEmail_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedIndex < 0) { MessageBox.Show("กรุณาเลือกโน้ต"); return; }
        ShareHelper.SendEmail(_notes[_selectedIndex]);
        SetStatus($"📧 เปิดแอป Email สำหรับ \"{_notes[_selectedIndex].Title}\"");
    }

    private void ExportCalendar_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedIndex < 0) { MessageBox.Show("กรุณาเลือกโน้ต"); return; }
        var n = _notes[_selectedIndex];
        var dlg = new SaveFileDialog
        {
            Filter = "Calendar|*.ics",
            FileName = n.Title + ".ics",
            Title = "บันทึกเป็นไฟล์ปฏิทิน"
        };
        if (dlg.ShowDialog() == true)
        {
            ShareHelper.ExportToCalendar(n, dlg.FileName);
            if (MessageBox.Show($"สร้างไฟล์ปฏิทิน \"{dlg.FileName}\" สำเร็จ\n\nต้องการเปิดเพื่อเพิ่มในปฏิทินเลยไหม?",
                "📅 ปฏิทิน", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                ShareHelper.OpenCalendarFile(dlg.FileName);
            }
            SetStatus($"📅 สร้างไฟล์ปฏิทิน \"{n.Title}\" สำเร็จ");
        }
    }

    private async void SendLine_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedIndex < 0) { MessageBox.Show("กรุณาเลือกโน้ต"); return; }
        var token = ShareHelper.LoadLineToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            MessageBox.Show("ยังไม่ได้ตั้งค่า LINE Token\n\nกดปุ่ม \"⚙️ ตั้งค่า LINE\" เพื่อใส่ Token ก่อน",
                "💬 LINE Notify", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var n = _notes[_selectedIndex];
        SetStatus($"💬 กำลังส่งไป LINE...");
        try
        {
            var ok = await ShareHelper.SendLineNotify(n, token);
            if (ok)
            {
                SetStatus($"💬 ส่ง \"{n.Title}\" ไป LINE สำเร็จ!");
                MessageBox.Show($"ส่งโน้ต \"{n.Title}\" ไป LINE สำเร็จ!", "💬 LINE Notify");
            }
            else
            {
                SetStatus("❌ ส่ง LINE ไม่สำเร็จ — ตรวจสอบ Token");
                MessageBox.Show("ส่งไม่สำเร็จ\nตรวจสอบ Token ว่าถูกต้องหรือยังไม่หมดอายุ",
                    "❌ LINE Notify", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            SetStatus("❌ ส่ง LINE ไม่สำเร็จ");
            MessageBox.Show($"เกิดข้อผิดพลาด:\n{ex.Message}", "❌ LINE Notify");
        }
    }

    private void SetupLine_Click(object sender, RoutedEventArgs e)
    {
        var current = ShareHelper.LoadLineToken();
        var dlg = new InputDialog("ตั้งค่า LINE Notify",
            "ใส่ LINE Notify Token:\n\n(ขอ Token ได้ที่ notify-bot.line.me)")
        {
            Owner = this
        };
        if (!string.IsNullOrEmpty(current))
            dlg.SetInitialText(current);

        if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.Answer))
        {
            ShareHelper.SaveLineToken(dlg.Answer.Trim());
            SetStatus("✅ บันทึก LINE Token สำเร็จ");
            MessageBox.Show("บันทึก LINE Token สำเร็จ!\n\nลองกดปุ่ม \"💬 LINE\" เพื่อทดสอบส่งโน้ต",
                "✅ ตั้งค่า LINE");
        }
    }

    // ====================================================
    // QR Code
    // ====================================================
    private void CreateQR_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedIndex < 0) { MessageBox.Show("กรุณาเลือกโน้ต"); return; }
        var note = _notes[_selectedIndex];
        var json = JsonSerializer.Serialize(new {
            note.Title, note.Content, note.Category, note.Priority,
            note.Reminder, note.RemindMinutes
        }, new JsonSerializerOptions { WriteIndented = false });

        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        var data = $"noteapp://{encoded}";

        using var qr = new QRCodeGenerator();
        using var qrData = qr.CreateQrCode(data, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrData);
        var png = qrCode.GetGraphic(8);

        var dlg = new SaveFileDialog { Filter = "PNG|*.png", FileName = $"QR_{note.Title}.png" };
        if (dlg.ShowDialog() == true)
        {
            File.WriteAllBytes(dlg.FileName, png);
            SetStatus($"🔲 สร้าง QR Code \"{note.Title}\" สำเร็จ");
            if (MessageBox.Show("สร้าง QR Code สำเร็จ!\n\nต้องการเปิดดูไหม?", "🔲 QR Code",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
        }
    }

    private void ScanQR_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new InputDialog("📷 สแกน QR Code",
            "วิธีใช้:\n1. ใช้มือถือสแกน QR Code ที่สร้างจากแอป\n2. คัดลอกข้อความที่ได้ (เริ่มต้นด้วย noteapp://)\n3. วางลงในช่องด้านล่าง:")
        { Owner = this };

        // Try clipboard first
        if (Clipboard.ContainsText())
        {
            var clip = Clipboard.GetText();
            if (clip.StartsWith("noteapp://"))
                dlg.SetInitialText(clip);
        }

        if (dlg.ShowDialog() != true) return;
        var result = dlg.Answer.Trim();

        try
        {
            if (!result.StartsWith("noteapp://"))
            {
                MessageBox.Show("ข้อมูลไม่ถูกต้อง\n\nต้องเริ่มต้นด้วย noteapp://\nลองสแกน QR Code ที่สร้างจากแอป Note App",
                    "📷 สแกน QR", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var encoded = result.Replace("noteapp://", "");
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var note = new Note
            {
                Title = root.GetProperty("Title").GetString() ?? "",
                Content = root.GetProperty("Content").GetString() ?? "",
                Category = root.GetProperty("Category").GetString() ?? "ทั่วไป",
                Priority = root.GetProperty("Priority").GetString() ?? "ปกติ",
            };
            if (root.TryGetProperty("Reminder", out var rem)) note.Reminder = rem.GetString() ?? "";
            if (root.TryGetProperty("RemindMinutes", out var rm)) note.RemindMinutes = rm.GetInt32();

            if (MessageBox.Show($"พบโน้ตจาก QR Code:\n\n📝 {note.Title}\n📂 {note.Category}\n{note.PriorityIcon} {note.Priority}\n{(string.IsNullOrEmpty(note.Content) ? "" : $"\n{note.Content[..Math.Min(note.Content.Length, 100)]}")}\n\nต้องการเพิ่มเข้าโน้ตของคุณไหม?",
                "📷 สแกน QR", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _notes.Add(note);
                NoteStore.Save(_notes);
                RefreshCategories();
                RefreshList();
                SetStatus($"📷 นำเข้าโน้ต \"{note.Title}\" จาก QR สำเร็จ!");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ไม่สามารถอ่านข้อมูลได้:\n{ex.Message}", "❌ สแกน QR");
        }
    }

    // ====================================================
    // Lock Note
    // ====================================================
    private void LockNote_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedIndex < 0) { MessageBox.Show("กรุณาเลือกโน้ต"); return; }
        var note = _notes[_selectedIndex];

        if (note.Locked)
        {
            var dlg = new InputDialog("ปลดล็อคโน้ต", "ใส่รหัสผ่านเพื่อปลดล็อค:") { Owner = this };
            if (dlg.ShowDialog() == true && dlg.Answer == note.Password)
            {
                note.Locked = false;
                note.Password = "";
                NoteStore.Save(_notes);
                RefreshList();
                SetStatus($"🔓 ปลดล็อค \"{note.Title}\" แล้ว");
            }
            else if (dlg.DialogResult == true)
            {
                MessageBox.Show("รหัสผ่านไม่ถูกต้อง", "❌ ล็อค", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            var dlg = new InputDialog("ล็อคโน้ต", "ตั้งรหัสผ่านสำหรับโน้ตนี้:") { Owner = this };
            if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.Answer))
            {
                note.Locked = true;
                note.Password = dlg.Answer;
                NoteStore.Save(_notes);
                RefreshList();
                SetStatus($"🔒 ล็อค \"{note.Title}\" แล้ว");
            }
        }
    }

    // ====================================================
    // Trash Bin
    // ====================================================
    private void TrashBin_Click(object sender, RoutedEventArgs e)
    {
        // Clean expired (>30 days)
        _trash.RemoveAll(n =>
            DateTime.TryParse(n.DeletedAt, out var dt) && (DateTime.Now - dt).TotalDays > 30);
        TrashStore.Save(_trash);

        var win = new TrashWindow(_trash, note =>
        {
            _notes.Add(note);
            NoteStore.Save(_notes);
            _trash.Remove(note);
            TrashStore.Save(_trash);
            RefreshCategories();
            RefreshList();
        }) { Owner = this };
        win.ShowDialog();
    }

    // ====================================================
    // AI อัจฉริยะ
    // ====================================================
    private void AIAutoCategory_Click(object sender, RoutedEventArgs e)
    {
        var title = txtTitle.Text.Trim();
        var content = txtContent.Text.Trim();
        if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(content))
        {
            MessageBox.Show("กรุณาพิมพ์หัวข้อหรือเนื้อหาก่อน\nAI จะวิเคราะห์และจัดหมวดให้อัตโนมัติ", "🤖 AI");
            return;
        }
        var sugCat = AIHelper.SuggestCategory(title, content);
        var sugPri = AIHelper.SuggestPriority(title, content);

        cmbCategory.SelectedItem = sugCat;
        if (cmbCategory.SelectedItem == null)
        {
            var cats = GetAllCategories();
            if (!cats.Contains(sugCat)) cats.Add(sugCat);
            cmbCategory.ItemsSource = cats;
            cmbCategory.SelectedItem = sugCat;
        }
        foreach (ComboBoxItem item in cmbPriority.Items)
            if ((string)item.Tag == sugPri) { cmbPriority.SelectedItem = item; break; }

        RefreshCategories();
        SetStatus($"🤖 AI แนะนำ: หมวด \"{sugCat}\" ความสำคัญ \"{sugPri}\"");
        MessageBox.Show($"🤖 AI วิเคราะห์แล้ว:\n\n📂 หมวดหมู่: {sugCat}\n{(sugPri == "สูง" ? "🔴" : sugPri == "ปานกลาง" ? "🟡" : "⚪")} ความสำคัญ: {sugPri}\n\nตั้งค่าให้แล้ว สามารถแก้ไขได้ก่อนบันทึก", "🤖 AI จัดหมวดอัตโนมัติ");
    }

    // ====================================================
    // เทมเพลต
    // ====================================================
    private void Template_Click(object sender, RoutedEventArgs e)
    {
        var win = new TemplateWindow() { Owner = this };
        if (win.ShowDialog() == true && win.SelectedIndex >= 0)
        {
            var t = NoteTemplates.Templates[win.SelectedIndex];
            txtTitle.Text = t.Title;
            txtContent.Text = t.Content;
            cmbCategory.SelectedItem = t.Category;
            if (cmbCategory.SelectedItem == null)
            {
                var cats = GetAllCategories();
                if (!cats.Contains(t.Category)) cats.Add(t.Category);
                cmbCategory.ItemsSource = cats;
                cmbCategory.SelectedItem = t.Category;
            }
            RefreshCategories();
            SetStatus($"📝 ใช้เทมเพลต \"{t.Name}\" แล้ว — แก้ไขเนื้อหาได้เลย");
        }
    }

    // ====================================================
    // Checklist
    // ====================================================
    private void Checklist_Click(object sender, RoutedEventArgs e)
    {
        var content = txtContent.Text;
        if (content.Contains("☐") || content.Contains("☑"))
        {
            content = content.Replace("☐ ", "☑ ");
            txtContent.Text = content;
            SetStatus("☑️ ติ๊กทุกรายการใน Checklist แล้ว");
        }
        else
        {
            var lines = content.Split('\n').Where(l => l.Trim().Length > 0)
                .Select(l => l.TrimStart().StartsWith("☐") || l.TrimStart().StartsWith("☑") ? l : $"☐ {l.Trim()}");
            txtContent.Text = string.Join("\n", lines);
            SetStatus("☑️ แปลงเนื้อหาเป็น Checklist แล้ว");
        }
    }

    // ====================================================
    // Import / Export JSON + Backup
    // ====================================================
    private void ImportJson_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "JSON|*.json", Title = "นำเข้าโน้ตจาก JSON" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var imported = BackupHelper.ImportJson(dlg.FileName);
            if (imported == null || !imported.Any())
            {
                MessageBox.Show("ไม่พบโน้ตในไฟล์นี้", "📥 Import");
                return;
            }
            if (MessageBox.Show($"พบ {imported.Count} โน้ต\n\nต้องการนำเข้าทั้งหมดไหม?\n(โน้ตเดิมจะไม่ถูกลบ)",
                "📥 Import JSON", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _notes.AddRange(imported);
                NoteStore.Save(_notes);
                RefreshCategories();
                RefreshList();
                SetStatus($"📥 นำเข้า {imported.Count} โน้ตสำเร็จ!");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ไม่สามารถอ่านไฟล์ได้:\n{ex.Message}", "❌ Import");
        }
    }

    private void ExportJson_Click(object sender, RoutedEventArgs e)
    {
        if (!_notes.Any()) { MessageBox.Show("ไม่มีโน้ต"); return; }
        var dlg = new SaveFileDialog { Filter = "JSON|*.json", FileName = $"notes_export_{DateTime.Now:yyyyMMdd}.json" };
        if (dlg.ShowDialog() == true)
        {
            BackupHelper.ExportJson(_notes, dlg.FileName);
            SetStatus($"📤 Export {_notes.Count} โน้ตเป็น JSON สำเร็จ");
            MessageBox.Show($"Export {_notes.Count} โน้ตไปที่\n{dlg.FileName}", "📤 Export JSON");
        }
    }

    private void Backup_Click(object sender, RoutedEventArgs e)
    {
        var path = BackupHelper.CreateBackup(_notes);
        var count = BackupHelper.GetBackupCount();
        SetStatus($"💾 Backup สำเร็จ ({count} ไฟล์)");
        MessageBox.Show($"สำรองข้อมูลสำเร็จ!\n\n📁 {path}\n💾 มี Backup ทั้งหมด {count} ไฟล์\n(เก็บล่าสุด 10 ไฟล์)", "💾 Backup");
    }

    // ====================================================
    // Share as HTML
    // ====================================================
    private void ShareHTML_Click(object sender, RoutedEventArgs e)
    {
        if (!_notes.Any()) { MessageBox.Show("ไม่มีโน้ต"); return; }

        var choice = MessageBox.Show("เลือกโน้ตที่จะแชร์:\n\nYes = โน้ตที่เลือกอยู่\nNo = โน้ตทั้งหมด",
            "🌐 แชร์เป็น HTML", MessageBoxButton.YesNoCancel);

        if (choice == MessageBoxResult.Cancel) return;

        List<Note> toShare;
        string title;
        if (choice == MessageBoxResult.Yes && _selectedIndex >= 0)
        {
            toShare = new List<Note> { _notes[_selectedIndex] };
            title = _notes[_selectedIndex].Title;
        }
        else
        {
            toShare = _notes;
            title = "โน้ตทั้งหมด";
        }

        var dlg = new SaveFileDialog { Filter = "HTML|*.html", FileName = $"shared_notes_{DateTime.Now:yyyyMMdd}.html" };
        if (dlg.ShowDialog() == true)
        {
            ShareHTMLHelper.GenerateHTML(toShare, dlg.FileName, title);
            SetStatus($"🌐 สร้างหน้าเว็บแชร์ {toShare.Count} โน้ตสำเร็จ");
            if (MessageBox.Show($"สร้างไฟล์ HTML สำเร็จ!\n\n{dlg.FileName}\n\nเปิดใน Browser เลยไหม?",
                "🌐 แชร์", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
        }
    }

    // ====================================================
    // Keyboard Shortcuts & System Tray
    // ====================================================
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
        {
            switch (e.Key)
            {
                case Key.N: ClearForm(); txtTitle.Focus(); SetStatus("Ctrl+N: โน้ตใหม่"); e.Handled = true; break;
                case Key.S: Add_Click(sender, e); SetStatus("Ctrl+S: บันทึก"); e.Handled = true; break;
                case Key.F: txtSearch.Focus(); txtSearch.SelectAll(); SetStatus("Ctrl+F: ค้นหา"); e.Handled = true; break;
                case Key.E: ExportOne_Click(sender, e); e.Handled = true; break;
                case Key.Q: CreateQR_Click(sender, e); e.Handled = true; break;
                case Key.G: txtGoogleSearch.Focus(); txtGoogleSearch.SelectAll(); SetStatus("Ctrl+G: Google Search"); e.Handled = true; break;
            }
        }
        if (e.Key == Key.Delete && _selectedIndex >= 0) { Delete_Click(sender, e); e.Handled = true; }
        if (e.Key == Key.Escape) { ClearForm(); e.Handled = true; }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_isReady || !IsVisible) return;
        if (MessageBox.Show("ต้องการปิดแอปหรือไม่?", "Note App", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            e.Cancel = true;
    }

    // ====================================================
    // Quick Note
    // ====================================================
    // ====================================================
    // Google Quick Access
    // ====================================================
    private void Study_Click(object sender, RoutedEventArgs e)
    {
        var win = new StudyWindow(n =>
        {
            _notes.Add(n);
            NoteStore.Save(_notes);
            RefreshCategories();
            RefreshList();
        }) { Owner = this };
        win.ShowDialog();
    }

    private void GoogleSearch_Click(object sender, RoutedEventArgs e) => OpenGoogle("https://www.google.com/search?q=");
    private void GoogleMaps_Click(object sender, RoutedEventArgs e) => OpenGoogle("https://www.google.com/maps/search/");
    private void GoogleTranslate_Click(object sender, RoutedEventArgs e) => OpenGoogle("https://translate.google.com/?sl=auto&tl=en&text=");
    private void YouTube_Click(object sender, RoutedEventArgs e) => OpenGoogle("https://www.youtube.com/results?search_query=");

    private void GoogleSearch_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) OpenGoogle("https://www.google.com/search?q=");
    }

    private void OpenGoogle(string baseUrl)
    {
        var query = txtGoogleSearch.Text.Trim();
        if (string.IsNullOrEmpty(query))
        {
            var url = baseUrl.Contains("search?q=") ? "https://www.google.com" :
                      baseUrl.Contains("maps") ? "https://www.google.com/maps" :
                      baseUrl.Contains("translate") ? "https://translate.google.com" :
                      "https://www.youtube.com";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            return;
        }
        var encoded = Uri.EscapeDataString(query);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(baseUrl + encoded) { UseShellExecute = true });
        SetStatus($"🔍 ค้นหา \"{query}\"");
    }

    private void Accounts_Click(object sender, RoutedEventArgs e)
    {
        var win = new AccountsWindow() { Owner = this };
        win.ShowDialog();
    }

    private void Community_Click(object sender, RoutedEventArgs e)
    {
        var win = new CommunityWindow() { Owner = this };
        win.ShowDialog();
    }

    private void CloudSync_Click(object sender, RoutedEventArgs e)
    {
        var win = new CloudSyncWindow(() =>
        {
            _notes = NoteStore.Load();
            RefreshCategories();
            RefreshList();
        }) { Owner = this };
        win.ShowDialog();
    }

    private void QuickNote_Click(object sender, RoutedEventArgs e)
    {
        var win = new QuickNoteWindow(n =>
        {
            _notes.Add(n);
            NoteStore.Save(_notes);
            RefreshCategories();
            RefreshList();
            SetStatus($"⚡ Quick Note \"{n.Title}\" บันทึกแล้ว");
        });
        win.Show();
    }

    // ====================================================
    // Telegram
    // ====================================================
    private async void SendTelegram_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedIndex < 0) { MessageBox.Show("กรุณาเลือกโน้ต"); return; }
        var (token, chatId) = TelegramHelper.LoadConfig();
        if (string.IsNullOrEmpty(token))
        {
            MessageBox.Show("กรุณาตั้งค่า Telegram ก่อน\nกดปุ่ม ⚙️ TG", "✈️ Telegram"); return;
        }
        SetStatus("✈️ กำลังส่งไป Telegram...");
        var ok = await TelegramHelper.SendNote(token, chatId, _notes[_selectedIndex]);
        SetStatus(ok ? $"✈️ ส่ง \"{_notes[_selectedIndex].Title}\" ไป Telegram สำเร็จ!" : "❌ ส่งไม่สำเร็จ");
        if (ok) MessageBox.Show("ส่งสำเร็จ!", "✈️ Telegram");
        else MessageBox.Show("ส่งไม่สำเร็จ ตรวจสอบ Token/Chat ID", "❌ Telegram");
    }

    private void SetupTelegram_Click(object sender, RoutedEventArgs e)
    {
        var (curToken, curChat) = TelegramHelper.LoadConfig();
        var dlg = new InputDialog("✈️ ตั้งค่า Telegram",
            "Bot Token (จาก @BotFather):") { Owner = this };
        if (!string.IsNullOrEmpty(curToken)) dlg.SetInitialText(curToken);
        if (dlg.ShowDialog() != true) return;
        var token = dlg.Answer.Trim();

        var dlg2 = new InputDialog("✈️ Chat ID",
            "Chat ID (จาก @userinfobot หรือ @getidsbot):") { Owner = this };
        if (!string.IsNullOrEmpty(curChat)) dlg2.SetInitialText(curChat);
        if (dlg2.ShowDialog() != true) return;

        TelegramHelper.SaveConfig(token, dlg2.Answer.Trim());
        SetStatus("✅ บันทึกการตั้งค่า Telegram สำเร็จ");
        MessageBox.Show("ตั้งค่า Telegram สำเร็จ!\n\nวิธีขอ Token:\n1. คุยกับ @BotFather ใน Telegram\n2. พิมพ์ /newbot\n3. คัดลอก Token มาใส่\n\nวิธีขอ Chat ID:\n1. คุยกับ @userinfobot\n2. จะบอก Chat ID ให้", "✈️ Telegram");
    }

    // ====================================================
    // Spending Alert
    // ====================================================
    private void SpendingAlert_Click(object sender, RoutedEventArgs e)
    {
        var data = FinanceStore.Load();
        var alerts = SpendingAlert.Analyze(data);
        var msg = "🔔 การแจ้งเตือนการใช้จ่าย\n\n" + string.Join("\n\n", alerts);
        MessageBox.Show(msg, "🔔 Spending Alert");
        SetStatus($"🔔 มี {alerts.Count} การแจ้งเตือน");
    }

    // ====================================================
    // Helpers
    // ====================================================
    private void SetStatus(string text) => lblStatus.Text = text;
    private Brush FindBrush(string key) => (Brush)FindResource(key);
}
