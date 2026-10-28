using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace NoteApp;

public partial class ProfileWindow : Window
{
    private UserProfileData _profile;

    public ProfileWindow()
    {
        InitializeComponent();
        _profile = UserProfileStore.Load();

        if (string.IsNullOrEmpty(_profile.DisplayName))
            _profile.DisplayName = App.CurrentUser ?? "Guest";

        txtDisplayName.Text = _profile.DisplayName;
        txtBio.Text = _profile.Bio;
        txtEmail.Text = _profile.Email;
        txtSchool.Text = _profile.School;
        txtCompany.Text = _profile.Company;
        lblJoined.Text = $"เข้าร่วมเมื่อ: {_profile.JoinedDate}";

        foreach (System.Windows.Controls.ComboBoxItem item in cmbRole.Items)
        {
            if (item.Content as string == _profile.Role)
            {
                cmbRole.SelectedItem = item;
                break;
            }
        }
        if (cmbRole.SelectedItem == null) cmbRole.SelectedIndex = 0;

        LoadAvatar();
        UpdateHeader();
    }

    private void UpdateHeader()
    {
        lblDisplayName.Text = string.IsNullOrEmpty(txtDisplayName.Text) ? "ชื่อผู้ใช้" : txtDisplayName.Text;
        lblRole.Text = (cmbRole.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content as string ?? "สมาชิก";

        var name = txtDisplayName.Text.Trim();
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
        catch (Exception ex)
        {
            MessageBox.Show($"ไม่สามารถใช้รูปนี้ได้:\n{ex.Message}");
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _profile.DisplayName = txtDisplayName.Text.Trim();
        _profile.Bio = txtBio.Text.Trim();
        _profile.Email = txtEmail.Text.Trim();
        _profile.School = txtSchool.Text.Trim();
        _profile.Company = txtCompany.Text.Trim();
        _profile.Role = (cmbRole.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content as string ?? "สมาชิก";

        UserProfileStore.Save(_profile);

        if (!string.IsNullOrEmpty(_profile.DisplayName))
            App.CurrentUser = _profile.DisplayName;

        MessageBox.Show("บันทึกโปรไฟล์สำเร็จ!", "โปรไฟล์");
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
