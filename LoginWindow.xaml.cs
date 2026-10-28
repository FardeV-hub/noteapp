using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace NoteApp;

public class UserAccount
{
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Created { get; set; } = "";
}

public partial class LoginWindow : Window
{
    private static readonly string UsersFile = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "users.json");
    private bool _isSignup;
    public bool Authenticated { get; private set; }
    public string CurrentUser { get; private set; } = "";

    public LoginWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => txtUsername.Focus();
    }

    // ====================================================
    // Tab switching
    // ====================================================
    private void TabLogin_Click(object sender, RoutedEventArgs e) => SetMode(false);
    private void TabSignup_Click(object sender, RoutedEventArgs e) => SetMode(true);

    private void SetMode(bool signup)
    {
        _isSignup = signup;
        panelConfirm.Visibility = signup ? Visibility.Visible : Visibility.Collapsed;
        btnAction.Content = signup ? "สมัครสมาชิก" : "เข้าสู่ระบบ";
        lblMessage.Text = "";

        // Update tab visuals
        tabLogin.Template = CreateTabTemplate(!signup);
        tabSignup.Template = CreateTabTemplate(signup);
        tabLogin.Foreground = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(signup ? "#8b8fa7" : "White"));
        tabSignup.Foreground = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(signup ? "White" : "#8b8fa7"));
    }

    private System.Windows.Controls.ControlTemplate CreateTabTemplate(bool active)
    {
        var bg = active ? "#6c63ff" : "#2a2d45";
        var template = new System.Windows.Controls.ControlTemplate(typeof(System.Windows.Controls.Button));
        var border = new FrameworkElementFactory(typeof(System.Windows.Controls.Border));
        border.SetValue(System.Windows.Controls.Border.BackgroundProperty,
            new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(bg)));
        border.SetValue(System.Windows.Controls.Border.CornerRadiusProperty, new CornerRadius(10, 10, 10, 10));
        border.SetValue(System.Windows.Controls.Border.PaddingProperty, new Thickness(0, 10, 0, 10));
        var cp = new FrameworkElementFactory(typeof(System.Windows.Controls.ContentPresenter));
        cp.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        cp.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(cp);
        template.VisualTree = border;
        return template;
    }

    // ====================================================
    // Actions
    // ====================================================
    private void Action_Click(object sender, RoutedEventArgs e) => DoAction();
    private void Window_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) DoAction(); }
    private void Drag_MouseDown(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) DragMove(); }
    private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

    private void DoAction()
    {
        var username = txtUsername.Text.Trim();
        var password = txtPassword.Password;

        if (string.IsNullOrEmpty(username)) { ShowError("กรุณาใส่ชื่อผู้ใช้"); return; }
        if (string.IsNullOrEmpty(password)) { ShowError("กรุณาใส่รหัสผ่าน"); return; }
        if (password.Length < 4) { ShowError("รหัสผ่านต้องอย่างน้อย 4 ตัวอักษร"); return; }

        var users = LoadUsers();

        if (_isSignup)
        {
            if (users.Exists(u => u.Username.ToLower() == username.ToLower()))
            {
                ShowError("ชื่อผู้ใช้นี้มีแล้ว ลองชื่ออื่น");
                return;
            }
            if (txtConfirm.Password != password)
            {
                ShowError("รหัสผ่านไม่ตรงกัน");
                return;
            }

            users.Add(new UserAccount
            {
                Username = username,
                PasswordHash = Hash(password),
                Created = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            });
            SaveUsers(users);
            CurrentUser = username;
            Authenticated = true;
            Close();
        }
        else
        {
            var user = users.Find(u => u.Username.ToLower() == username.ToLower());
            if (user == null) { ShowError("ไม่พบชื่อผู้ใช้นี้"); return; }
            if (user.PasswordHash != Hash(password)) { ShowError("รหัสผ่านไม่ถูกต้อง"); txtPassword.Clear(); return; }

            CurrentUser = user.Username;
            Authenticated = true;
            Close();
        }
    }

    private void Skip_Click(object sender, RoutedEventArgs e)
    {
        CurrentUser = "Guest";
        Authenticated = true;
        Close();
    }

    private void ShowError(string msg)
    {
        lblMessage.Text = $"❌ {msg}";
    }

    // ====================================================
    // User storage
    // ====================================================
    private static List<UserAccount> LoadUsers()
    {
        if (!File.Exists(UsersFile)) return new List<UserAccount>();
        try { return JsonSerializer.Deserialize<List<UserAccount>>(File.ReadAllText(UsersFile)) ?? new(); }
        catch { return new(); }
    }

    private static void SaveUsers(List<UserAccount> users)
    {
        File.WriteAllText(UsersFile, JsonSerializer.Serialize(users,
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string Hash(string pw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(pw + "NoteApp_2024"));
        return Convert.ToBase64String(bytes);
    }

    public static bool HasUsers() => File.Exists(UsersFile) && File.ReadAllText(UsersFile).Contains("Username");
    public static bool HasPIN() => HasUsers();
}
