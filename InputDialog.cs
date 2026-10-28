using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NoteApp;

public class InputDialog : Window
{
    private readonly TextBox _textBox;
    public string Answer => _textBox.Text;

    public InputDialog(string title, string prompt)
    {
        Title = title;
        Width = 400; Height = 200;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        Topmost = true;

        var border = new Border
        {
            Background = Br("#12141e"),
            CornerRadius = new CornerRadius(12),
            BorderBrush = Br("#2a2d45"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(20),
        };

        var sp = new StackPanel();

        sp.Children.Add(new TextBlock
        {
            Text = title, FontSize = 14, FontWeight = FontWeights.SemiBold,
            Foreground = Br("#e8e9f0"), Margin = new Thickness(0, 0, 0, 6)
        });

        sp.Children.Add(new TextBlock
        {
            Text = prompt, FontSize = 12, Foreground = Br("#5a5d78"),
            Margin = new Thickness(0, 0, 0, 12), TextWrapping = TextWrapping.Wrap
        });

        _textBox = new TextBox
        {
            FontSize = 13, Padding = new Thickness(10, 8, 10, 8),
            Background = Br("#1a1c2c"), Foreground = Br("#e0e2eb"),
            BorderBrush = Br("#2a2d45"), BorderThickness = new Thickness(1),
            CaretBrush = Br("#6c63ff"),
        };
        sp.Children.Add(_textBox);

        var btnPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 14, 0, 0)
        };

        var cancel = new Button
        {
            Content = "ยกเลิก", Padding = new Thickness(16, 7, 16, 7),
            FontSize = 12, Foreground = Br("#5a5d78"),
            Background = Br("#1c1e30"), BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand, IsCancel = true
        };
        cancel.Template = RoundTemplate("#1c1e30");

        var ok = new Button
        {
            Content = "ตกลง", Padding = new Thickness(16, 7, 16, 7),
            FontSize = 12, FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White, Background = Br("#6c63ff"),
            BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
            IsDefault = true, Margin = new Thickness(8, 0, 0, 0)
        };
        ok.Template = RoundTemplate("#6c63ff");
        ok.Click += (_, _) => { DialogResult = true; };

        btnPanel.Children.Add(cancel);
        btnPanel.Children.Add(ok);
        sp.Children.Add(btnPanel);

        border.Child = sp;
        Content = border;

        MouseLeftButtonDown += (_, _) => { try { DragMove(); } catch { } };
        Loaded += (_, _) => _textBox.Focus();
    }

    public void SetInitialText(string text)
    {
        _textBox.Text = text;
        _textBox.SelectAll();
    }

    private static SolidColorBrush Br(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex));

    private static ControlTemplate RoundTemplate(string bg)
    {
        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.BackgroundProperty, Br(bg));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
        border.SetValue(Border.PaddingProperty, new Thickness(16, 7, 16, 7));
        var cp = new FrameworkElementFactory(typeof(ContentPresenter));
        cp.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        border.AppendChild(cp);
        template.VisualTree = border;
        return template;
    }
}
