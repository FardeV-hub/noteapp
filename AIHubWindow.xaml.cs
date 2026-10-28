using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NoteApp;

public partial class AIHubWindow : Window
{
    private AIConfig _config;
    private readonly List<Note> _notes;
    private readonly Action<Note> _addNote;

    private static readonly (string Key, string Name)[] Providers =
    {
        ("claude", "🟣 Claude (Anthropic)"),
        ("openai", "🟢 GPT (OpenAI)"),
        ("gemini", "🔵 Gemini (Google)"),
        ("huggingface", "🟡 Hugging Face"),
    };

    public AIHubWindow(List<Note> notes, Action<Note> addNote)
    {
        InitializeComponent();
        _notes = notes;
        _addNote = addNote;
        _config = AIConfigStore.Load();

        cmbProvider.ItemsSource = Providers.Select(p => p.Name).ToList();
        var idx = Array.FindIndex(Providers, p => p.Key == _config.DefaultProvider);
        cmbProvider.SelectedIndex = idx >= 0 ? idx : 0;

        txtClaudeKey.Text = _config.ClaudeKey;
        txtOpenAIKey.Text = _config.OpenAIKey;
        txtGeminiKey.Text = _config.GeminiKey;
        txtHFKey.Text = _config.HuggingFaceKey;

        BuildTabButtons();
        BuildTools();
        UpdateStatus();
        SwitchTab(0);
    }

    private int _currentTab;
    private readonly (string Icon, string Label)[] _tabs = { ("💬", "แชท"), ("🛠️", "เครื่องมือ"), ("🔑", "ตั้งค่า") };

