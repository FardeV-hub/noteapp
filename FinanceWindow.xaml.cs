using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NoteApp;

public partial class FinanceWindow : Window
{
    private FinanceData _data;
    private SMSReceiverServer? _smsServer;
    private static readonly string[] ExpenseCats = { "อาหาร", "เดินทาง", "ที่พัก", "ช้อปปิ้ง", "สาธารณูปโภค", "สุขภาพ", "การศึกษา", "บันเทิง", "อื่นๆ" };
    private static readonly string[] IncomeCats = { "เงินเดือน", "ฟรีแลนซ์", "ลงทุน", "โบนัส", "ขายของ", "อื่นๆ" };
    private static readonly string[] BillCats = { "ค่าเช่า", "ค่าน้ำ", "ค่าไฟ", "ค่าเน็ต", "ค่าโทรศัพท์", "ประกัน", "ผ่อนรถ", "ผ่อนบ้าน", "บัตรเครดิต", "อื่นๆ" };
    private static readonly (string Name, string Icon, string Color)[] Banks =
    {
        ("กสิกรไทย (KBank)", "💚", "#00a651"),
        ("ไทยพาณิชย์ (SCB)", "💜", "#4e2a84"),
        ("กรุงเทพ (BBL)", "💙", "#0033a0"),
        ("กรุงไทย (KTB)", "🔵", "#0072bc"),
        ("ทหารไทยธนชาต (ttb)", "🧡", "#f47920"),
        ("กรุงศรี (BAY)", "💛", "#ffc72c"),
        ("ออมสิน (GSB)", "💗", "#e91e8c"),
        ("PromptPay", "📱", "#003b71"),
        ("อื่นๆ", "🏦", "#636e72"),
    };

    public FinanceWindow()
    {
        InitializeComponent();
        _data = FinanceStore.Load();
        cmbTxCategory.ItemsSource = ExpenseCats;
        cmbTxCategory.SelectedIndex = 0;
        cmbBillCat.ItemsSource = BillCats;
        cmbBillCat.SelectedIndex = 0;
        dpTxDate.SelectedDate = DateTime.Today;
        dpBillDue.SelectedDate = DateTime.Today.AddDays(30);
        Refresh();
        _ = LoadMarketData();
    }

    private void Refresh()
    {
        RenderOverview();
        RenderAnalysis();
        RenderAIInvestment();
        RenderTransactions();
        RenderBills();
        RenderBanks();
    }

    // ====================================================
    // Tab 1: สรุปภาพรวม
    // ====================================================
    private void RenderOverview()
    {
        panelOverview.Children.Clear();
        var thisMonth = DateTime.Today.ToString("yyyy-MM");
        var monthTx = _data.Transactions.Where(t => t.Date.StartsWith(thisMonth)).ToList();
        var income = monthTx.Where(t => t.Type == "income").Sum(t => t.Amount);
        var expense = monthTx.Where(t => t.Type == "expense").Sum(t => t.Amount);
        var balance = income - expense;
        var totalBalance = _data.Accounts.Sum(a => a.Balance);
        var unpaidBills = _data.Bills.Where(b => !b.Paid).ToList();
        var overdueBills = unpaidBills.Where(b => b.IsOverdue).ToList();

        // KPI Cards
        var kpiGrid = new Grid();
        for (int i = 0; i < 5; i++)
        {
            if (i > 0) kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        AddKPI(kpiGrid, 0, "💵", $"{income:N0}", "รายรับเดือนนี้", "#27ae60");
        AddKPI(kpiGrid, 2, "💸", $"{expense:N0}", "รายจ่ายเดือนนี้", "#e74c3c");
        AddKPI(kpiGrid, 4, "💰", $"{balance:N0}", "คงเหลือเดือนนี้", balance >= 0 ? "#27ae60" : "#e74c3c");
        AddKPI(kpiGrid, 6, "🏦", $"{totalBalance:N0}", "ยอดรวมธนาคาร", "#0984e3");
        AddKPI(kpiGrid, 8, "📄", unpaidBills.Count.ToString(), $"บิลค้าง{(overdueBills.Any() ? $" (🔴{overdueBills.Count})" : "")}", overdueBills.Any() ? "#e74c3c" : "#f39c12");
        panelOverview.Children.Add(kpiGrid);

        // Expense by category chart
        if (monthTx.Any(t => t.Type == "expense"))
        {
            var card = MakeCard("📊 รายจ่ายเดือนนี้ แยกหมวด");
            var sp = (StackPanel)card.Child;
            var groups = monthTx.Where(t => t.Type == "expense").GroupBy(t => t.Category)
                .OrderByDescending(g => g.Sum(t => t.Amount)).ToList();
            var maxAmt = groups.First().Sum(t => t.Amount);
            var colors = new[] { "#e74c3c", "#f39c12", "#e84393", "#6c63ff", "#0984e3", "#00b894", "#fdcb6e" };

            foreach (var (g, idx) in groups.Select((g, i) => (g, i)))
            {
                var amt = g.Sum(t => t.Amount);
                var pct = expense > 0 ? amt / expense * 100 : 0;
                var color = colors[idx % colors.Length];

                var row = new Grid { Margin = new Thickness(0, 4, 0, 0) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

                row.Children.Add(Txt(g.Key, 12, "#e8e9f0"));
                var barGrid = new Grid();
                barGrid.Children.Add(new Border { Background = B("#2a2d45"), CornerRadius = new CornerRadius(4), Height = 20 });
                barGrid.Children.Add(new Border { Background = B(color), CornerRadius = new CornerRadius(4), Height = 20,
                    HorizontalAlignment = HorizontalAlignment.Left, Width = Math.Max(amt / maxAmt * 250, 4) });
                Grid.SetColumn(barGrid, 1);
                row.Children.Add(barGrid);
                var amtTxt = Txt($"฿{amt:N0} ({pct:F0}%)", 11, color);
                amtTxt.HorizontalAlignment = HorizontalAlignment.Right;
                Grid.SetColumn(amtTxt, 2);
                row.Children.Add(amtTxt);

                sp.Children.Add(row);
            }
            card.Margin = new Thickness(0, 12, 0, 0);
            panelOverview.Children.Add(card);
        }

        // Upcoming bills
        if (unpaidBills.Any())
        {
            var billCard = MakeCard($"📄 บิลที่ต้องจ่าย ({unpaidBills.Count})");
            var sp = (StackPanel)billCard.Child;
            foreach (var b in unpaidBills.OrderBy(b => b.DueDate).Take(5))
            {
                var row = new Border { Background = B("#2a2d45"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 3, 0, 0) };
                var dp = new DockPanel();
                dp.Children.Add(Txt($"{b.StatusIcon} {b.Title}", 12, "#e8e9f0"));
                var right = Txt($"฿{b.Amount:N0}  📅{b.DueDate}", 11, b.IsOverdue ? "#e74c3c" : "#f39c12");
                right.HorizontalAlignment = HorizontalAlignment.Right;
                DockPanel.SetDock(right, Dock.Right);
                dp.Children.Insert(0, right);
                row.Child = dp;
                sp.Children.Add(row);
            }
            billCard.Margin = new Thickness(0, 12, 0, 0);
            panelOverview.Children.Add(billCard);
        }
    }

    // ====================================================
    // Tab 2: รายรับ-รายจ่าย
    // ====================================================
    private void AddTransaction_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtTxTitle.Text)) { MessageBox.Show("กรุณาใส่รายการ"); return; }
        if (!double.TryParse(txtTxAmount.Text, out var amount) || amount <= 0) { MessageBox.Show("กรุณาใส่จำนวนเงิน"); return; }

