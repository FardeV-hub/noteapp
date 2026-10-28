using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace NoteApp;

public partial class App : Application
{
    public static string CurrentUser { get; set; } = "Guest";

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    public static void ApplyDarkTitleBar(Window window)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).EnsureHandle();
            int value = 1;
            DwmSetWindowAttribute(hwnd, 20, ref value, sizeof(int));
        }
        catch { }
    }

    private void App_Startup(object sender, StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Dark title bar for all windows
        EventManager.RegisterClassHandler(typeof(Window),
            Window.LoadedEvent, new RoutedEventHandler((s, _) =>
            {
                if (s is Window w) ApplyDarkTitleBar(w);
            }));

        try
        {
            new SplashWindow().ShowDialog();

            var login = new LoginWindow();
            login.ShowDialog();
            if (!login.Authenticated) { Shutdown(); return; }
            CurrentUser = login.CurrentUser;

            ShutdownMode = ShutdownMode.OnMainWindowClose;
            var main = new MainWindow();
            MainWindow = main;
            main.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error:\n{ex.Message}\n\n{ex.StackTrace}", "Error");
            Shutdown();
        }
    }
}
