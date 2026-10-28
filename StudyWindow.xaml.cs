using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NoteApp;

public partial class StudyWindow : Window
{
    private StudyData _data;
    private readonly Action<Note> _addNote;
    private DispatcherTimer? _pomTimer;
    private int _pomSeconds;
    private bool _pomRunning;
    private bool _pomBreak;

    public StudyWindow(Action<Note> addNote)
    {
        InitializeComponent();
        _data = StudyStore.Load();
        _addNote = addNote;
        Render();
        BuildAITools();
        BuildPomodoro();
    }

    private void Render()
    {
        var pending = _data.Assignments.Count(a => a.Status != "done");
        var overdue = _data.Assignments.Count(a => a.IsOverdue);
        lblStudyInfo.Text = $"{_data.Subjects.Count} วิชา  ·  {pending} งานค้าง" + (overdue > 0 ? $"  ·  {overdue} เลย deadline" : "");
        RenderAssignments();
        RenderGrades();
        RenderSchedule();
    }

    // ====================================================
    // AI ช่วยเรียน
    // ====================================================
    private void BuildAITools()
    {
        panelAIStudy.Children.Clear();
        var tools = new (string Icon, string Name, string Desc, string Color, string Prompt)[]
        {
            ("", "สรุปบทเรียน", "วางเนื้อหา แล้ว AI สรุปให้กระชับ", "#2b6cb0",
                "สรุปเนื้อหาบทเรียนนี้ให้กระชับ เข้าใจง่าย จัดหัวข้อ bullet points:\n\n"),
            ("", "สร้างแฟลชการ์ด", "AI สร้างคำถาม-คำตอบจากเนื้อหา", "#6c63ff",
                "สร้างแฟลชการ์ด 10 ข้อจากเนื้อหานี้ รูปแบบ:\nQ: คำถาม\nA: คำตอบ\n\nเนื้อหา:\n"),
            ("", "ออกข้อสอบจำลอง", "AI ออกข้อสอบ ปรนัย+อัตนัย", "#e84393",
                "ออกข้อสอบจากเนื้อหานี้:\n- ปรนัย 5 ข้อ (4 ตัวเลือก)\n- อัตนัย 3 ข้อ\nพร้อมเฉลยท้ายสุด\n\nเนื้อหา:\n"),
            ("", "อธิบายเพิ่ม", "ไม่เข้าใจอะไร AI อธิบายง่ายๆ", "#27ae60",
                "อธิบายเรื่องนี้ให้เข้าใจง่ายที่สุด ยกตัวอย่างประกอบ เหมือนอธิบายให้เด็ก ม.ต้น ฟัง:\n\n"),
            ("", "แปล + อธิบายแกรมมาร์", "แปลภาษาอังกฤษ + อธิบายไวยากรณ์", "#0984e3",
                "แปลข้อความนี้เป็นไทย แล้วอธิบายไวยากรณ์ภาษาอังกฤษที่ใช้:\n\n"),
            ("", "ช่วยเขียน Abstract", "AI ร่าง Abstract งานวิจัย", "#8e44ad",
                "เขียน Abstract ภาษาอังกฤษสำหรับงานวิจัยเรื่อง (ระบุด้านล่าง) ประมาณ 200-250 คำ มี: Background, Objective, Methods, Results, Conclusion:\n\n"),
            ("", "สร้าง Citation APA", "สร้าง Reference รูปแบบ APA 7th", "#636e72",
                "สร้าง Citation รูปแบบ APA 7th Edition จากข้อมูลนี้ (ผู้แต่ง, ปี, ชื่อ, แหล่งที่มา):\n\n"),
            ("", "สรุป Literature Review", "AI สรุปบทความวิจัยที่อ่าน", "#00b894",
                "สรุป Literature Review จากบทความนี้ ระบุ: วัตถุประสงค์, วิธีวิจัย, ผลลัพธ์, ข้อจำกัด, ความเกี่ยวข้องกับงานวิจัยของเรา:\n\n"),
        };

        foreach (var (icon, name, desc, color, prompt) in tools)
        {
            var card = new Border { Background = B("#161828"), CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 6),
                BorderBrush = B(color), BorderThickness = new Thickness(2, 0, 0, 0) };
            var dp = new DockPanel();
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = name, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = B("#d8dae2") });
            sp.Children.Add(new TextBlock { Text = desc, FontSize = 11, Foreground = B("#7d8298"), Margin = new Thickness(0, 2, 0, 0) });
            dp.Children.Add(sp);

            var btn = MakeBtn("ใช้งาน", color);
            var p = prompt;
            btn.Click += async (_, _) =>
            {
                var dlg = new InputDialog(name, "วางเนื้อหาหรือพิมพ์คำถาม:") { Owner = this };
                if (dlg.ShowDialog() != true || string.IsNullOrWhiteSpace(dlg.Answer)) return;

                var config = AIConfigStore.Load();
                var fullPrompt = p + dlg.Answer;
                var result = await AIService.Ask(config, fullPrompt, "คุณเป็นครูสอนพิเศษที่เก่งมาก ตอบเป็นภาษาไทย");

                if (MessageBox.Show($"ต้องการบันทึกผลลัพธ์เป็นโน้ตไหม?", name, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    _addNote(new Note { Title = $"[{name}] {dlg.Answer[..Math.Min(dlg.Answer.Length, 30)]}", Content = result, Category = "การเรียน" });

                MessageBox.Show(result.Length > 500 ? result[..500] + "...\n\n(บันทึกเป็นโน้ตเพื่อดูเต็ม)" : result, name);
            };
            DockPanel.SetDock(btn, Dock.Right);
            dp.Children.Insert(0, btn);
            card.Child = dp;
            panelAIStudy.Children.Add(card);
        }
    }

    // ====================================================
    // การบ้าน/งาน
    // ====================================================
    private void RenderAssignments()
    {
        panelAssignments.Children.Clear();

        // Add form
        var formCard = new Border { Background = B("#161828"), CornerRadius = new CornerRadius(8), Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 10) };
        var fSp = new StackPanel();
        fSp.Children.Add(Txt("เพิ่มการบ้าน / งาน", 14, "#d8dae2", true));
        var fRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
        var txtHw = new TextBox { Width = 200, FontSize = 12, Padding = new Thickness(6), Background = B("#2a2d45"), Foreground = B("#e8e9f0"), BorderBrush = B("#2a2d45") };
        fRow.Children.Add(txtHw);
        var cmbSub = new ComboBox { Width = 120, FontSize = 12, Padding = new Thickness(4), Margin = new Thickness(6, 0, 0, 0) };
        cmbSub.ItemsSource = _data.Subjects.Select(s => s.Name).ToList();
        if (cmbSub.Items.Count > 0) cmbSub.SelectedIndex = 0;
        fRow.Children.Add(cmbSub);
        var dpDue = new DatePicker { FontSize = 11, Width = 120, Margin = new Thickness(6, 0, 0, 0) };
        dpDue.SelectedDate = DateTime.Today.AddDays(7);
        fRow.Children.Add(dpDue);
        var addBtn = MakeBtn("+ เพิ่ม", "#27ae60");
        addBtn.Margin = new Thickness(6, 0, 0, 0);
        addBtn.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(txtHw.Text)) return;
            var subj = cmbSub.SelectedIndex >= 0 ? _data.Subjects[cmbSub.SelectedIndex] : null;
            _data.Assignments.Add(new Assignment { Title = txtHw.Text.Trim(), SubjectId = subj?.Id ?? "",
                DueDate = dpDue.SelectedDate?.ToString("yyyy-MM-dd") ?? "" });
            StudyStore.Save(_data); txtHw.Text = ""; Render();
        };
        fRow.Children.Add(addBtn);
        fSp.Children.Add(fRow);
        formCard.Child = fSp;
        panelAssignments.Children.Add(formCard);

        // List
        var pending = _data.Assignments.Where(a => a.Status != "done").OrderBy(a => a.DueDate).ToList();
        var done = _data.Assignments.Where(a => a.Status == "done").ToList();

        if (pending.Any()) panelAssignments.Children.Add(Txt($"ยังไม่เสร็จ ({pending.Count})", 14, "#f5a623", true));
        foreach (var a in pending) AddAssignmentCard(a);
        if (done.Any()) panelAssignments.Children.Add(Txt($"\nเสร็จแล้ว ({done.Count})", 14, "#30a46c", true));
        foreach (var a in done.TakeLast(10)) AddAssignmentCard(a);
    }

    private void AddAssignmentCard(Assignment a)
    {
        var subj = _data.Subjects.FirstOrDefault(s => s.Id == a.SubjectId);
        var color = a.IsOverdue ? "#e74c3c" : a.Status == "done" ? "#27ae60" : "#f39c12";
        var card = new Border { Background = B("#161828"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(0, 3, 0, 0), BorderBrush = B(color), BorderThickness = new Thickness(2, 0, 0, 0) };
        var dp = new DockPanel();
        dp.Children.Add(Txt($"{a.StatusIcon} {a.Title}", 13, "#e8e9f0"));
        var meta = new StackPanel { Orientation = Orientation.Horizontal };
        if (subj != null) meta.Children.Add(Badge(subj.Name));
        meta.Children.Add(Badge($"📅 {a.DueDate}"));
        if (a.IsOverdue) meta.Children.Add(Badge("🔴 เลย!"));
        var aRef = a;
        if (a.Status != "done")
        {
            var doneBtn = MakeBtn("✅", "#27ae60");
            doneBtn.Click += (_, _) => { aRef.Status = "done"; StudyStore.Save(_data); Render(); };
            DockPanel.SetDock(doneBtn, Dock.Right);
            dp.Children.Insert(0, doneBtn);
        }
        var sp = new StackPanel(); sp.Children.Add(dp); sp.Children.Add(meta);
        card.Child = sp;
        panelAssignments.Children.Add(card);
    }

    // ====================================================
    // เกรด/GPA
    // ====================================================
    private void RenderGrades()
    {
        panelGrades.Children.Clear();
        panelGrades.Children.Add(Txt("เกรดและ GPA", 16, "#d8dae2", true));

        if (!_data.Subjects.Any()) { panelGrades.Children.Add(Txt("กรุณาเพิ่มวิชาก่อน", 13, "#8b8fa7")); return; }

        var gpaList = new List<(double grade, int credits)>();

        foreach (var subj in _data.Subjects)
        {
            var grades = _data.Grades.Where(g => g.SubjectId == subj.Id).ToList();
            var avg = grades.Any() ? grades.Average(g => g.Score / g.MaxScore * 100) : -1;
            var gpa = avg >= 0 ? GPACalculator.ScoreToGrade(avg) : -1;

            if (gpa >= 0) gpaList.Add((gpa, subj.Credits));

            var card = new Border { Background = B("#161828"), CornerRadius = new CornerRadius(8), Padding = new Thickness(12), Margin = new Thickness(0, 4, 0, 0) };
            var sp = new StackPanel();
            var topRow = new DockPanel();
            topRow.Children.Add(Txt($"{subj.Name} ({subj.Credits} หน่วยกิต)", 13, "#d8dae2", true));
            if (gpa >= 0)
            {
                var letter = GPACalculator.GradeToLetter(gpa);
                var gradeColor = gpa >= 3.0 ? "#27ae60" : gpa >= 2.0 ? "#f39c12" : "#e74c3c";
                topRow.Children.Add(Txt($"เกรด {letter} ({gpa:F1})", 13, gradeColor, true));
            }
            sp.Children.Add(topRow);

            foreach (var g in grades)
                sp.Children.Add(Txt($"   {g.ExamName}: {g.Score}/{g.MaxScore} ({g.Score / g.MaxScore * 100:F0}%)", 11, "#8b8fa7"));

            // Add grade button
            var addBtn = MakeBtn("+ เพิ่มคะแนน", "#2b6cb0");
            var subjRef = subj;
            addBtn.Click += (_, _) =>
            {
                var dlg = new InputDialog("เพิ่มคะแนน", $"ชื่อสอบ (เช่น Midterm, Final):") { Owner = this };
                if (dlg.ShowDialog() != true) return;
                var name = dlg.Answer;
                var dlg2 = new InputDialog("คะแนน", "คะแนนที่ได้ / คะแนนเต็ม\nเช่น 85/100:") { Owner = this };
                if (dlg2.ShowDialog() != true) return;
                var parts = dlg2.Answer.Split('/');
                if (parts.Length == 2 && double.TryParse(parts[0].Trim(), out var score) && double.TryParse(parts[1].Trim(), out var max))
                {
                    _data.Grades.Add(new Grade { SubjectId = subjRef.Id, ExamName = name, Score = score, MaxScore = max, Date = DateTime.Today.ToString("yyyy-MM-dd") });
                    StudyStore.Save(_data); Render();
                }
            };
            addBtn.Margin = new Thickness(0, 4, 0, 0);
            sp.Children.Add(addBtn);
            card.Child = sp;
            panelGrades.Children.Add(card);
        }

        // GPA Summary
        if (gpaList.Any())
        {
            var gpa = GPACalculator.CalcGPA(gpaList);
            var gpaColor = gpa >= 3.0 ? "#27ae60" : gpa >= 2.0 ? "#f39c12" : "#e74c3c";
            var summary = new Border { Background = B("#161828"), CornerRadius = new CornerRadius(8), Padding = new Thickness(14),
                Margin = new Thickness(0, 12, 0, 0), BorderBrush = B(gpaColor), BorderThickness = new Thickness(0, 0, 0, 3) };
            var sSp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            sSp.Children.Add(Txt("GPA รวม", 14, "#7d8298"));
            sSp.Children.Add(Txt($"{gpa:F2}", 36, gpaColor, true));
            sSp.Children.Add(Txt($"จาก {gpaList.Count} วิชา / {gpaList.Sum(g => g.credits)} หน่วยกิต", 11, "#8b8fa7"));
            summary.Child = sSp;
            panelGrades.Children.Insert(1, summary);
        }
    }

    // ====================================================
    // ตารางเรียน
    // ====================================================
    private void RenderSchedule()
    {
        panelSchedule.Children.Clear();
        panelSchedule.Children.Add(Txt("ตารางเรียน", 16, "#d8dae2", true));

        var addBtn = MakeBtn("+ เพิ่มคาบเรียน", "#2b6cb0");
        addBtn.Click += (_, _) => AddScheduleSlot();
        addBtn.Margin = new Thickness(0, 0, 0, 10);
        panelSchedule.Children.Add(addBtn);

        var days = new[] { "จันทร์", "อังคาร", "พุธ", "พฤหัสบดี", "ศุกร์", "เสาร์", "อาทิตย์" };
        foreach (var day in days)
        {
            var slots = _data.Schedule.Where(s => s.Day == day).OrderBy(s => s.StartTime).ToList();
            if (!slots.Any()) continue;

            panelSchedule.Children.Add(Txt(day, 13, "#7d8298", true));
            foreach (var slot in slots)
            {
                var subj = _data.Subjects.FirstOrDefault(s => s.Id == slot.SubjectId);
                var card = new Border { Background = B("#161828"), CornerRadius = new CornerRadius(6), Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(0, 2, 0, 0) };
                card.Child = Txt($"   {slot.StartTime} – {slot.EndTime}    {subj?.Name ?? "?"}{(string.IsNullOrEmpty(slot.Room) ? "" : $"    {slot.Room}")}", 12, "#d8dae2");
                panelSchedule.Children.Add(card);
            }
        }
    }

    private void AddScheduleSlot()
    {
        if (!_data.Subjects.Any()) { MessageBox.Show("กรุณาเพิ่มวิชาก่อน"); return; }
        var dlg = new InputDialog("เพิ่มคาบเรียน", "วัน (จันทร์-อาทิตย์):") { Owner = this };
        if (dlg.ShowDialog() != true) return;
        var day = dlg.Answer.Trim();
        var dlg2 = new InputDialog("เวลา", "เวลาเริ่ม-จบ เช่น 09:00-12:00:") { Owner = this };
        if (dlg2.ShowDialog() != true) return;
        var times = dlg2.Answer.Split('-');
        if (times.Length != 2) return;
        _data.Schedule.Add(new ScheduleSlot { Day = day, StartTime = times[0].Trim(), EndTime = times[1].Trim(),
            SubjectId = _data.Subjects.First().Id });
        StudyStore.Save(_data); Render();
    }

    // ====================================================
    // Pomodoro Timer (Modern)
    // ====================================================
    private TextBlock? _pomTimerText;
    private TextBlock? _pomStatusText;
    private Border? _pomProgressBorder;
    private int _pomTotalSeconds;
    private readonly int[] _studyPresets = { 15, 25, 30, 45, 60, 90, 120 };
    private readonly int[] _breakPresets = { 5, 10, 15, 20 };
    private int _selectedStudy = 25;
    private int _selectedBreak = 5;

    private void BuildPomodoro()
    {
        panelPomodoro.Children.Clear();

        // Card container
        var card = new Border { Background = B("#161828"), CornerRadius = new CornerRadius(16),
            Padding = new Thickness(30, 24, 30, 24), MinWidth = 400 };
        var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };

        sp.Children.Add(new TextBlock { Text = "Focus Timer", FontSize = 22, FontWeight = FontWeights.Bold,
            Foreground = B("#d8dae2"), HorizontalAlignment = HorizontalAlignment.Center });

        _pomStatusText = new TextBlock { Text = _pomRunning ? (_pomBreak ? "พักผ่อน" : "กำลังโฟกัส...") : "พร้อมเริ่ม",
            FontSize = 13, Foreground = B(_pomRunning ? (_pomBreak ? "#30a46c" : "#e5484d") : "#7d8298"),
            HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 14) };
        sp.Children.Add(_pomStatusText);

        // Timer display
        var timerCard = new Border { Background = B("#0f1018"), CornerRadius = new CornerRadius(12),
            Padding = new Thickness(20, 10, 20, 10), Margin = new Thickness(0, 0, 0, 8),
            HorizontalAlignment = HorizontalAlignment.Center };
        _pomTimerText = new TextBlock
        {
            Text = _pomRunning ? $"{_pomSeconds / 60:D2}:{_pomSeconds % 60:D2}" : $"{_selectedStudy:D2}:00",
            FontSize = 52, FontWeight = FontWeights.Bold,
            Foreground = B(_pomBreak ? "#27ae60" : "#e8e9f0"),
            HorizontalAlignment = HorizontalAlignment.Center,
            FontFamily = new FontFamily("Consolas")
        };
        timerCard.Child = _pomTimerText;
        sp.Children.Add(timerCard);

        // Progress bar
        _pomProgressBorder = new Border { Background = B("#2a2d45"), CornerRadius = new CornerRadius(4),
            Height = 6, Margin = new Thickness(0, 0, 0, 14) };
        var progressFill = new Border { Background = B(_pomBreak ? "#27ae60" : "#e74c3c"),
            CornerRadius = new CornerRadius(4), Height = 6, HorizontalAlignment = HorizontalAlignment.Left,
            Width = _pomRunning && _pomTotalSeconds > 0 ? (1 - (double)_pomSeconds / _pomTotalSeconds) * 340 : 0 };
        var progressGrid = new Grid { Width = 340 };
        progressGrid.Children.Add(_pomProgressBorder);
        progressGrid.Children.Add(progressFill);
        sp.Children.Add(progressGrid);

        // Time selection (only when not running)
        if (!_pomRunning)
        {
            sp.Children.Add(Txt("เวลาอ่าน (นาที)", 12, "#7d8298"));
            var studyRow = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 10) };
            foreach (var mins in _studyPresets)
            {
                var btn = TimePillBtn($"{mins}", mins == _selectedStudy, "#e74c3c");
                var m = mins;
                btn.Click += (_, _) => { _selectedStudy = m; BuildPomodoro(); };
                studyRow.Children.Add(btn);
            }
            sp.Children.Add(studyRow);

            sp.Children.Add(Txt("เวลาพัก (นาที)", 12, "#7d8298"));
            var breakRow = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 14) };
            foreach (var mins in _breakPresets)
            {
                var btn = TimePillBtn($"{mins}", mins == _selectedBreak, "#27ae60");
                var m = mins;
                btn.Click += (_, _) => { _selectedBreak = m; BuildPomodoro(); };
                breakRow.Children.Add(btn);
            }
            sp.Children.Add(breakRow);
        }

        // Action buttons
        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) };

        if (!_pomRunning)
        {
            var startBtn = PillBtn("เริ่มโฟกัส", "#e5484d", 15, 28, 12);
            startBtn.Click += (_, _) => StartPomodoro();
            btnRow.Children.Add(startBtn);
        }
        else
        {
            var pauseBtn = PillBtn("หยุดชั่วคราว", "#f5a623", 14, 22, 10);
            pauseBtn.Click += (_, _) => StopPomodoro();
            btnRow.Children.Add(pauseBtn);

            var resetBtn = PillBtn("รีเซ็ต", "#e5484d", 14, 22, 10);
            resetBtn.Click += (_, _) => { _pomBreak = false; StopPomodoro(); };
            btnRow.Children.Add(resetBtn);
        }
        sp.Children.Add(btnRow);

        // Stats
        var statsRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 16, 0, 0) };
        statsRow.Children.Add(StatBadge("", _data.PomodoroTotal.ToString(), "รอบสำเร็จ"));
        statsRow.Children.Add(StatBadge("", $"{_data.PomodoroTotal * _selectedStudy}", "นาทีรวม"));
        statsRow.Children.Add(StatBadge("", $"{_data.PomodoroTotal / 4}", "ชั่วโมง"));
        sp.Children.Add(statsRow);

        card.Child = sp;
        panelPomodoro.Children.Add(card);
    }

    private static Border StatBadge(string icon, string value, string label)
    {
        var bd = new Border { Background = B("#1c1f2a"), CornerRadius = new CornerRadius(10),
            Padding = new Thickness(18, 10, 18, 10), Margin = new Thickness(4, 0, 4, 0) };
        var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        sp.Children.Add(new TextBlock { Text = value, FontSize = 22, FontWeight = FontWeights.Bold,
            Foreground = B("#d8dae2"), HorizontalAlignment = HorizontalAlignment.Center });
        sp.Children.Add(new TextBlock { Text = label, FontSize = 10, Foreground = B("#7d8298"),
            HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 0) });
        bd.Child = sp;
        return bd;
    }

    private static Button PillBtn(string text, string bg, double fontSize = 13, double padX = 16, double padY = 8)
    {
        var btn = new Button { Cursor = Cursors.Hand, BorderThickness = new Thickness(0), Margin = new Thickness(2) };
        var border = new Border
        {
            Background = B(bg), CornerRadius = new CornerRadius(20),
            Padding = new Thickness(padX, padY, padX, padY)
        };
        border.Child = new TextBlock
        {
            Text = text, FontSize = fontSize, FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center
        };
        btn.Content = border;
        btn.Template = TransparentBtnTemplate();
        return btn;
    }

    private static Button TimePillBtn(string text, bool selected, string activeColor)
    {
        var bg = selected ? activeColor : "#2a2d45";
        var btn = new Button { Cursor = Cursors.Hand, BorderThickness = new Thickness(0), Margin = new Thickness(3, 2, 3, 2) };
        var border = new Border
        {
            Background = B(bg), CornerRadius = new CornerRadius(16),
            Padding = new Thickness(14, 6, 14, 6), MinWidth = 36
        };
        border.Child = new TextBlock
        {
            Text = text, FontSize = 14, FontWeight = selected ? FontWeights.Bold : FontWeights.Normal,
            Foreground = B(selected ? "White" : "#8b8fa7"),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        btn.Content = border;
        btn.Template = TransparentBtnTemplate();
        return btn;
    }

    private static ControlTemplate TransparentBtnTemplate()
    {
        var t = new ControlTemplate(typeof(Button));
        var cp = new FrameworkElementFactory(typeof(ContentPresenter));
        t.VisualTree = cp;
        return t;
    }

    private void StartPomodoro()
    {
        _pomTotalSeconds = _pomBreak ? _selectedBreak * 60 : _selectedStudy * 60;
        _pomSeconds = _pomTotalSeconds;
        _pomRunning = true;
        BuildPomodoro();

        _pomTimer?.Stop();
        _pomTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _pomTimer.Tick += (_, _) =>
        {
            if (_pomSeconds <= 0)
            {
                _pomTimer.Stop();
                _pomRunning = false;
                if (!_pomBreak) { _data.PomodoroTotal++; StudyStore.Save(_data); }
                _pomBreak = !_pomBreak;
                try { System.Media.SystemSounds.Exclamation.Play(); } catch { }
                MessageBox.Show(
                    _pomBreak ? $"🍅 จบ 1 รอบ! ({_selectedStudy} นาที)\nพักผ่อนสัก {_selectedBreak} นาที"
                    : "☕ พักเสร็จแล้ว! เริ่มรอบใหม่ 💪", "⏱️ Focus Timer");
                BuildPomodoro();
                return;
            }
            _pomSeconds--;
            if (_pomTimerText != null)
                _pomTimerText.Text = $"{_pomSeconds / 60:D2}:{_pomSeconds % 60:D2}";
        };
        _pomTimer.Start();
    }

    private void StopPomodoro()
    {
        _pomTimer?.Stop();
        _pomRunning = false;
        BuildPomodoro();
    }

    // ====================================================
    // เพิ่มวิชา
    // ====================================================
    private void AddSubject_Click(object s, RoutedEventArgs e)
    {
        var dlg = new InputDialog("เพิ่มวิชา", "ชื่อวิชา:") { Owner = this };
        if (dlg.ShowDialog() != true || string.IsNullOrWhiteSpace(dlg.Answer)) return;
        var colors = new[] { "#2b6cb0", "#e84393", "#00b894", "#f39c12", "#6c63ff", "#e17055" };
        _data.Subjects.Add(new Subject { Name = dlg.Answer.Trim(), Color = colors[_data.Subjects.Count % colors.Length] });
        StudyStore.Save(_data); Render();
    }

    // ====================================================
    private static TextBlock Txt(string t, double sz, string c, bool b = false) => new()
    { Text = t, FontSize = sz, Foreground = B(c), FontWeight = b ? FontWeights.Bold : FontWeights.Normal, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) };
    private static Border Badge(string t) => new() { Background = B("#2a2d45"), CornerRadius = new CornerRadius(5), Padding = new Thickness(6, 1, 6, 1), Margin = new Thickness(0, 0, 6, 0), Child = new TextBlock { Text = t, FontSize = 10, Foreground = B("#8b8fa7") } };
    private static Button MakeBtn(string t, string bg) => new() { Content = t, Padding = new Thickness(10, 5, 10, 5), FontSize = 11, Background = B(bg), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand };
    private static SolidColorBrush B(string h) => new((Color)ColorConverter.ConvertFromString(h));
}
