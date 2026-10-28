using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NoteApp;

public partial class AccountsWindow : Window
{
    private AllAccounts _data;

    public AccountsWindow()
    {
        InitializeComponent();
        _data = AccountsStore.Load();
        Render();
    }

    private void Render()
    {
        panelServices.Children.Clear();
        var connectedCount = _data.Services.Count(s => s.Value.Connected);
        lblStatus.Text = $"เชื่อมต่อแล้ว {connectedCount}/{AccountsStore.ServiceDefs.Length} บริการ";

        // Group by category
        var groups = new (string Title, int From, int To)[]
        {
            ("💬 แชท / แจ้งเตือน", 0, 3),
            ("📅 Google Services", 3, 5),
            ("🤖 AI Providers", 5, 9),
            ("🏠 Smart Home", 9, 11),
        };

        foreach (var (title, from, to) in groups)
        {
            panelServices.Children.Add(new TextBlock
            {
                Text = title, FontSize = 14, FontWeight = FontWeights.Bold,
                Foreground = B("#8b8fa7"), Margin = new Thickness(4, 12, 0, 6)
            });

            for (int i = from; i < to && i < AccountsStore.ServiceDefs.Length; i++)
            {
                var def = AccountsStore.ServiceDefs[i];
                var acc = _data.Services.GetValueOrDefault(def.Key) ?? new ConnectedAccount
                {
                    Service = def.Key, Icon = def.Icon, Color = def.Color
                };

                var card = new Border
                {
                    Background = B("#161828"), CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(14), Margin = new Thickness(0, 0, 0, 6),
                    BorderBrush = B(acc.Connected ? "#27ae60" : "#2a2d45"),
                    BorderThickness = new Thickness(acc.Connected ? 2 : 1)
                };

                var sp = new StackPanel();

                // Header row
                var topRow = new DockPanel();
                var namePanel = new StackPanel { Orientation = Orientation.Horizontal };
                namePanel.Children.Add(new TextBlock { Text = def.Icon, FontSize = 18, Margin = new Thickness(0, 0, 8, 0) });
                namePanel.Children.Add(new TextBlock { Text = def.Name, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = B("#e8e9f0"), VerticalAlignment = VerticalAlignment.Center });
                topRow.Children.Add(namePanel);

                var statusBadge = new Border
                {
                    Background = B(acc.Connected ? "#27ae6033" : "#e74c3c33"),
                    CornerRadius = new CornerRadius(10), Padding = new Thickness(8, 2, 8, 2),
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                statusBadge.Child = new TextBlock
                {
                    Text = acc.Connected ? "✅ เชื่อมต่อแล้ว" : "❌ ยังไม่เชื่อม",
                    FontSize = 10, Foreground = B(acc.Connected ? "#27ae60" : "#e74c3c")
                };
                DockPanel.SetDock(statusBadge, Dock.Right);
                topRow.Children.Insert(0, statusBadge);
                sp.Children.Add(topRow);

                // Fields
                var fields = new Dictionary<string, TextBox>();
                foreach (var (fieldId, label, hint) in def.Fields)
                {
                    sp.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = B("#8b8fa7"), Margin = new Thickness(0, 8, 0, 2) });

                    var tb = new PasswordBox
                    {
                        FontSize = 12, Padding = new Thickness(8), MaxLength = 200,
                        Background = B("#2a2d45"), Foreground = B("#e8e9f0"), BorderBrush = B("#2a2d45")
                    };
                    if (acc.Credentials.TryGetValue(fieldId, out var val))
                        tb.Password = val;

                    sp.Children.Add(tb);
                    sp.Children.Add(new TextBlock { Text = hint, FontSize = 9, Foreground = B("#444466"), TextWrapping = TextWrapping.Wrap });

                    // Store ref for save
                    var fieldKey = $"{def.Key}_{fieldId}";
                    tb.Tag = fieldKey;
                }

                // Buttons
                var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };

                var saveBtn = new Button
                {
                    Content = acc.Connected ? "💾 อัพเดท" : "🔗 เชื่อมต่อ",
                    Padding = new Thickness(14, 6, 14, 6), FontSize = 12,
                    Background = B(def.Color), Foreground = Brushes.White,
                    BorderThickness = new Thickness(0), Cursor = Cursors.Hand
                };
                var defRef = def;
                saveBtn.Click += (_, _) =>
                {
                    var newAcc = new ConnectedAccount
                    {
                        Service = defRef.Key, Icon = defRef.Icon, Color = defRef.Color,
                        Connected = true, ConnectedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                        Credentials = new()
                    };
                    foreach (var child in sp.Children)
                    {
                        if (child is PasswordBox pb && pb.Tag is string tag && tag.StartsWith(defRef.Key + "_"))
                        {
                            var fId = tag.Replace(defRef.Key + "_", "");
                            newAcc.Credentials[fId] = pb.Password;
                            if (string.IsNullOrWhiteSpace(pb.Password)) newAcc.Connected = false;
                        }
                    }
                    _data.Services[defRef.Key] = newAcc;
                    AccountsStore.Save(_data);
                    AccountsStore.SyncToOldConfigs(_data);
                    Render();
                    MessageBox.Show($"{defRef.Icon} {defRef.Name} — {(newAcc.Connected ? "เชื่อมต่อสำเร็จ!" : "บันทึกแล้ว (ยังไม่ครบ)")}", "🔗");
                };
                btnRow.Children.Add(saveBtn);

                if (acc.Connected)
                {
                    var disconnBtn = new Button
                    {
                        Content = "🔌 ยกเลิก", Padding = new Thickness(10, 6, 10, 6),
                        FontSize = 11, Background = B("#e74c3c33"), Foreground = B("#e74c3c"),
                        BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
                        Margin = new Thickness(8, 0, 0, 0)
                    };
                    disconnBtn.Click += (_, _) =>
                    {
                        _data.Services.Remove(defRef.Key);
                        AccountsStore.Save(_data);
                        AccountsStore.SyncToOldConfigs(_data);
                        Render();
                    };
                    btnRow.Children.Add(disconnBtn);
                }

                sp.Children.Add(btnRow);

                if (acc.Connected && !string.IsNullOrEmpty(acc.ConnectedAt))
                    sp.Children.Add(new TextBlock { Text = $"เชื่อมต่อเมื่อ: {acc.ConnectedAt}", FontSize = 9, Foreground = B("#444466"), Margin = new Thickness(0, 4, 0, 0) });

                card.Child = sp;
                panelServices.Children.Add(card);
            }
        }
    }

    private static SolidColorBrush B(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
}
