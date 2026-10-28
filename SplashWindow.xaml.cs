using System.Windows;
using System.Windows.Threading;
using System;

namespace NoteApp;

public partial class SplashWindow : Window
{
    private readonly DispatcherTimer _timer;
    private int _step;
    private readonly string[] _messages = { "กำลังโหลดโน้ต...", "เตรียมห้องประชุม...", "เชื่อมต่อตลาด...", "เกือบเสร็จแล้ว..." };

    public SplashWindow()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (_step < _messages.Length)
        {
            lblLoading.Text = _messages[_step++];
        }
        else
        {
            _timer.Stop();
            Close();
        }
    }
}