    private void BuildTabButtons()
    {
        panelTabBtns.Children.Clear();
        for (int i = 0; i < _tabs.Length; i++)
        {
            var idx = i;
            var btn = new Button { Cursor = Cursors.Hand, BorderThickness = new Thickness(0), Margin = new Thickness(2) };
            var bd = new Border
            {
                CornerRadius = new CornerRadius(10), Padding = new Thickness(20, 8, 20, 8),
                Background = B(i == _currentTab ? "#6c63ff" : "Transparent")
            };
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new TextBlock { Text = _tabs[i].Icon, FontSize = 16, Margin = new Thickness(0, 0, 6, 0) });
            sp.Children.Add(new TextBlock { Text = _tabs[i].Label, FontSize = 13,
                FontWeight = i == _currentTab ? FontWeights.Bold : FontWeights.Normal,
                Foreground = B(i == _currentTab ? "White" : "#8b8fa7"), VerticalAlignment = VerticalAlignment.Center });
            bd.Child = sp;
            btn.Content = bd;
            btn.Template = new ControlTemplate(typeof(Button)) { VisualTree = new FrameworkElementFactory(typeof(ContentPresenter)) };
            btn.Click += (_, _) => SwitchTab(idx);
            panelTabBtns.Children.Add(btn);
        }
    }

    private void SwitchTab(int idx)
    {
        _currentTab = idx;
        pageChat.Visibility = idx == 0 ? Visibility.Visible : Visibility.Collapsed;
        pageTools.Visibility = idx == 1 ? Visibility.Visible : Visibility.Collapsed;
        pageSettings.Visibility = idx == 2 ? Visibility.Visible : Visibility.Collapsed;
        BuildTabButtons();
    }

    private void UpdateStatus()
    {
        var name = AIService.GetProviderName(_config.DefaultProvider);
        var hasKey = _config.DefaultProvider switch
        {
            "claude" => !string.IsNullOrEmpty(_config.ClaudeKey),
            "openai" => !string.IsNullOrEmpty(_config.OpenAIKey),
            "gemini" => !string.IsNullOrEmpty(_config.GeminiKey),
            "huggingface" => !string.IsNullOrEmpty(_config.HuggingFaceKey),
            _ => false
        };
        lblProvider.Text = $"AI ที่ใช้: {name}  {(hasKey ? "✅ พร้อมใช้งาน" : "❌ ยังไม่มี API Key")}";
    }

    private void Provider_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (cmbProvider.SelectedIndex < 0) return;
        _config.DefaultProvider = Providers[cmbProvider.SelectedIndex].Key;
        AIConfigStore.Save(_config);
        UpdateStatus();
    }

    private void SaveKeys_Click(object sender, RoutedEventArgs e)
    {
        _config.ClaudeKey = txtClaudeKey.Text.Trim();
        _config.OpenAIKey = txtOpenAIKey.Text.Trim();
        _config.GeminiKey = txtGeminiKey.Text.Trim();
        _config.HuggingFaceKey = txtHFKey.Text.Trim();
        AIConfigStore.Save(_config);
        UpdateStatus();
        MessageBox.Show("บันทึก API Keys สำเร็จ!", "🔑");
    }

    // ====================================================
    // Chat
    // ====================================================
    private void Chat_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            SendChat_Click(sender, e);
    }

    private async void SendChat_Click(object sender, RoutedEventArgs e)
    {
        var msg = txtChat.Text.Trim();
        if (string.IsNullOrEmpty(msg)) return;

        AddChatBubble("👤 คุณ", msg, "#2a2d45", "#e8e9f0");
        txtChat.Clear();
        AddChatBubble("🤖 AI", "⏳ กำลังคิด...", "#1a1a3e", "#8b8fa7");

        try
        {
            var response = await AIService.Ask(_config, msg);
            panelChat.Children.RemoveAt(panelChat.Children.Count - 1);
            AddChatBubble($"🤖 {AIService.GetProviderName(_config.DefaultProvider)}", response, "#1a1a3e", "#e8e9f0");
        }
        catch (Exception ex)
        {
            panelChat.Children.RemoveAt(panelChat.Children.Count - 1);
            AddChatBubble("❌ Error", ex.Message, "#2e1a1a", "#e74c3c");
        }
    }

    private void AddChatBubble(string sender, string text, string bg, string fg)
    {
        var isUser = sender.StartsWith("👤");
        var card = new Border
        {
            CornerRadius = new CornerRadius(isUser ? 14 : 4, isUser ? 4 : 14, 14, 14),
            Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(isUser ? 60 : 0, 6, isUser ? 0 : 60, 0),
            HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left
        };

        if (isUser)
        {
            var gradient = new LinearGradientBrush();
            gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#6c63ff"), 0));
            gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#8b5cf6"), 1));
            card.Background = gradient;
        }
        else
        {
            card.Background = B(bg);
        }

        var sp = new StackPanel();
        sp.Children.Add(new TextBlock { Text = sender, FontSize = 10,
            Foreground = B(isUser ? "#c4b5fd" : "#8b8fa7"), Margin = new Thickness(0, 0, 0, 3) });
        sp.Children.Add(new TextBlock { Text = text, FontSize = 13,
            Foreground = B(isUser ? "White" : fg), TextWrapping = TextWrapping.Wrap, LineHeight = 21 });
        card.Child = sp;
        panelChat.Children.Add(card);
    }

    // ====================================================
    // AI Tools
    // ====================================================
    private void BuildTools()
    {
        panelTools.Children.Clear();
        var tools = new (string Icon, string Name, string Desc, string Color, Action<object, RoutedEventArgs> Handler)[]
        {
            ("📝", "สรุปโน้ตทั้งหมด", "AI สรุปโน้ตทั้งหมดเป็นภาพรวม", "#6c63ff", SummarizeNotes),
            ("📂", "จัดหมวดโน้ตอัตโนมัติ", "AI วิเคราะห์และจัดหมวดหมู่โน้ตที่ยังไม่จัด", "#e84393", AutoCategorize),
            ("🌐", "แปลโน้ต", "แปลโน้ตที่เลือกเป็นภาษาอังกฤษ", "#0984e3", TranslateNote),
            ("💰", "วิเคราะห์การเงิน", "AI วิเคราะห์รายรับรายจ่ายแนะนำการออม", "#f39c12", AnalyzeFinance),
            ("📋", "สร้าง To-Do จากโน้ต", "แปลงโน้ตเป็น To-Do List อัตโนมัติ", "#27ae60", GenerateTodo),
            ("✍️", "เขียนโน้ตจาก AI", "บอก AI ว่าอยากจดอะไร แล้วสร้างโน้ตให้", "#e17055", GenerateNote),
            ("📧", "ร่าง Email จากโน้ต", "แปลงโน้ตเป็น Email ที่สุภาพ พร้อมส่ง", "#00b894", DraftEmail),
            ("🔍", "ถาม AI เกี่ยวกับโน้ต", "ถามคำถามเกี่ยวกับเนื้อหาในโน้ตทั้งหมด", "#8e44ad", AskAboutNotes),
        };

        foreach (var (icon, name, desc, color, handler) in tools)
        {
            var card = new Border
            {
                Background = B("#161828"), CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14), Margin = new Thickness(0, 0, 0, 8),
                BorderBrush = B(color), BorderThickness = new Thickness(2, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Icon
            var iconBorder = new Border { Background = B(color + "22"), CornerRadius = new CornerRadius(10),
                Width = 38, Height = 38, VerticalAlignment = VerticalAlignment.Center };
            iconBorder.Child = new TextBlock { Text = icon, FontSize = 20, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(iconBorder, 0);
            grid.Children.Add(iconBorder);

            // Text
            var sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
            sp.Children.Add(new TextBlock { Text = name, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = B("#e8e9f0") });
            sp.Children.Add(new TextBlock { Text = desc, FontSize = 11, Foreground = B("#8b8fa7") });
            Grid.SetColumn(sp, 1);
            grid.Children.Add(sp);

            // Button
            var btn = UI.Pill("▶ ใช้งาน", color, 12, 14, 6);
            btn.Click += new RoutedEventHandler(handler);
            btn.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(btn, 2);
            grid.Children.Add(btn);

            card.Child = grid;
            panelTools.Children.Add(card);
        }

        // Result area
        panelTools.Children.Add(new Border { Height = 1, Background = B("#2a2d45"), Margin = new Thickness(0, 8, 0, 8) });
        panelTools.Children.Add(new TextBlock { Text = "📋 ผลลัพธ์:", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = B("#8b8fa7"), Margin = new Thickness(0, 0, 0, 4) });
    }

    private void ShowResult(string title, string result)
    {
        while (panelTools.Children.Count > 10) panelTools.Children.RemoveAt(10);
        var card = new Border
        {
            Background = B("#161828"), CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14), Margin = new Thickness(0, 4, 0, 0)
        };
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock { Text = title, FontSize = 14, FontWeight = FontWeights.Bold, Foreground = B("#6c63ff"), Margin = new Thickness(0, 0, 0, 6) });
        sp.Children.Add(new TextBlock { Text = result, FontSize = 12, Foreground = B("#e8e9f0"), TextWrapping = TextWrapping.Wrap, LineHeight = 20 });
        card.Child = sp;
        panelTools.Children.Add(card);
    }

    // ====================================================
    // Tool Handlers
    // ====================================================
    private async void SummarizeNotes(object s, RoutedEventArgs e)
    {
        if (!_notes.Any()) { MessageBox.Show("ไม่มีโน้ต"); return; }
        ShowResult("⏳", "กำลังวิเคราะห์...");
        var noteList = string.Join("\n", _notes.Take(20).Select(n => $"- [{n.Category}] {n.Title}: {n.Content?.Take(100)}"));
        var result = await AIService.Ask(_config, $"สรุปภาพรวมโน้ตเหล่านี้เป็นภาษาไทย บอกว่ามีเรื่องอะไรบ้าง อะไรสำคัญที่สุด:\n{noteList}");
        ShowResult("📝 สรุปโน้ตทั้งหมด", result);
    }

    private async void AutoCategorize(object s, RoutedEventArgs e)
    {
        var uncategorized = _notes.Where(n => n.Category == "ทั่วไป").Take(10).ToList();
        if (!uncategorized.Any()) { MessageBox.Show("ไม่มีโน้ตที่ต้องจัดหมวด"); return; }
        ShowResult("⏳", "กำลังจัดหมวดหมู่...");
        var noteList = string.Join("\n", uncategorized.Select(n => $"- {n.Title}: {n.Content?.Take(80)}"));
        var result = await AIService.Ask(_config, $"จัดหมวดหมู่โน้ตเหล่านี้ เลือกจาก: งาน, สุขภาพ, การเงิน, การเรียน, ส่วนตัว, ไอเดีย, สำคัญ\nตอบเป็นรูปแบบ: ชื่อโน้ต → หมวดหมู่\n\n{noteList}");
        ShowResult("📂 แนะนำหมวดหมู่", result);
    }

    private async void TranslateNote(object s, RoutedEventArgs e)
    {
        var dlg = new InputDialog("🌐 แปลภาษา", "พิมพ์ข้อความที่ต้องการแปล:") { Owner = this };
        if (dlg.ShowDialog() != true || string.IsNullOrWhiteSpace(dlg.Answer)) return;
        ShowResult("⏳", "กำลังแปล...");
        var result = await AIService.Ask(_config, $"แปลข้อความนี้เป็นภาษาอังกฤษ:\n{dlg.Answer}");
        ShowResult("🌐 ผลการแปล", result);
    }

    private async void AnalyzeFinance(object s, RoutedEventArgs e)
    {
        var data = FinanceStore.Load();
        if (!data.Transactions.Any()) { MessageBox.Show("ไม่มีข้อมูลการเงิน"); return; }
        ShowResult("⏳", "กำลังวิเคราะห์...");
        var txList = string.Join("\n", data.Transactions.TakeLast(30).Select(t =>
            $"- {t.Date} {t.Type}: {t.Title} ฿{t.Amount:N0} [{t.Category}]"));
        var result = await AIService.Ask(_config,
            $"วิเคราะห์รายรับรายจ่ายนี้ สรุปภาพรวม แนะนำการออมเงิน:\n{txList}",
            "คุณเป็นที่ปรึกษาการเงินส่วนบุคคล ตอบเป็นภาษาไทย ให้คำแนะนำเชิงปฏิบัติ");
        ShowResult("💰 วิเคราะห์การเงิน", result);
    }

    private async void GenerateTodo(object s, RoutedEventArgs e)
    {
        var dlg = new InputDialog("📋 สร้าง To-Do", "บอก AI ว่าต้องทำอะไร:") { Owner = this };
        if (dlg.ShowDialog() != true || string.IsNullOrWhiteSpace(dlg.Answer)) return;
        ShowResult("⏳", "กำลังสร้าง...");
        var result = await AIService.Ask(_config, $"สร้าง To-Do List จากสิ่งนี้ ใส่ ☐ หน้าแต่ละรายการ:\n{dlg.Answer}");
        ShowResult("📋 To-Do List", result);

        if (MessageBox.Show("ต้องการสร้างเป็นโน้ตไหม?", "📋", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            _addNote(new Note { Title = $"To-Do: {dlg.Answer[..Math.Min(dlg.Answer.Length, 30)]}", Content = result, Category = "งาน" });
            MessageBox.Show("สร้างโน้ต To-Do สำเร็จ!", "✅");
        }
    }

    private async void GenerateNote(object s, RoutedEventArgs e)
    {
        var dlg = new InputDialog("✍️ AI เขียนโน้ต", "บอก AI ว่าอยากจดอะไร:") { Owner = this };
        if (dlg.ShowDialog() != true || string.IsNullOrWhiteSpace(dlg.Answer)) return;
        ShowResult("⏳", "กำลังเขียน...");
        var result = await AIService.Ask(_config, $"เขียนโน้ตจดบันทึกเรื่อง: {dlg.Answer}\nเขียนให้กระชับ ครบประเด็น จัดหัวข้อให้");
        ShowResult("✍️ โน้ตที่ AI เขียน", result);

        if (MessageBox.Show("ต้องการบันทึกเป็นโน้ตไหม?", "✍️", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            _addNote(new Note { Title = dlg.Answer[..Math.Min(dlg.Answer.Length, 40)], Content = result, Category = "ทั่วไป" });
            MessageBox.Show("บันทึกโน้ตสำเร็จ!", "✅");
        }
    }

    private async void DraftEmail(object s, RoutedEventArgs e)
    {
        var dlg = new InputDialog("📧 ร่าง Email", "บอกเนื้อหาที่ต้องการ (เช่น ลาป่วย, นัดประชุม):") { Owner = this };
        if (dlg.ShowDialog() != true || string.IsNullOrWhiteSpace(dlg.Answer)) return;
        ShowResult("⏳", "กำลังร่าง...");
        var result = await AIService.Ask(_config,
            $"ร่าง Email ภาษาไทย สุภาพ เป็นทางการ เรื่อง: {dlg.Answer}\nใส่ Subject, เนื้อหา, ลงท้าย",
            "คุณเป็นผู้เชี่ยวชาญเขียน Email ภาษาไทยที่สุภาพและเป็นทางการ");
        ShowResult("📧 Email ที่ร่างให้", result);
    }

    private async void AskAboutNotes(object s, RoutedEventArgs e)
    {
        if (!_notes.Any()) { MessageBox.Show("ไม่มีโน้ต"); return; }
        var dlg = new InputDialog("🔍 ถามเกี่ยวกับโน้ต", "ถามอะไรก็ได้เกี่ยวกับโน้ตของคุณ:") { Owner = this };
        if (dlg.ShowDialog() != true || string.IsNullOrWhiteSpace(dlg.Answer)) return;
        ShowResult("⏳", "กำลังค้นหา...");
        var context = string.Join("\n", _notes.Take(20).Select(n =>
            $"[{n.Category}|{n.Priority}] {n.Title}: {n.Content?.Take(120)}"));
        var result = await AIService.Ask(_config,
            $"จากโน้ตเหล่านี้:\n{context}\n\nตอบคำถาม: {dlg.Answer}");
        ShowResult("🔍 คำตอบ", result);
    }

    // ====================================================
    private static Button MakeBtn(string text, string bg) => new()
    {
        Content = text, Padding = new Thickness(12, 6, 12, 6), FontSize = 12,
        Background = B(bg), Foreground = Brushes.White, BorderThickness = new Thickness(0),
        Cursor = Cursors.Hand, VerticalAlignment = VerticalAlignment.Center
    };
    private static SolidColorBrush B(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
}
