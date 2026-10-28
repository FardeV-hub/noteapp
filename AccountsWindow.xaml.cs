using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace NoteApp;

public partial class AccountsWindow : Window
{
    private AllAccounts _data;
    private UserProfileData _profile;

    public AccountsWindow()
    {
        InitializeComponent();
        _data = AccountsStore.Load();
        _profile = UserProfileStore.Load();

        if (string.IsNullOrEmpty(_profile.DisplayName))
            _profile.DisplayName = App.CurrentUser ?? "Guest";

        txtDisplayName.Text = _profile.DisplayName;
        txtBio.Text = _profile.Bio;
        txtEmail.Text = _profile.Email;
        txtSchool.Text = _profile.School;
        txtCompany.Text = _profile.Company;
        lblJoined.Text = $"เข้าร่วมเมื่อ: {_profile.JoinedDate}";

        foreach (ComboBoxItem item in cmbRole.Items)
            if (item.Content as string == _profile.Role) { cmbRole.SelectedItem = item; break; }
        if (cmbRole.SelectedItem == null) cmbRole.SelectedIndex = 0;

        LoadAvatar();
        UpdateHeader();
        RenderServices();
    }

    private void UpdateHeader()
    {
        var name = txtDisplayName.Text.Trim();
        lblDisplayName.Text = string.IsNullOrEmpty(name) ? App.CurrentUser ?? "Guest" : name;
        lblRole.Text = (cmbRole.SelectedItem as ComboBoxItem)?.Content as string ?? "สมาชิก";
        if (!string.IsNullOrEmpty(name))
            avatarInitial.Text = name[..1].ToUpper();
    }

