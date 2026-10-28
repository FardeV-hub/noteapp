using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace NoteApp;

public static class AuthGuard
{
    private static readonly string AuthFile = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "auth.dat");
    private static DateTime _lastAuth = DateTime.MinValue;
    private static readonly int SessionMinutes = 15;

    public static bool HasAuth() => File.Exists(AuthFile) && File.ReadAllText(AuthFile).Trim().Length > 0;

    public static void SetupAuth(string password)
    {
        var hash = HashPassword(password);
        File.WriteAllText(AuthFile, hash);
    }

    public static bool Verify(string password)
    {
        if (!HasAuth()) return true;
        var saved = File.ReadAllText(AuthFile).Trim();
        if (HashPassword(password) == saved)
        {
            _lastAuth = DateTime.Now;
            return true;
        }
        return false;
    }

    public static bool IsSessionValid() =>
        (DateTime.Now - _lastAuth).TotalMinutes < SessionMinutes;

    public static bool RequireAuth(string section)
    {
        if (!HasAuth()) return true;
        if (IsSessionValid()) return true;

        var dlg = new AuthDialog(section);
        dlg.ShowDialog();
        return dlg.Authenticated;
    }

    private static string HashPassword(string pw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(pw + "NoteApp_Salt_2024"));
        return Convert.ToBase64String(bytes);
    }
}

public class AuthDialog : Window
{
    private readonly System.Windows.Controls.PasswordBox _pwBox;
    public bool Authenticated { get; private set; }

    public AuthDialog(string section)
    {
        Title = "🔐 ยืนยันตัวตน";
        Width = 360; Height = 240;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        Background = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0f1018"));

        var sp = new System.Windows.Controls.StackPanel { Margin = new Thickness(24) };

        sp.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = "🔐", FontSize = 36, HorizontalAlignment = HorizontalAlignment.Center
        });
        sp.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = $"ยืนยันตัวตนเพื่อเข้า\n{section}",
            FontSize = 13, Foreground = System.Windows.Media.Brushes.Gray,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 4, 0, 12)
        });

        _pwBox = new System.Windows.Controls.PasswordBox
        {
            FontSize = 16, Padding = new Thickness(8), MaxLength = 32,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        sp.Children.Add(_pwBox);

        var btn = new System.Windows.Controls.Button
        {
            Content = "ยืนยัน", Padding = new Thickness(20, 8, 20, 8),
            FontSize = 14, Margin = new Thickness(0, 12, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        btn.Click += (_, _) =>
        {
            if (AuthGuard.Verify(_pwBox.Password))
            {
                Authenticated = true;
                Close();
            }
            else
            {
                MessageBox.Show("รหัสผ่านไม่ถูกต้อง", "❌");
                _pwBox.Clear();
                _pwBox.Focus();
            }
        };
        sp.Children.Add(btn);

        Content = sp;
        Loaded += (_, _) => _pwBox.Focus();
        KeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Enter) btn.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent)); };
    }
}
