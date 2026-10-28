using System.Windows;

namespace NoteApp;

public partial class App : Application
{
    public static string CurrentUser { get; set; } = "Guest";

    private void App_Startup(object sender, StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        try
        {
            // 1. Splash
            new SplashWindow().ShowDialog();

            // 2. Login
            var login = new LoginWindow();
            login.ShowDialog();
            if (!login.Authenticated) { Shutdown(); return; }
            CurrentUser = login.CurrentUser;

            // 3. Main
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            var main = new MainWindow();
            MainWindow = main;
            main.Show();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error:\n{ex.Message}\n\n{ex.StackTrace}", "Error");
            Shutdown();
        }
    }
}
