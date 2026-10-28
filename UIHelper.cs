using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NoteApp;

public static class UI
{
    public static Button Pill(string text, string bg, double fontSize = 12, double padX = 14, double padY = 7)
    {
        var btn = new Button { Cursor = System.Windows.Input.Cursors.Hand, BorderThickness = new Thickness(0), Margin = new Thickness(2) };
        var border = new Border
        {
            Background = Br(bg), CornerRadius = new CornerRadius(6),
            Padding = new Thickness(padX, padY, padX, padY)
        };
        border.Child = new TextBlock
        {
            Text = text, FontSize = fontSize, FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center
        };
        btn.Content = border;
        btn.Template = TransparentTemplate();
        return btn;
    }

    public static Button SmallBtn(string text, string bg)
    {
        return Pill(text, bg, 11, 10, 5);
    }

    public static Border GradientHeader(string emoji, string title, string subtitle, string color1 = "#161828", string color2 = "#1c1e35")
    {
        var border = new Border { CornerRadius = new CornerRadius(6), Margin = new Thickness(0, 0, 0, 8) };
        var gradient = new LinearGradientBrush();
        gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(color1), 0));
        gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(color2), 1));
        border.Background = gradient;

        var grid = new Grid { Margin = new Thickness(16, 12, 16, 12) };
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock { Text = title, FontSize = 18, FontWeight = FontWeights.Bold, Foreground = Br("#e8e9f0") });
        sp.Children.Add(new TextBlock { Text = subtitle, FontSize = 11, Foreground = Br("#5a5d78"), Margin = new Thickness(0, 3, 0, 0) });
        grid.Children.Add(sp);

        border.Child = grid;
        return border;
    }

    public static Border Card(string bg = "#161828", string borderColor = "", int radius = 10)
    {
        var b = new Border
        {
            Background = Br(bg), CornerRadius = new CornerRadius(radius),
            Padding = new Thickness(14), Margin = new Thickness(0, 0, 0, 8)
        };
        if (!string.IsNullOrEmpty(borderColor))
        {
            b.BorderBrush = Br(borderColor);
            b.BorderThickness = new Thickness(0, 0, 0, 3);
        }
        return b;
    }

    public static Border SectionTitle(string text, string color = "#5a5d78")
    {
        var border = new Border
        {
            Background = Br("#1c1e35"), CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 5, 10, 5), Margin = new Thickness(0, 10, 0, 6)
        };
        border.Child = new TextBlock { Text = text, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = Br(color) };
        return border;
    }

    public static Border Badge(string text, string bg = "#2a2d45", string fg = "#8b8fa7")
    {
        return new Border
        {
            Background = Br(bg), CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8, 2, 8, 2), Margin = new Thickness(0, 0, 6, 0),
            Child = new TextBlock { Text = text, FontSize = 10, Foreground = Br(fg) }
        };
    }

    public static TextBlock Text(string text, double size, string color, bool bold = false) => new()
    {
        Text = text, FontSize = size, Foreground = Br(color),
        FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
        TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0)
    };

    public static Border StatusBadge(bool connected)
    {
        var bg = connected ? "#30a46c22" : "#e5484d22";
        var fg = connected ? "#30a46c" : "#e5484d";
        var text = connected ? "เชื่อมต่อ" : "ไม่ได้เชื่อม";
        return new Border
        {
            Background = Br(bg), CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8, 2, 8, 2),
            Child = new TextBlock { Text = text, FontSize = 10, Foreground = Br(fg) }
        };
    }

    public static SolidColorBrush Br(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex));

    private static ControlTemplate TransparentTemplate()
    {
        var t = new ControlTemplate(typeof(Button));
        var cp = new FrameworkElementFactory(typeof(ContentPresenter));
        t.VisualTree = cp;
        return t;
    }
}