        var tx = new Transaction
        {
            Type = rbIncome.IsChecked == true ? "income" : "expense",
            Title = txtTxTitle.Text.Trim(),
            Amount = amount,
            Category = cmbTxCategory.SelectedItem as string ?? "อื่นๆ",
            Date = dpTxDate.SelectedDate?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd"),
        };
        _data.Transactions.Add(tx);
        FinanceStore.Save(_data);
        txtTxTitle.Text = ""; txtTxAmount.Text = "";
        Refresh();
    }

    // ====================================================
    // นำเข้าข้อมูลธนาคาร
    // ====================================================
    private void ImportCSV_Click(object sender, RoutedEventArgs e)
    {
        var bankItem = cmbImportBank.SelectedItem as ComboBoxItem;
        var bankTag = bankItem?.Tag as string ?? "Other";

        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "CSV/Text|*.csv;*.txt|Excel|*.xlsx|All|*.*",
            Title = "เลือกไฟล์ Statement จากธนาคาร"
        };
        if (dlg.ShowDialog() != true) return;

        panelImportResult.Children.Clear();
        try
        {
            var transactions = BankStatementParser.ParseCSV(dlg.FileName, bankTag);
            if (!transactions.Any())
            {
                panelImportResult.Children.Add(Txt("❌ ไม่พบรายการในไฟล์นี้\n\nลองตรวจสอบ:\n• ไฟล์เป็น CSV หรือไม่\n• มีหัวคอลัมน์ (วันที่, รายละเอียด, จำนวนเงิน)\n• ลองเลือกธนาคารให้ตรง", 13, "#e74c3c"));
                return;
            }

            ShowImportPreview(transactions, bankTag);
        }
        catch (Exception ex)
        {
            panelImportResult.Children.Add(Txt($"❌ เกิดข้อผิดพลาด:\n{ex.Message}", 13, "#e74c3c"));
        }
    }

    private void ParseSMS_Click(object sender, RoutedEventArgs e)
    {
        var text = txtSMSInput.Text.Trim();
        if (string.IsNullOrEmpty(text))
        {
            MessageBox.Show("กรุณาวาง SMS หรือแจ้งเตือนจากธนาคาร"); return;
        }

        panelImportResult.Children.Clear();

        // Split by lines — each line might be a separate SMS
        var lines = text.Split('\n').Where(l => l.Trim().Length > 5).ToList();
        var parsed = new List<BankTransaction>();

        foreach (var line in lines)
        {
            var tx = SMSParser.Parse(line.Trim());
            if (tx != null) parsed.Add(tx);
        }

        if (!parsed.Any())
        {
            panelImportResult.Children.Add(Txt("❌ ไม่สามารถแยกข้อมูลได้\n\nตัวอย่าง SMS ที่รองรับ:\n• 'KBank: จ่าย 350.00 บาท ที่ 7-Eleven คงเหลือ 12,500.00'\n• 'SCB แจ้งเตือน: รับเงิน 25,000 บาท จาก บจ.ABC คงเหลือ 45,000'", 13, "#e74c3c"));
            return;
        }

        ShowImportPreview(parsed, "SMS");
    }

    private void ShowImportPreview(List<BankTransaction> transactions, string source)
    {
        panelImportResult.Children.Clear();

        var income = transactions.Where(t => t.Type == "income").ToList();
        var expense = transactions.Where(t => t.Type == "expense").ToList();
        var totalIn = income.Sum(t => t.Amount);
        var totalOut = expense.Sum(t => t.Amount);

        panelImportResult.Children.Add(Txt($"✅ พบ {transactions.Count} รายการจาก {source}", 15, "#27ae60", true));
        panelImportResult.Children.Add(Txt($"💵 รายรับ: {income.Count} รายการ (฿{totalIn:N0})  |  💸 รายจ่าย: {expense.Count} รายการ (฿{totalOut:N0})", 12, "#8b8fa7"));

        // Category breakdown
        var catGroups = transactions.GroupBy(t => t.Category).OrderByDescending(g => g.Sum(t => t.Amount)).ToList();
        var catCard = MakeCard("🤖 AI จัดหมวดอัตโนมัติ");
        var cSp = (StackPanel)catCard.Child;
        foreach (var g in catGroups)
        {
            var total = g.Sum(t => t.Amount);
            var color = g.Key switch
            {
                "อาหาร" => "#e17055", "เดินทาง" => "#0984e3", "ช้อปปิ้ง" => "#e84393",
                "สาธารณูปโภค" => "#fdcb6e", "สุขภาพ" => "#00b894", "เงินเดือน" => "#27ae60",
                _ => "#6c63ff"
            };
            cSp.Children.Add(Txt($"  {g.Key}: {g.Count()} รายการ (฿{total:N0})", 12, color));
        }
        catCard.Margin = new Thickness(0, 8, 0, 0);
        panelImportResult.Children.Add(catCard);

        // Transaction list preview
        var listCard = MakeCard($"📋 รายการ (แสดง {Math.Min(transactions.Count, 20)} รายการแรก)");
        var lSp = (StackPanel)listCard.Child;
        foreach (var tx in transactions.Take(20))
        {
            var typeIcon = tx.Type == "income" ? "💵" : "💸";
            var amtColor = tx.Type == "income" ? "#27ae60" : "#e74c3c";
            var row = new Border { Background = B("#2a2d45"), CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 2, 0, 0) };
            var dp = new DockPanel();
            dp.Children.Add(Txt($"{typeIcon} {tx.Description}", 11, "#e8e9f0"));
            var rightPanel = new StackPanel { Orientation = Orientation.Horizontal };
            rightPanel.Children.Add(Badge(tx.Category));
            rightPanel.Children.Add(Txt($"฿{tx.Amount:N0}", 11, amtColor, true));
            DockPanel.SetDock(rightPanel, Dock.Right);
            dp.Children.Insert(0, rightPanel);
            row.Child = dp;
            lSp.Children.Add(row);
        }
        listCard.Margin = new Thickness(0, 8, 0, 0);
        panelImportResult.Children.Add(listCard);

        // Import button
        var txList = transactions;
        var importBtn = new Button
        {
            Content = $"✅ นำเข้า {transactions.Count} รายการเข้าระบบ",
            Padding = new Thickness(20, 10, 20, 10), FontSize = 14,
            Background = B("#27ae60"), Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand,
            Margin = new Thickness(0, 12, 0, 0), FontWeight = FontWeights.Bold
        };
        importBtn.Click += (_, _) =>
        {
            foreach (var tx in txList)
            {
                _data.Transactions.Add(new Transaction
                {
                    Type = tx.Type, Title = tx.Description, Amount = tx.Amount,
                    Category = tx.Category, Date = tx.Date, Note = $"นำเข้าจาก {source} ({tx.Bank})"
                });
            }
            FinanceStore.Save(_data);
            Refresh();
            MessageBox.Show($"นำเข้า {txList.Count} รายการสำเร็จ!\n\n💵 รายรับ: ฿{totalIn:N0}\n💸 รายจ่าย: ฿{totalOut:N0}\n\n🤖 AI จัดหมวดหมู่ให้แล้ว {catGroups.Count} หมวด",
                "✅ นำเข้าสำเร็จ");
            panelImportResult.Children.Clear();
            panelImportResult.Children.Add(Txt($"✅ นำเข้า {txList.Count} รายการเรียบร้อย!", 14, "#27ae60", true));
        };
        panelImportResult.Children.Add(importBtn);
    }

    private void RenderTransactions()
    {
        panelTransactions.Children.Clear();
        var grouped = _data.Transactions.OrderByDescending(t => t.Date).ThenByDescending(t => t.Created)
            .GroupBy(t => t.Date).ToList();

        foreach (var g in grouped.Take(30))
        {
            var dayIncome = g.Where(t => t.Type == "income").Sum(t => t.Amount);
            var dayExpense = g.Where(t => t.Type == "expense").Sum(t => t.Amount);
            panelTransactions.Children.Add(new TextBlock
            {
                Text = $"📅 {g.Key}   💵+{dayIncome:N0}  💸-{dayExpense:N0}",
                FontSize = 12, FontWeight = FontWeights.Bold, Foreground = B("#8b8fa7"),
                Margin = new Thickness(4, 10, 0, 4)
            });

            foreach (var tx in g)
            {
                var card = new Border { Background = B("#161828"), CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 2, 0, 0),
                    BorderBrush = B(tx.AmountColor), BorderThickness = new Thickness(2, 0, 0, 0) };
                var dp = new DockPanel();
                dp.Children.Add(Txt($"{tx.TypeIcon} {tx.Title}", 12, "#e8e9f0"));
                dp.Children.Add(Badge(tx.Category));
                var amtTxt = Txt($"฿{tx.AmountDisplay}", 13, tx.AmountColor);
                amtTxt.FontWeight = FontWeights.Bold;
                amtTxt.HorizontalAlignment = HorizontalAlignment.Right;
                DockPanel.SetDock(amtTxt, Dock.Right);
                dp.Children.Insert(0, amtTxt);

                var delBtn = MakeBtn("✕", "#e74c3c33");
                var txRef = tx;
                delBtn.Click += (_, _) => { _data.Transactions.Remove(txRef); FinanceStore.Save(_data); Refresh(); };
                DockPanel.SetDock(delBtn, Dock.Right);
                dp.Children.Insert(0, delBtn);

                card.Child = dp;
                panelTransactions.Children.Add(card);
            }
        }
    }

    // ====================================================
    // Tab 3: บิล
    // ====================================================
    private void AddBill_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtBillTitle.Text)) { MessageBox.Show("กรุณาใส่ชื่อบิล"); return; }
        if (!double.TryParse(txtBillAmount.Text, out var amount) || amount <= 0) { MessageBox.Show("กรุณาใส่จำนวนเงิน"); return; }

        _data.Bills.Add(new Bill
        {
            Title = txtBillTitle.Text.Trim(),
            Amount = amount,
            DueDate = dpBillDue.SelectedDate?.ToString("yyyy-MM-dd") ?? "",
            Category = cmbBillCat.SelectedItem as string ?? "อื่นๆ",
            Recurring = chkRecurring.IsChecked == true,
            RecurringType = chkRecurring.IsChecked == true ? "monthly" : "",
        });
        FinanceStore.Save(_data);
        txtBillTitle.Text = ""; txtBillAmount.Text = "";
        Refresh();
    }

    private void RenderBills()
    {
        panelBills.Children.Clear();

        var unpaid = _data.Bills.Where(b => !b.Paid).OrderBy(b => b.DueDate).ToList();
        var paid = _data.Bills.Where(b => b.Paid).OrderByDescending(b => b.PaidDate).ToList();

        if (unpaid.Any())
        {
            panelBills.Children.Add(Txt($"🟡 รอจ่าย ({unpaid.Count})", 14, "#f39c12", true));
            foreach (var b in unpaid) AddBillCard(b);
        }
        if (paid.Any())
        {
            panelBills.Children.Add(Txt($"\n✅ จ่ายแล้ว ({paid.Count})", 14, "#27ae60", true));
            foreach (var b in paid.Take(20)) AddBillCard(b);
        }
    }

    private void AddBillCard(Bill bill)
    {
        var color = bill.Paid ? "#27ae60" : bill.IsOverdue ? "#e74c3c" : "#f39c12";
        var card = new Border { Background = B("#161828"), CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12), Margin = new Thickness(0, 4, 0, 0),
            BorderBrush = B(color), BorderThickness = new Thickness(2, 0, 0, 0) };

        var sp = new StackPanel();
        var topRow = new DockPanel();
        topRow.Children.Add(Txt($"{bill.StatusIcon} {bill.Title}", 14, "#e8e9f0", true));
        var amtTxt = Txt($"฿{bill.Amount:N0}", 15, color, true);
        amtTxt.HorizontalAlignment = HorizontalAlignment.Right;
        DockPanel.SetDock(amtTxt, Dock.Right);
        topRow.Children.Insert(0, amtTxt);
        sp.Children.Add(topRow);

        var meta = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
        meta.Children.Add(Badge(bill.Category));
        meta.Children.Add(Badge($"📅 กำหนด: {bill.DueDate}"));
        if (bill.Recurring) meta.Children.Add(Badge("🔄 ประจำเดือน"));
        meta.Children.Add(Badge(bill.StatusLabel));
        sp.Children.Add(meta);

        if (!bill.Paid)
        {
            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
            var payBtn = MakeBtn("💳 จ่ายแล้ว", "#27ae60");
            var bRef = bill;
            payBtn.Click += (_, _) =>
            {
                bRef.Paid = true;
                bRef.PaidDate = DateTime.Today.ToString("yyyy-MM-dd");
                FinanceStore.Save(_data);
                Refresh();
                MessageBox.Show($"บันทึกจ่าย \"{bRef.Title}\" ฿{bRef.Amount:N0} แล้ว!", "✅ จ่ายบิล");
            };
            btnRow.Children.Add(payBtn);

            var delBtn = MakeBtn("🗑️ ลบ", "#e74c3c");
            delBtn.Margin = new Thickness(6, 0, 0, 0);
            delBtn.Click += (_, _) =>
            {
                _data.Bills.Remove(bRef); FinanceStore.Save(_data); Refresh();
            };
            btnRow.Children.Add(delBtn);
            sp.Children.Add(btnRow);
        }

        card.Child = sp;
        panelBills.Children.Add(card);
    }

    // ====================================================
    // SMS Receiver Server
    // ====================================================
    private void StartServer_Click(object sender, RoutedEventArgs e)
    {
        if (_smsServer?.IsRunning == true) { MessageBox.Show("เซิร์ฟเวอร์ทำงานอยู่แล้ว"); return; }
        try
        {
            _smsServer = new SMSReceiverServer(8899,
                onReceive: tx => Dispatcher.Invoke(() =>
                {
                    _data.Transactions.Add(new Transaction
                    {
                        Type = tx.Type, Title = tx.Description, Amount = tx.Amount,
                        Category = tx.Category, Date = tx.Date, Note = $"[SMS] {tx.Bank}"
                    });
                    FinanceStore.Save(_data);
                    AddSMSLogEntry(tx);
                    Refresh();
                }),
                onLog: msg => Dispatcher.Invoke(() => lblServerStatus.Text = msg)
            );
            _smsServer.Start();

            var ip = GetLocalIP();
            lblServerIP.Text = $"📡 URL: http://{ip}:8899\n📱 เปิด URL นี้ในมือถือ (WiFi เดียวกัน)";
            btnStartServer.IsEnabled = false;
        }
        catch (Exception ex) { MessageBox.Show($"เริ่มเซิร์ฟเวอร์ไม่ได้:\n{ex.Message}\n\nลองรันแอปในฐานะ Administrator"); }
    }

    private void StopServer_Click(object sender, RoutedEventArgs e)
    {
        _smsServer?.Stop();
        btnStartServer.IsEnabled = true;
        lblServerIP.Text = "";
    }

    private void OpenSetup_Click(object sender, RoutedEventArgs e)
    {
        if (_smsServer?.IsRunning != true) { MessageBox.Show("กรุณาเริ่มเซิร์ฟเวอร์ก่อน"); return; }
        var ip = GetLocalIP();
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo($"http://{ip}:8899") { UseShellExecute = true });
    }

    private void AddSMSLogEntry(BankTransaction tx)
    {
        var row = new Border { Background = B("#2a2d45"), CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 3, 0, 0) };
        var dp = new DockPanel();
        var typeIcon = tx.Type == "income" ? "💵" : "💸";
        var color = tx.Type == "income" ? "#27ae60" : "#e74c3c";
        dp.Children.Add(Txt($"{typeIcon} {tx.Description}", 11, "#e8e9f0"));
        var rightSp = new StackPanel { Orientation = Orientation.Horizontal };
        rightSp.Children.Add(Badge(tx.Category));
        rightSp.Children.Add(Badge(tx.Bank));
        rightSp.Children.Add(Txt($"฿{tx.Amount:N0}", 12, color, true));
        DockPanel.SetDock(rightSp, Dock.Right);
        dp.Children.Insert(0, rightSp);
        row.Child = dp;
        panelSMSLog.Children.Insert(0, row);
        if (panelSMSLog.Children.Count > 20) panelSMSLog.Children.RemoveAt(20);
    }

    private static string GetLocalIP()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            var ip = host.AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            return ip?.ToString() ?? "localhost";
        }
        catch { return "localhost"; }
    }

    // ====================================================
    // วิเคราะห์ความเสี่ยง-กำไร + กราฟ
    // ====================================================
    private void RenderAnalysis()
    {
        panelAnalysis.Children.Clear();
        panelAnalysis.Children.Add(Txt("📈 วิเคราะห์ความเสี่ยง-กำไร + สรุปกราฟภาพรวม", 18, "#e8e9f0", true));
        panelAnalysis.Children.Add(Txt($"อัพเดท: {DateTime.Now:dd/MM/yyyy HH:mm}", 11, "#8b8fa7"));

        var totalBalance = _data.Accounts.Sum(a => a.Balance);
        var thisMonth = DateTime.Today.ToString("yyyy-MM");
        var monthIncome = _data.Transactions.Where(t => t.Date.StartsWith(thisMonth) && t.Type == "income").Sum(t => t.Amount);
        var monthExpense = _data.Transactions.Where(t => t.Date.StartsWith(thisMonth) && t.Type == "expense").Sum(t => t.Amount);

        // 1. Risk-Return Scatter Plot
        RenderRiskReturnChart();

        // 2. Income vs Expense Bar Chart (6 months)
        RenderMonthlyChart();

        // 3. Portfolio allocation Donut chart
        RenderAllocationChart();

        // 4. Risk meter
        RenderRiskMeter();

        // 5. Profit projection comparison chart
        RenderProfitProjectionChart(totalBalance);

        // 6. AI สรุปวิเคราะห์
        RenderAISummary(totalBalance, monthIncome, monthExpense);
    }

    private void RenderRiskReturnChart()
    {
        var card = MakeCard("📊 แผนภูมิ ความเสี่ยง vs ผลตอบแทน (Risk-Return)");
        var sp = (StackPanel)card.Child;
        sp.Children.Add(Txt("แกนนอน = ความเสี่ยง  |  แกนตั้ง = ผลตอบแทนคาดหวัง (%/ปี)", 10, "#8b8fa7"));

        var canvas = new Canvas { Width = 500, Height = 280, Margin = new Thickness(0, 8, 0, 0), Background = B("#151528") };

        // Grid lines + labels
        for (int i = 0; i <= 4; i++)
        {
            var y = 260 - i * 60;
            canvas.Children.Add(new Line { X1 = 50, Y1 = y, X2 = 490, Y2 = y, Stroke = B("#2a2d45"), StrokeThickness = 1 });
            var lbl = Txt($"{i * 4}%", 9, "#5a5d70");
            Canvas.SetLeft(lbl, 15); Canvas.SetTop(lbl, y - 7); canvas.Children.Add(lbl);
        }
        var xLabels = new[] { ("ต่ำ", 100), ("ปานกลาง", 230), ("สูง", 360), ("สูงมาก", 460) };
        foreach (var (text, x) in xLabels)
        {
            var lbl = Txt(text, 9, "#5a5d70");
            Canvas.SetLeft(lbl, x - 15); Canvas.SetTop(lbl, 265); canvas.Children.Add(lbl);
        }

        // Plot investments
        var items = new (string name, string icon, double risk, double ret, string color)[]
        {
            ("เงินฝาก", "🏦", 0.5, 1.5, "#27ae60"),
            ("พันธบัตร", "🏛️", 1.0, 2.8, "#2ecc71"),
            ("กองทุนตราสารหนี้", "📄", 1.5, 3.5, "#00b894"),
            ("ทองคำ", "🥇", 2.5, 5.5, "#fdcb6e"),
            ("กองทุนผสม", "🎯", 3.0, 6.0, "#f39c12"),
            ("SSF/RMF", "💎", 3.2, 7.0, "#e84393"),
            ("REIT", "🏠", 3.5, 6.5, "#0984e3"),
            ("ETF", "📊", 4.0, 8.0, "#6c63ff"),
            ("หุ้นไทย", "📈", 4.5, 8.5, "#e17055"),
            ("หุ้นต่างประเทศ", "🌍", 4.8, 10.0, "#d63031"),
            ("คริปโต", "₿", 5.5, 15.0, "#e74c3c"),
        };

        foreach (var (name, icon, risk, ret, color) in items)
        {
            var x = 50 + risk / 6.0 * 440;
            var y = 260 - ret / 16.0 * 240;
            var dot = new Ellipse { Width = 18, Height = 18, Fill = B(color), Opacity = 0.9 };
            Canvas.SetLeft(dot, x - 9); Canvas.SetTop(dot, y - 9); canvas.Children.Add(dot);
            var label = Txt($"{icon}{name}", 8, color);
            Canvas.SetLeft(label, x - 20); Canvas.SetTop(label, y - 22); canvas.Children.Add(label);
        }

        sp.Children.Add(canvas);
        sp.Children.Add(Txt("💡 มุมซ้ายบน = ดีที่สุด (ผลตอบแทนสูง ความเสี่ยงต่ำ)  |  มุมขวาล่าง = เสี่ยงสูง ผลตอบแทนต่ำ", 10, "#8b8fa7"));
        card.Margin = new Thickness(0, 12, 0, 0);
        panelAnalysis.Children.Add(card);
    }

    private void RenderMonthlyChart()
    {
        var card = MakeCard("📊 รายรับ vs รายจ่าย 6 เดือนล่าสุด");
        var sp = (StackPanel)card.Child;

        var months = Enumerable.Range(0, 6).Select(i => DateTime.Today.AddMonths(-5 + i)).ToList();
        var data = months.Select(m =>
        {
            var key = m.ToString("yyyy-MM");
            var inc = _data.Transactions.Where(t => t.Date.StartsWith(key) && t.Type == "income").Sum(t => t.Amount);
            var exp = _data.Transactions.Where(t => t.Date.StartsWith(key) && t.Type == "expense").Sum(t => t.Amount);
            return (month: m, income: inc, expense: exp);
        }).ToList();

        var maxVal = Math.Max(data.Max(d => Math.Max(d.income, d.expense)), 1);

        var chartGrid = new Grid { Height = 160, Margin = new Thickness(0, 8, 0, 0) };
        for (int i = 0; i < 6; i++)
            chartGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        for (int i = 0; i < 6; i++)
        {
            var d = data[i];
            var colSp = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Center };

            // Bars side by side
            var barRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            var incH = Math.Max(d.income / maxVal * 100, d.income > 0 ? 4 : 1);
            var expH = Math.Max(d.expense / maxVal * 100, d.expense > 0 ? 4 : 1);

            barRow.Children.Add(new Border { Width = 18, Height = incH, Background = B("#27ae60"),
                CornerRadius = new CornerRadius(3, 3, 0, 0), Margin = new Thickness(1, 0, 1, 0) });
            barRow.Children.Add(new Border { Width = 18, Height = expH, Background = B("#e74c3c"),
                CornerRadius = new CornerRadius(3, 3, 0, 0), Margin = new Thickness(1, 0, 1, 0) });
            colSp.Children.Add(barRow);

            var isThis = d.month.ToString("yyyy-MM") == DateTime.Today.ToString("yyyy-MM");
            colSp.Children.Add(Txt(d.month.ToString("MM/yy"), 10, isThis ? "#6c63ff" : "#8b8fa7", isThis));
            Grid.SetColumn(colSp, i);
            chartGrid.Children.Add(colSp);
        }

        sp.Children.Add(chartGrid);
        var legend = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
        legend.Children.Add(new Ellipse { Width = 10, Height = 10, Fill = B("#27ae60"), Margin = new Thickness(0, 0, 4, 0) });
        legend.Children.Add(Txt("รายรับ  ", 11, "#27ae60"));
        legend.Children.Add(new Ellipse { Width = 10, Height = 10, Fill = B("#e74c3c"), Margin = new Thickness(8, 0, 4, 0) });
        legend.Children.Add(Txt("รายจ่าย", 11, "#e74c3c"));
        sp.Children.Add(legend);

        var lastMonth = data.Last();
        var net = lastMonth.income - lastMonth.expense;
        sp.Children.Add(Txt($"เดือนนี้: รายรับ ฿{lastMonth.income:N0}  รายจ่าย ฿{lastMonth.expense:N0}  คงเหลือ ฿{net:N0}", 11,
            net >= 0 ? "#27ae60" : "#e74c3c"));

        card.Margin = new Thickness(0, 12, 0, 0);
        panelAnalysis.Children.Add(card);
    }

    private void RenderAllocationChart()
    {
        var card = MakeCard("🥧 สัดส่วนพอร์ตแนะนำ (สมดุล)");
        var sp = (StackPanel)card.Child;

        var plans = InvestmentAdvisor.RecommendPortfolio(_data, "moderate");
        var canvas = new Canvas { Width = 200, Height = 200, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 4) };
        double cx = 100, cy = 100, r = 90, startAngle = 0;
        var colors = new[] { "#6c63ff", "#e84393", "#00b894", "#fdcb6e", "#e17055", "#0984e3", "#00cec9", "#d63031" };

        for (int i = 0; i < plans.Count; i++)
        {
            var sweep = plans[i].AllocationPercent / 100.0 * 360;
            var path = CreatePieSlice(cx, cy, r, startAngle, startAngle + sweep, colors[i % colors.Length]);
            canvas.Children.Add(path);
            startAngle += sweep;
        }
        canvas.Children.Add(new Ellipse { Width = 70, Height = 70, Fill = B("#161828"),
            Margin = new Thickness(cx - 35, cy - 35, 0, 0) });
        var centerTxt = Txt("⚖️", 20, "#e8e9f0");
        Canvas.SetLeft(centerTxt, cx - 12); Canvas.SetTop(centerTxt, cy - 14); canvas.Children.Add(centerTxt);
        sp.Children.Add(canvas);

        // Legend
        for (int i = 0; i < plans.Count; i++)
        {
            var p = plans[i];
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };
            row.Children.Add(new Ellipse { Width = 10, Height = 10, Fill = B(colors[i % colors.Length]),
                Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center });
            row.Children.Add(Txt($"{p.Icon} {p.Name} — {p.AllocationPercent:F0}% (ผลตอบแทน ~{p.ExpectedReturn:F1}%)", 11, "#e8e9f0"));
            sp.Children.Add(row);
        }

        card.Margin = new Thickness(0, 12, 0, 0);
        panelAnalysis.Children.Add(card);
    }

    private void RenderRiskMeter()
    {
        var card = MakeCard("🎯 เกจวัดระดับความเสี่ยงพอร์ต");
        var sp = (StackPanel)card.Child;

        var plans = InvestmentAdvisor.RecommendPortfolio(_data, "moderate");
        var avgRisk = plans.Sum(p =>
        {
            var riskScore = p.RiskLevel switch
            {
                "ต่ำมาก" => 1, "ต่ำ" => 2, "ต่ำ-ปานกลาง" => 3, "ปานกลาง" => 4,
                "ปานกลาง-สูง" => 5, "สูง" => 6, "สูงมาก" => 7, _ => 4
            };
            return riskScore * p.AllocationPercent / 100.0;
        });
        var avgReturn = plans.Sum(p => p.ExpectedReturn * p.AllocationPercent / 100.0);

        // Risk bar
        var barContainer = new Grid { Height = 30, Margin = new Thickness(0, 8, 0, 4) };
        var gradient = new LinearGradientBrush();
        gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#27ae60"), 0));
        gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#f39c12"), 0.4));
        gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#e74c3c"), 0.7));
        gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#8e44ad"), 1));

        barContainer.Children.Add(new Border { Background = gradient, CornerRadius = new CornerRadius(8), Height = 16 });

        var markerPos = Math.Clamp(avgRisk / 7.0, 0, 1);
        var marker = new Border { Width = 4, Height = 28, Background = B("#ffffff"), CornerRadius = new CornerRadius(2),
            HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(markerPos * 480 + 10, 0, 0, 0) };
        barContainer.Children.Add(marker);
        sp.Children.Add(barContainer);

        var labelsRow = new Grid();
        labelsRow.ColumnDefinitions.Add(new ColumnDefinition());
        labelsRow.ColumnDefinitions.Add(new ColumnDefinition());
        labelsRow.ColumnDefinitions.Add(new ColumnDefinition());
        labelsRow.ColumnDefinitions.Add(new ColumnDefinition());
        labelsRow.Children.Add(Txt("🟢 ต่ำ", 10, "#27ae60"));
        var m1 = Txt("🟡 ปานกลาง", 10, "#f39c12"); Grid.SetColumn(m1, 1); labelsRow.Children.Add(m1);
        var m2 = Txt("🔴 สูง", 10, "#e74c3c"); Grid.SetColumn(m2, 2); labelsRow.Children.Add(m2);
        var m3 = Txt("🟣 สูงมาก", 10, "#8e44ad"); m3.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetColumn(m3, 3); labelsRow.Children.Add(m3);
        sp.Children.Add(labelsRow);

        var riskLabel = avgRisk < 2.5 ? "ต่ำ" : avgRisk < 4 ? "ปานกลาง" : avgRisk < 5.5 ? "สูง" : "สูงมาก";
        var riskColor = avgRisk < 2.5 ? "#27ae60" : avgRisk < 4 ? "#f39c12" : avgRisk < 5.5 ? "#e74c3c" : "#8e44ad";

        sp.Children.Add(Txt($"\n📊 ค่าความเสี่ยงเฉลี่ย: {avgRisk:F1}/7 ({riskLabel})", 14, riskColor, true));
        sp.Children.Add(Txt($"📈 ผลตอบแทนคาดหวัง: {avgReturn:F1}% ต่อปี", 14, "#27ae60", true));
        sp.Children.Add(Txt($"⚖️ อัตราส่วน Return/Risk: {avgReturn / Math.Max(avgRisk, 0.1):F2}  " +
            $"({(avgReturn / Math.Max(avgRisk, 0.1) > 1.5 ? "✅ ดี — คุ้มค่าความเสี่ยง" : "⚠️ ปรับปรุงได้ — ลองกระจายพอร์ตเพิ่ม")})", 12, "#8b8fa7"));

        card.Margin = new Thickness(0, 12, 0, 0);
        panelAnalysis.Children.Add(card);
    }

    private void RenderProfitProjectionChart(double totalBalance)
    {
        var monthlyAdd = _data.Transactions.Any(t => t.Type == "income")
            ? _data.Transactions.Where(t => t.Type == "income").Average(t => t.Amount) * 0.2 : 5000;
        var emergency = totalBalance * 0.3;
        var investable = Math.Max(totalBalance - emergency, 0);
        var projections = InvestmentAdvisor.ProjectGrowth(investable, monthlyAdd, 20);

        var card = MakeCard($"📈 กราฟคาดการณ์เงินเติบโต 20 ปี (เริ่ม ฿{investable:N0})");
        var sp = (StackPanel)card.Child;

        var canvas = new Canvas { Width = 520, Height = 220, Margin = new Thickness(0, 8, 0, 0), Background = B("#151528") };
        var maxVal = projections.Last().Aggressive;
        if (maxVal <= 0) maxVal = 1;

        // Grid lines
        for (int i = 0; i <= 4; i++)
        {
            var y = 200 - i * 45;
            canvas.Children.Add(new Line { X1 = 50, Y1 = y, X2 = 510, Y2 = y, Stroke = B("#2a2d45"), StrokeThickness = 1 });
            var val = maxVal / 4 * i;
            var lbl = Txt(val >= 1000000 ? $"{val / 1000000:F1}M" : $"{val / 1000:F0}K", 8, "#5a5d70");
            Canvas.SetLeft(lbl, 5); Canvas.SetTop(lbl, y - 6); canvas.Children.Add(lbl);
        }

        // Lines
        var lineData = new (Func<InvestmentProjection, double> getter, string color, string label)[]
        {
            (p => p.Conservative, "#27ae60", "🛡️ ระวัง"),
            (p => p.Moderate, "#f39c12", "⚖️ สมดุล"),
            (p => p.Aggressive, "#e74c3c", "🚀 เชิงรุก"),
        };

        foreach (var (getter, color, label) in lineData)
        {
            var polyline = new Polyline { Stroke = B(color), StrokeThickness = 2.5 };
            polyline.Points.Add(new Point(50, 200));
            foreach (var p in projections)
            {
                var x = 50 + p.Year / 20.0 * 460;
                var y = 200 - getter(p) / maxVal * 180;
                polyline.Points.Add(new Point(x, Math.Max(y, 10)));
            }
            canvas.Children.Add(polyline);

            // End label
            var lastP = projections.Last();
            var endY = 200 - getter(lastP) / maxVal * 180;
            var endLbl = Txt($"{label} ฿{getter(lastP):N0}", 8, color);
            Canvas.SetLeft(endLbl, 400); Canvas.SetTop(endLbl, Math.Max(endY - 12, 2));
            canvas.Children.Add(endLbl);
        }

        // Year labels
        foreach (var yr in new[] { 1, 5, 10, 15, 20 })
        {
            var x = 50 + yr / 20.0 * 460;
            var lbl = Txt($"{yr}ปี", 9, "#8b8fa7");
            Canvas.SetLeft(lbl, x - 10); Canvas.SetTop(lbl, 205);
            canvas.Children.Add(lbl);
        }

        sp.Children.Add(canvas);

        var last = projections.Last();
        sp.Children.Add(Txt($"\n💰 20 ปีข้างหน้า:", 13, "#e8e9f0", true));
        sp.Children.Add(Txt($"   🛡️ ระวัง (3.5%): ฿{last.Conservative:N0}  กำไร +{(last.Conservative / Math.Max(investable, 1) - 1) * 100:F0}%", 12, "#27ae60"));
        sp.Children.Add(Txt($"   ⚖️ สมดุล (6.5%): ฿{last.Moderate:N0}  กำไร +{(last.Moderate / Math.Max(investable, 1) - 1) * 100:F0}%", 12, "#f39c12"));
        sp.Children.Add(Txt($"   🚀 เชิงรุก (10%): ฿{last.Aggressive:N0}  กำไร +{(last.Aggressive / Math.Max(investable, 1) - 1) * 100:F0}%", 12, "#e74c3c"));

        card.Margin = new Thickness(0, 12, 0, 0);
        panelAnalysis.Children.Add(card);
    }

    private void RenderAISummary(double totalBalance, double income, double expense)
    {
        var card = new Border { Background = B("#161828"), CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16), Margin = new Thickness(0, 12, 0, 0),
            BorderBrush = B("#6c63ff"), BorderThickness = new Thickness(0, 0, 0, 3) };
        var sp = new StackPanel();
        sp.Children.Add(Txt("🤖 AI สรุปวิเคราะห์ภาพรวม", 16, "#6c63ff", true));

        var savings = income - expense;
        var savingsRate = income > 0 ? savings / income * 100 : 0;

        var lines = new List<string>();
        lines.Add($"📋 สถานะ: ยอดเงินรวม ฿{totalBalance:N0}");
        if (income > 0) lines.Add($"💰 อัตราการออมเดือนนี้: {savingsRate:F0}% ({(savingsRate >= 20 ? "✅ ดีมาก" : savingsRate >= 10 ? "🟡 พอใช้" : "⚠️ ควรเพิ่ม")})");

        if (savings > 0 && totalBalance > 0)
            lines.Add($"📈 แนะนำ: นำเงินออม ฿{savings:N0}/เดือน ไปลงทุนแทนฝากออมทรัพย์ จะเติบโตเร็วกว่า 5-10 เท่า");

        var overdue = _data.Bills.Count(b => b.IsOverdue);
        if (overdue > 0)
            lines.Add($"🔴 เร่งด่วน: มีบิลเลยกำหนด {overdue} รายการ — จ่ายก่อนเสียค่าปรับ");

        var btc = _marketPrices.Find(p => p.Name == "Bitcoin");
        if (btc != null && btc.Change24h < -5)
            lines.Add($"₿ ตลาดคริปโตร่วง {btc.Change24h:F1}% — อาจเป็นจังหวะซื้อสะสมระยะยาว (DCA) แต่ระวังความเสี่ยง");
        else if (btc != null && btc.Change24h > 5)
            lines.Add($"₿ ตลาดคริปโตพุ่ง +{btc.Change24h:F1}% — ระวังอย่าซื้อตามกระแส FOMO");

        lines.Add("\n💡 กลยุทธ์แนะนำ:");
        if (totalBalance < 100000)
            lines.Add("1. เน้นสร้างเงินสำรอง 3-6 เดือน ก่อนลงทุน\n2. เริ่มจากกองทุนรวมตราสารหนี้/ผสม ความเสี่ยงต่ำ\n3. ตั้งเป้าออม 20% ของรายได้ทุกเดือน");
        else if (totalBalance < 500000)
            lines.Add("1. มีเงินสำรองแล้ว เริ่มกระจายลงทุนได้\n2. แบ่ง: 40% กองทุน + 30% หุ้น/ETF + 20% ทอง + 10% คริปโต\n3. ใช้กลยุทธ์ DCA ลงทุนสม่ำเสมอทุกเดือน");
        else
            lines.Add("1. เงินทุนพร้อม กระจายพอร์ตเต็มที่\n2. พิจารณา SSF/RMF เพื่อประหยัดภาษี\n3. ลงทุนต่างประเทศเพื่อกระจายความเสี่ยงจากเศรษฐกิจไทย\n4. รีวิวพอร์ตทุก 3-6 เดือน ปรับสมดุล Rebalance");

        foreach (var line in lines) sp.Children.Add(Txt(line, 12, "#d0d0e0"));

        sp.Children.Add(Txt("\n⚠️ ข้อมูลเป็นการวิเคราะห์เบื้องต้น ไม่ใช่คำแนะนำการลงทุน ควรศึกษาเพิ่มเติมก่อนตัดสินใจ", 10, "#5a5d78"));
        card.Child = sp;
        panelAnalysis.Children.Add(card);
    }

    private Path CreatePieSlice(double cx, double cy, double r, double startDeg, double endDeg, string color)
    {
        if (endDeg - startDeg >= 359.99) endDeg = startDeg + 359.99;
        var s = startDeg * Math.PI / 180; var e = endDeg * Math.PI / 180;
        var x1 = cx + r * Math.Cos(s); var y1 = cy + r * Math.Sin(s);
        var x2 = cx + r * Math.Cos(e); var y2 = cy + r * Math.Sin(e);
        var fig = new PathFigure { StartPoint = new Point(cx, cy), IsClosed = true };
        fig.Segments.Add(new LineSegment(new Point(x1, y1), true));
        fig.Segments.Add(new ArcSegment(new Point(x2, y2), new Size(r, r), 0, (endDeg - startDeg) > 180, SweepDirection.Clockwise, true));
        var geo = new PathGeometry(); geo.Figures.Add(fig);
        return new Path { Data = geo, Fill = B(color) };
    }

    // ====================================================
    // ข้อมูลตลาดล่าสุด
    // ====================================================
    private List<MarketPrice> _marketPrices = new();

    private async Task LoadMarketData()
    {
        lblMarketStatus.Text = "⏳ กำลังโหลดข้อมูลตลาด...";
        try
        {
            _marketPrices = await MarketDataService.FetchAll();
            RenderMarketTab();
            lblMarketStatus.Text = $"✅ อัพเดท {DateTime.Now:HH:mm}";
        }
        catch (Exception ex)
        {
            lblMarketStatus.Text = $"❌ โหลดไม่สำเร็จ";
            panelMarket.Children.Clear();
            panelMarket.Children.Add(Txt($"ไม่สามารถโหลดข้อมูลตลาดได้:\n{ex.Message}\n\nลองกดปุ่ม 🔄 อัพเดทตลาด อีกครั้ง", 13, "#e74c3c"));
        }
    }

    private async void RefreshMarket_Click(object sender, RoutedEventArgs e)
    {
        MarketDataService.ClearCache();
        await LoadMarketData();
        RenderAIInvestment();
    }

    private void RenderMarketTab()
    {
        panelMarket.Children.Clear();
        panelMarket.Children.Add(Txt("📡 ข้อมูลตลาดล่าสุด (Real-time)", 18, "#e8e9f0", true));
        panelMarket.Children.Add(Txt($"อัพเดทเมื่อ: {DateTime.Now:dd/MM/yyyy HH:mm}  ·  กดปุ่ม 🔄 เพื่ออัพเดท  ·  แคช 5 นาที", 11, "#8b8fa7"));

        var categories = _marketPrices.Select(p => p.Category).Distinct();
        foreach (var cat in categories)
        {
            var items = _marketPrices.Where(p => p.Category == cat).ToList();
            var catIcon = cat switch
            {
                "คริปโต" => "₿",
                "อัตราแลกเปลี่ยน" => "💱",
                "สินค้าโภคภัณฑ์" => "🥇",
                "อัตราดอกเบี้ย" => "🏛️",
                "เศรษฐกิจ" => "📊",
                _ => "📌"
            };

            var catCard = MakeCard($"{catIcon} {cat} ({items.Count})");
            var sp = (StackPanel)catCard.Child;

            foreach (var item in items)
            {
                var row = new Border
                {
                    Background = B("#2a2d45"), CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10, 7, 10, 7), Margin = new Thickness(0, 3, 0, 0)
                };
                var dp = new DockPanel();

                dp.Children.Add(Txt($"{item.Icon} {item.Name}", 13, "#e8e9f0"));

                var rightSp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

                string priceStr;
                if (item.Currency == "%" )
                    priceStr = $"{item.Price:F2}%";
                else if (item.Price >= 1000000)
                    priceStr = $"฿{item.Price:N0}";
                else if (item.Price >= 1)
                    priceStr = $"฿{item.Price:N2}";
                else
                    priceStr = $"{item.Price:F4}";

                rightSp.Children.Add(Txt(priceStr, 14, "#e8e9f0", true));

                if (Math.Abs(item.Change24h) > 0.001)
                {
                    var changeColor = item.Change24h >= 0 ? "#27ae60" : "#e74c3c";
                    var changeIcon = item.Change24h >= 0 ? "▲" : "▼";
                    rightSp.Children.Add(Txt($"  {changeIcon}{Math.Abs(item.Change24h):F2}%", 11, changeColor));
                }

                DockPanel.SetDock(rightSp, Dock.Right);
                dp.Children.Insert(0, rightSp);

                row.Child = dp;
                sp.Children.Add(row);
            }

            catCard.Margin = new Thickness(0, 10, 0, 0);
            panelMarket.Children.Add(catCard);
        }

        // Market insight
        var btc = _marketPrices.Find(p => p.Name == "Bitcoin");
        var usdThb = _marketPrices.Find(p => p.Name == "USD/THB");
        if (btc != null || usdThb != null)
        {
            var insightCard = MakeCard("💡 AI วิเคราะห์ตลาดวันนี้");
            var iSp = (StackPanel)insightCard.Child;

            if (btc != null)
            {
                var btcTrend = btc.Change24h > 2 ? "📈 ตลาดคริปโตขาขึ้น — อาจพิจารณาถือต่อ"
                    : btc.Change24h < -2 ? "📉 ตลาดคริปโตขาลง — ระวังความเสี่ยง อาจเป็นจังหวะซื้อสะสมสำหรับระยะยาว"
                    : "➡️ ตลาดคริปโตทรงตัว — ไม่มีสัญญาณเด่นชัด";
                iSp.Children.Add(Txt($"₿ Bitcoin: ฿{btc.Price:N0} ({(btc.Change24h >= 0 ? "+" : "")}{btc.Change24h:F2}%)", 12, btc.Change24h >= 0 ? "#27ae60" : "#e74c3c"));
                iSp.Children.Add(Txt($"   {btcTrend}", 11, "#8b8fa7"));
            }

            if (usdThb != null)
            {
                var thbTrend = usdThb.Price > 36 ? "⚠️ บาทอ่อน — การลงทุนต่างประเทศอาจได้กำไรจากอัตราแลกเปลี่ยนเมื่อบาทแข็งค่ากลับ"
                    : usdThb.Price < 33 ? "💪 บาทแข็ง — จังหวะดีซื้อสินทรัพย์ต่างประเทศ"
                    : "➡️ ค่าเงินบาททรงตัว";
                iSp.Children.Add(Txt($"\n🇺🇸 USD/THB: {usdThb.Price:F2} บาท", 12, "#0984e3"));
                iSp.Children.Add(Txt($"   {thbTrend}", 11, "#8b8fa7"));
            }

            iSp.Children.Add(Txt("\n📊 คำแนะนำ: กระจายพอร์ตลงทุนหลายสินทรัพย์ ไม่ทุ่มทั้งหมดในตัวเดียว\n" +
                "อัพเดทข้อมูลสม่ำเสมอเพื่อปรับกลยุทธ์ตามสถานการณ์ตลาด", 11, "#8b8fa7"));

            insightCard.Margin = new Thickness(0, 10, 0, 0);
            panelMarket.Children.Add(insightCard);
        }
    }

    // ====================================================
    // Tab: AI แนะนำการลงทุน
    // ====================================================
    private void RenderAIInvestment()
    {
        panelAIInvest.Children.Clear();

        // Financial Health
        var health = InvestmentAdvisor.AnalyzeHealth(_data);
        var healthCard = new Border { Background = B("#161828"), CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16), Margin = new Thickness(0, 0, 0, 12),
            BorderBrush = B(health.GradeColor), BorderThickness = new Thickness(0, 0, 0, 3) };
        var hSp = new StackPanel();
        var hTop = new DockPanel();
        hTop.Children.Add(Txt("🩺 สุขภาพการเงินของคุณ", 16, "#e8e9f0", true));
        var gradeTxt = Txt($"เกรด {health.Grade}  ({health.Score}/100)", 18, health.GradeColor, true);
        gradeTxt.HorizontalAlignment = HorizontalAlignment.Right;
        DockPanel.SetDock(gradeTxt, Dock.Right);
        hTop.Children.Insert(0, gradeTxt);
        hSp.Children.Add(hTop);

        // Progress bar
        var barGrid = new Grid { Margin = new Thickness(0, 8, 0, 8), Height = 12 };
        barGrid.Children.Add(new Border { Background = B("#2a2d45"), CornerRadius = new CornerRadius(6) });
        barGrid.Children.Add(new Border { Background = B(health.GradeColor), CornerRadius = new CornerRadius(6),
            HorizontalAlignment = HorizontalAlignment.Left, Width = health.Score * 4.5 });
        hSp.Children.Add(barGrid);

        foreach (var s in health.Strengths) hSp.Children.Add(Txt(s, 12, "#27ae60"));
        foreach (var w in health.Warnings) hSp.Children.Add(Txt(w, 12, "#e74c3c"));
        foreach (var r in health.Recommendations) hSp.Children.Add(Txt(r, 12, "#0984e3"));
        healthCard.Child = hSp;
        panelAIInvest.Children.Add(healthCard);

        // Portfolio recommendation buttons
        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        btnRow.Children.Add(Txt("🤖 เลือกระดับความเสี่ยง:  ", 13, "#8b8fa7"));
        foreach (var (label, profile, color) in new[] { ("🛡️ ระวัง", "conservative", "#27ae60"), ("⚖️ สมดุล", "moderate", "#f39c12"), ("🚀 เชิงรุก", "aggressive", "#e74c3c") })
        {
            var btn = MakeBtn(label, color);
            btn.Margin = new Thickness(0, 0, 8, 0);
            var p = profile;
            btn.Click += (_, _) => ShowPortfolio(p);
            btnRow.Children.Add(btn);
        }
        panelAIInvest.Children.Add(btnRow);

        // Live market mini dashboard
        if (_marketPrices.Any())
        {
            var liveCard = new Border { Background = B("#161828"), CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 12),
                BorderBrush = B("#0984e3"), BorderThickness = new Thickness(0, 0, 0, 2) };
            var liveSp = new StackPanel();
            liveSp.Children.Add(Txt("📡 ข้อมูลตลาดล่าสุด (อ้างอิงในการแนะนำ)", 13, "#0984e3", true));
            var liveRow = new WrapPanel { Margin = new Thickness(0, 6, 0, 0) };

            var btc = _marketPrices.Find(p => p.Name == "Bitcoin");
            if (btc != null)
            {
                var chgIcon = btc.Change24h >= 0 ? "▲" : "▼";
                var chgColor = btc.Change24h >= 0 ? "#27ae60" : "#e74c3c";
                liveRow.Children.Add(Badge($"₿ BTC: ฿{btc.Price:N0} {chgIcon}{Math.Abs(btc.Change24h):F1}%"));
            }
            var eth = _marketPrices.Find(p => p.Name == "Ethereum");
            if (eth != null) liveRow.Children.Add(Badge($"Ξ ETH: ฿{eth.Price:N0}"));
            var gold = _marketPrices.Find(p => p.Name == "ทองคำ บาทละ");
            if (gold != null) liveRow.Children.Add(Badge($"🥇 ทอง: ฿{gold.Price:N0}/บาท"));
            var usd = _marketPrices.Find(p => p.Name == "USD/THB");
            if (usd != null) liveRow.Children.Add(Badge($"🇺🇸 USD: {usd.Price:F2}฿"));

            liveSp.Children.Add(liveRow);
            liveCard.Child = liveSp;
            panelAIInvest.Children.Add(liveCard);
        }

        // Default: show moderate
        ShowPortfolio("moderate");
    }

    private void ShowPortfolio(string riskProfile)
    {
        // Remove old portfolio cards (keep health + buttons = first 2)
        while (panelAIInvest.Children.Count > 2) panelAIInvest.Children.RemoveAt(2);

        var profileName = riskProfile switch { "conservative" => "🛡️ ระมัดระวัง", "aggressive" => "🚀 เชิงรุก", _ => "⚖️ สมดุล" };
        var plans = InvestmentAdvisor.RecommendPortfolio(_data, riskProfile);
        var totalBalance = _data.Accounts.Sum(a => a.Balance);
        var thisMonth = DateTime.Today.ToString("yyyy-MM");
        var monthExpense = _data.Transactions.Where(t => t.Date.StartsWith(thisMonth) && t.Type == "expense").Sum(t => t.Amount);
        var emergency = monthExpense > 0 ? monthExpense * 6 : totalBalance * 0.3;
        var investable = Math.Max(totalBalance - emergency, 0);

        // Portfolio header
        var portCard = MakeCard($"📋 พอร์ตแนะนำ — {profileName}");
        var pSp = (StackPanel)portCard.Child;
        pSp.Children.Add(Txt($"💰 เงินรวม: ฿{totalBalance:N0}  |  🛡️ เงินสำรอง: ฿{emergency:N0}  |  📊 ลงทุนได้: ฿{investable:N0}", 12, "#8b8fa7"));
        var expectedReturn = plans.Sum(p => p.ExpectedReturn * p.AllocationPercent / 100);
        pSp.Children.Add(Txt($"📈 ผลตอบแทนคาดหวังเฉลี่ย: {expectedReturn:F1}% ต่อปี  (~฿{investable * expectedReturn / 100:N0}/ปี)", 12, "#27ae60"));
        portCard.Margin = new Thickness(0, 0, 0, 8);
        panelAIInvest.Children.Add(portCard);

        // Allocation pie + details
        foreach (var plan in plans.OrderByDescending(p => p.AllocationPercent))
        {
            var amount = investable * plan.AllocationPercent / 100;
            var card = new Border { Background = B("#161828"), CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 6),
                BorderBrush = B(plan.RiskColor), BorderThickness = new Thickness(2, 0, 0, 0) };
            var sp = new StackPanel();

            var topRow = new DockPanel();
            topRow.Children.Add(Txt($"{plan.Icon} {plan.Name}", 14, "#e8e9f0", true));
            var rightSp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            rightSp.Children.Add(Badge($"{plan.AllocationPercent:F0}%"));
            rightSp.Children.Add(Txt($"฿{amount:N0}", 13, plan.RiskColor, true));
            DockPanel.SetDock(rightSp, Dock.Right);
            topRow.Children.Insert(0, rightSp);
            sp.Children.Add(topRow);

            // Bar
            var barG = new Grid { Height = 8, Margin = new Thickness(0, 6, 0, 4) };
            barG.Children.Add(new Border { Background = B("#2a2d45"), CornerRadius = new CornerRadius(4) });
            barG.Children.Add(new Border { Background = B(plan.RiskColor), CornerRadius = new CornerRadius(4),
                HorizontalAlignment = HorizontalAlignment.Left, Width = Math.Max(plan.AllocationPercent * 4, 4) });
            sp.Children.Add(barG);

            var meta = new WrapPanel();
            meta.Children.Add(Badge($"ความเสี่ยง: {plan.RiskLevel}"));
            meta.Children.Add(Badge($"ผลตอบแทน: ~{plan.ExpectedReturn:F1}%/ปี"));
            sp.Children.Add(meta);

            sp.Children.Add(Txt(plan.Description, 11, "#8b8fa7"));
            sp.Children.Add(Txt($"✅ ข้อดี: {plan.Pros}", 11, "#27ae60"));
            sp.Children.Add(Txt($"⚠️ ข้อเสีย: {plan.Cons}", 11, "#e74c3c"));
            sp.Children.Add(Txt($"👤 เหมาะกับ: {plan.Suitable}", 11, "#0984e3"));

            card.Child = sp;
            panelAIInvest.Children.Add(card);
        }

        // Growth projection
        var monthlyAdd = _data.Transactions.Any(t => t.Type == "income")
            ? _data.Transactions.Where(t => t.Type == "income").Average(t => t.Amount) * 0.2 : 5000;
        var projections = InvestmentAdvisor.ProjectGrowth(investable, monthlyAdd, 20);

        var projCard = MakeCard($"📈 คาดการณ์เงินเติบโต (เริ่มต้น ฿{investable:N0} + ออมเพิ่ม ฿{monthlyAdd:N0}/เดือน)");
        var projSp = (StackPanel)projCard.Child;

        var milestones = new[] { 1, 3, 5, 10, 15, 20 };
        var headerRow = new Grid();
        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerRow.Children.Add(Txt("ปี", 11, "#8b8fa7", true));
        var h1 = Txt("🛡️ ระวัง", 11, "#27ae60", true); Grid.SetColumn(h1, 1); headerRow.Children.Add(h1);
        var h2 = Txt("⚖️ สมดุล", 11, "#f39c12", true); Grid.SetColumn(h2, 2); headerRow.Children.Add(h2);
        var h3 = Txt("🚀 เชิงรุก", 11, "#e74c3c", true); Grid.SetColumn(h3, 3); headerRow.Children.Add(h3);
        projSp.Children.Add(headerRow);

        foreach (var y in milestones)
        {
            var p = projections.FirstOrDefault(x => x.Year == y);
            if (p == null) continue;
            var row = new Grid { Margin = new Thickness(0, 3, 0, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.Children.Add(Txt($"{y} ปี", 12, "#e8e9f0", true));
            var c1 = Txt($"฿{p.Conservative:N0}", 12, "#27ae60"); Grid.SetColumn(c1, 1); row.Children.Add(c1);
            var c2 = Txt($"฿{p.Moderate:N0}", 12, "#f39c12"); Grid.SetColumn(c2, 2); row.Children.Add(c2);
            var c3 = Txt($"฿{p.Aggressive:N0}", 12, "#e74c3c"); Grid.SetColumn(c3, 3); row.Children.Add(c3);

            var bg = new Border { Background = B(y % 2 == 0 ? "#161828" : "#151528"), CornerRadius = new CornerRadius(4), Padding = new Thickness(4, 2, 4, 2) };
            bg.Child = row;
            projSp.Children.Add(bg);
        }

        var last = projections.Last();
        projSp.Children.Add(Txt($"\n💡 ถ้าลงทุน 20 ปี เงินจะโตจาก ฿{investable:N0} เป็น:", 13, "#e8e9f0", true));
        projSp.Children.Add(Txt($"   🛡️ ระวัง: ฿{last.Conservative:N0} (+{(last.Conservative / Math.Max(investable, 1) - 1) * 100:F0}%)", 13, "#27ae60"));
        projSp.Children.Add(Txt($"   ⚖️ สมดุล: ฿{last.Moderate:N0} (+{(last.Moderate / Math.Max(investable, 1) - 1) * 100:F0}%)", 13, "#f39c12"));
        projSp.Children.Add(Txt($"   🚀 เชิงรุก: ฿{last.Aggressive:N0} (+{(last.Aggressive / Math.Max(investable, 1) - 1) * 100:F0}%)", 13, "#e74c3c"));

        projSp.Children.Add(Txt("\n⚠️ หมายเหตุ: ตัวเลขเป็นการคาดการณ์จากผลตอบแทนเฉลี่ยในอดีต ผลตอบแทนจริงอาจแตกต่าง\n" +
            "การลงทุนมีความเสี่ยง ผู้ลงทุนควรศึกษาข้อมูลก่อนตัดสินใจ", 10, "#5a5d70"));

        projCard.Margin = new Thickness(0, 12, 0, 0);
        panelAIInvest.Children.Add(projCard);
    }

    // ====================================================
    // Tab 4: บัญชีธนาคาร
    // ====================================================
    private void RenderBanks()
    {
        panelBanks.Children.Clear();
        panelBanks.Children.Add(Txt("🏦 บัญชีธนาคาร", 16, "#e8e9f0", true));

        var total = _data.Accounts.Sum(a => a.Balance);
        panelBanks.Children.Add(Txt($"ยอดรวมทั้งหมด: ฿{total:N0}", 13, "#0984e3"));

        var addBtn = MakeBtn("+ เพิ่มบัญชี", "#0984e3");
        addBtn.Margin = new Thickness(0, 8, 0, 12);
        addBtn.Click += (_, _) => AddBankAccount();
        panelBanks.Children.Add(addBtn);

        foreach (var acc in _data.Accounts)
        {
            var card = new Border { Background = B("#161828"), CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14), Margin = new Thickness(0, 0, 0, 8),
                BorderBrush = B(acc.Color), BorderThickness = new Thickness(0, 0, 0, 3) };

            var sp = new StackPanel();
            var topRow = new DockPanel();
            topRow.Children.Add(Txt($"{acc.Icon} {acc.BankName}", 15, acc.Color, true));
            var balTxt = Txt($"฿{acc.Balance:N0}", 18, acc.Color, true);
            balTxt.HorizontalAlignment = HorizontalAlignment.Right;
            DockPanel.SetDock(balTxt, Dock.Right);
            topRow.Children.Insert(0, balTxt);
            sp.Children.Add(topRow);

            var meta = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
            meta.Children.Add(Badge($"👤 {acc.AccountName}"));
            if (!string.IsNullOrEmpty(acc.AccountNumber))
                meta.Children.Add(Badge($"📋 {MaskAccount(acc.AccountNumber)}"));
            sp.Children.Add(meta);

            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            var accRef = acc;
            var editBtn = MakeBtn("✏️ แก้ไขยอด", "#f39c12");
            editBtn.Click += (_, _) =>
            {
                var dlg = new InputDialog("แก้ไขยอด", $"ยอดเงินปัจจุบันของ {accRef.BankName}:") { Owner = this };
                dlg.SetInitialText(accRef.Balance.ToString("F0"));
                if (dlg.ShowDialog() == true && double.TryParse(dlg.Answer, out var newBal))
                {
                    accRef.Balance = newBal;
                    FinanceStore.Save(_data);
                    Refresh();
                }
            };
            btnRow.Children.Add(editBtn);

            var delBtn = MakeBtn("🗑️ ลบ", "#e74c3c");
            delBtn.Margin = new Thickness(6, 0, 0, 0);
            delBtn.Click += (_, _) =>
            {
                if (MessageBox.Show($"ลบบัญชี \"{accRef.BankName}\"?", "ยืนยัน", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _data.Accounts.Remove(accRef); FinanceStore.Save(_data); Refresh();
                }
            };
            btnRow.Children.Add(delBtn);
            sp.Children.Add(btnRow);

            card.Child = sp;
            panelBanks.Children.Add(card);
        }

        // PromptPay / QR info
        var infoCard = MakeCard("💡 เชื่อมกับธนาคาร");
        var infoSp = (StackPanel)infoCard.Child;
        infoSp.Children.Add(new TextBlock
        {
            Text = "📱 รองรับธนาคารไทย:\n" +
                   "• กสิกร, SCB, กรุงเทพ, กรุงไทย, ttb, กรุงศรี, ออมสิน\n" +
                   "• บันทึกยอดเงินจากแอปธนาคาร → อัพเดทในแอป\n" +
                   "• ติดตามรายรับ-รายจ่ายแยกบัญชี\n" +
                   "• แจ้งเตือนบิลที่ต้องจ่ายก่อนถึงกำหนด\n\n" +
                   "🔐 ข้อมูลเก็บในเครื่องเท่านั้น ไม่ส่งออกไปที่ไหน",
            FontSize = 12, Foreground = B("#8b8fa7"), TextWrapping = TextWrapping.Wrap, LineHeight = 20
        });
        infoCard.Margin = new Thickness(0, 12, 0, 0);
        panelBanks.Children.Add(infoCard);
    }

    private void AddBankAccount()
    {
        var win = new Window
        {
            Title = "เพิ่มบัญชีธนาคาร", Width = 380, Height = 350,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = this,
            Background = B("#0f1018")
        };
        var sp = new StackPanel { Margin = new Thickness(16) };
        sp.Children.Add(Txt("🏦 เพิ่มบัญชีธนาคาร", 16, "#e8e9f0", true));

        sp.Children.Add(Txt("ธนาคาร", 12, "#8b8fa7"));
        var cmbBank = new ComboBox { FontSize = 13, Padding = new Thickness(6), Margin = new Thickness(0, 4, 0, 0) };
        cmbBank.ItemsSource = Banks.Select(b => $"{b.Icon} {b.Name}").ToList();
        cmbBank.SelectedIndex = 0;
        sp.Children.Add(cmbBank);

        sp.Children.Add(Txt("ชื่อบัญชี", 12, "#8b8fa7"));
        var txtName = new TextBox { FontSize = 13, Padding = new Thickness(6), Background = B("#2a2d45"), Foreground = B("#e8e9f0"), BorderBrush = B("#2a2d45"), Margin = new Thickness(0, 4, 0, 0) };
        sp.Children.Add(txtName);

        sp.Children.Add(Txt("เลขบัญชี", 12, "#8b8fa7"));
        var txtNum = new TextBox { FontSize = 13, Padding = new Thickness(6), Background = B("#2a2d45"), Foreground = B("#e8e9f0"), BorderBrush = B("#2a2d45"), Margin = new Thickness(0, 4, 0, 0) };
        sp.Children.Add(txtNum);

        sp.Children.Add(Txt("ยอดเงินปัจจุบัน (บาท)", 12, "#8b8fa7"));
        var txtBal = new TextBox { FontSize = 13, Padding = new Thickness(6), Background = B("#2a2d45"), Foreground = B("#e8e9f0"), BorderBrush = B("#2a2d45"), Margin = new Thickness(0, 4, 0, 0) };
        sp.Children.Add(txtBal);

        var saveBtn = MakeBtn("💾 บันทึก", "#0984e3");
        saveBtn.Margin = new Thickness(0, 12, 0, 0);
        saveBtn.Click += (_, _) =>
        {
            if (!double.TryParse(txtBal.Text, out var bal)) { MessageBox.Show("กรุณาใส่ยอดเงิน"); return; }
            var bank = Banks[cmbBank.SelectedIndex];
            _data.Accounts.Add(new BankAccount
            {
                BankName = bank.Name, Icon = bank.Icon, Color = bank.Color,
                AccountName = txtName.Text.Trim(), AccountNumber = txtNum.Text.Trim(), Balance = bal,
            });
            FinanceStore.Save(_data);
            Refresh();
            win.Close();
        };
        sp.Children.Add(saveBtn);
        win.Content = sp;
        win.ShowDialog();
    }

    // ====================================================
    // Helpers
    // ====================================================
    private void AddKPI(Grid grid, int col, string icon, string value, string label, string color)
    {
        var card = new Border { Background = B("#161828"), CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10, 8, 10, 8), BorderBrush = B(color), BorderThickness = new Thickness(0, 0, 0, 3) };
        var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        sp.Children.Add(Txt(icon, 20, color));
        var valTxt = Txt(value, 20, color, true);
        valTxt.HorizontalAlignment = HorizontalAlignment.Center;
        sp.Children.Add(valTxt);
        sp.Children.Add(Txt(label, 10, "#8b8fa7"));
        card.Child = sp;
        Grid.SetColumn(card, col);
        grid.Children.Add(card);
    }

    private static string MaskAccount(string num) =>
        num.Length > 4 ? new string('*', num.Length - 4) + num[^4..] : num;

    private static Border MakeCard(string title)
    {
        var card = new Border { Background = B("#161828"), CornerRadius = new CornerRadius(12), Padding = new Thickness(14) };
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock { Text = title, FontSize = 14, FontWeight = FontWeights.Bold, Foreground = B("#e8e9f0"), Margin = new Thickness(0, 0, 0, 8) });
        card.Child = sp;
        return card;
    }

    private static Border Badge(string text) => new()
    {
        Background = B("#2a2d45"), CornerRadius = new CornerRadius(6),
        Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(0, 0, 6, 0),
        Child = new TextBlock { Text = text, FontSize = 10, Foreground = B("#8b8fa7") }
    };

    private static Button MakeBtn(string text, string bg) => new()
    {
        Content = text, Padding = new Thickness(10, 5, 10, 5),
        FontSize = 11, Background = B(bg), Foreground = Brushes.White,
        BorderThickness = new Thickness(0), Cursor = Cursors.Hand
    };

    private static TextBlock Txt(string text, double size, string color, bool bold = false) => new()
    {
        Text = text, FontSize = size, Foreground = B(color),
        FontWeight = bold ? FontWeights.Bold : FontWeights.Normal, Margin = new Thickness(0, 2, 0, 0)
    };

    private static SolidColorBrush B(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
}