    private void LoadAvatar()
    {
        if (!string.IsNullOrEmpty(_profile.AvatarPath) && File.Exists(_profile.AvatarPath))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(_profile.AvatarPath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                avatarImage.Source = bitmap;
                avatarImage.Visibility = Visibility.Visible;
                avatarInitial.Visibility = Visibility.Collapsed;
            }
            catch { }
        }
    }

    private void ChangeAvatar_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "รูปภาพ|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
            Title = "เลือกรูปโปรไฟล์"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var savedPath = UserProfileStore.SaveAvatar(dlg.FileName);
            _profile.AvatarPath = savedPath;
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(savedPath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            avatarImage.Source = bitmap;
            avatarImage.Visibility = Visibility.Visible;
            avatarInitial.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex) { MessageBox.Show($"ไม่สามารถใช้รูปนี้ได้:\n{ex.Message}"); }
    }

    private void DisplayName_Changed(object sender, TextChangedEventArgs e) => UpdateHeader();

    private void SaveProfile_Click(object sender, RoutedEventArgs e)
    {
        _profile.DisplayName = txtDisplayName.Text.Trim();
        _profile.Bio = txtBio.Text.Trim();
        _profile.Email = txtEmail.Text.Trim();
        _profile.School = txtSchool.Text.Trim();
        _profile.Company = txtCompany.Text.Trim();
        _profile.Role = (cmbRole.SelectedItem as ComboBoxItem)?.Content as string ?? "สมาชิก";
        UserProfileStore.Save(_profile);

        if (!string.IsNullOrEmpty(_profile.DisplayName))
            App.CurrentUser = _profile.DisplayName;

        UpdateHeader();
        MessageBox.Show("บันทึกโปรไฟล์สำเร็จ!", "โปรไฟล์");
    }

    // ====================================================
    // Tab: เชื่อมต่อบัญชี
    // ====================================================
    private void RenderServices()
    {
        panelServices.Children.Clear();
        var connectedCount = _data.Services.Count(s => s.Value.Connected);

        var groups = new (string Title, int From, int To)[]
        {
            ("แชท / แจ้งเตือน", 0, 3),
            ("Google Services", 3, 5),
            ("AI Providers", 5, 9),
            ("Smart Home", 9, 11),
        };

        foreach (var (title, from, to) in groups)
        {
            panelServices.Children.Add(new TextBlock
            {
                Text = title, FontSize = 12, FontWeight = FontWeights.SemiBold,
                Foreground = B("#7d8298"), Margin = new Thickness(4, 14, 0, 6)
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
                    Background = B("#1c1f2a"), CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(14), Margin = new Thickness(0, 0, 0, 6),
                    BorderBrush = B(acc.Connected ? "#30a46c" : "#272b38"),
                    BorderThickness = new Thickness(acc.Connected ? 2 : 1)
                };

                var sp = new StackPanel();

                var topRow = new DockPanel();
                topRow.Children.Add(new TextBlock
                {
                    Text = $"{def.Icon}  {def.Name}", FontSize = 14, FontWeight = FontWeights.SemiBold,
                    Foreground = B("#d8dae2"), VerticalAlignment = VerticalAlignment.Center
                });

                var statusText = acc.Connected ? "เชื่อมต่อแล้ว" : "ยังไม่เชื่อม";
                var statusColor = acc.Connected ? "#30a46c" : "#7d8298";
                var statusBadge = new Border
                {
                    Background = B(acc.Connected ? "#30a46c20" : "#272b38"),
                    CornerRadius = new CornerRadius(8), Padding = new Thickness(8, 2, 8, 2),
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                statusBadge.Child = new TextBlock { Text = statusText, FontSize = 10, Foreground = B(statusColor) };
                DockPanel.SetDock(statusBadge, Dock.Right);
                topRow.Children.Insert(0, statusBadge);
                sp.Children.Add(topRow);

                foreach (var (fieldId, label, hint) in def.Fields)
                {
                    sp.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = B("#7d8298"), Margin = new Thickness(0, 8, 0, 2) });
                    var tb = new PasswordBox
                    {
                        FontSize = 12, Padding = new Thickness(10, 8, 10, 8),
                        Background = B("#272b38"), Foreground = B("#d8dae2"), BorderBrush = B("#303345"),
                        BorderThickness = new Thickness(1)
                    };
                    if (acc.Credentials.TryGetValue(fieldId, out var val)) tb.Password = val;
                    sp.Children.Add(tb);
                    sp.Children.Add(new TextBlock { Text = hint, FontSize = 9, Foreground = B("#5a5d70"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) });
                    tb.Tag = $"{def.Key}_{fieldId}";
                }

                var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };
                var saveBtn = new Button
                {
                    Content = acc.Connected ? "อัพเดท" : "เชื่อมต่อ",
                    Padding = new Thickness(14, 7, 14, 7), FontSize = 12, FontWeight = FontWeights.SemiBold,
                    Background = B(def.Color), Foreground = Brushes.White,
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
                    RenderServices();
                    MessageBox.Show($"{defRef.Name} — {(newAcc.Connected ? "เชื่อมต่อสำเร็จ!" : "บันทึกแล้ว")}", "บัญชี");
                };
                btnRow.Children.Add(saveBtn);

                if (acc.Connected)
                {
                    var disconnBtn = new Button
                    {
                        Content = "ยกเลิก", Padding = new Thickness(12, 7, 12, 7),
                        FontSize = 11, Background = B("#e5484d20"), Foreground = B("#e5484d"),
                        Margin = new Thickness(8, 0, 0, 0)
                    };
                    disconnBtn.Click += (_, _) =>
                    {
                        _data.Services.Remove(defRef.Key);
                        AccountsStore.Save(_data);
                        AccountsStore.SyncToOldConfigs(_data);
                        RenderServices();
                    };
                    btnRow.Children.Add(disconnBtn);
                }
                sp.Children.Add(btnRow);

                if (acc.Connected && !string.IsNullOrEmpty(acc.ConnectedAt))
                    sp.Children.Add(new TextBlock { Text = $"เชื่อมต่อเมื่อ: {acc.ConnectedAt}", FontSize = 9, Foreground = B("#5a5d70"), Margin = new Thickness(0, 6, 0, 0) });

                card.Child = sp;
                panelServices.Children.Add(card);
            }
        }
    }

    private static SolidColorBrush B(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
}
