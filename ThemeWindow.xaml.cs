using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NoteApp;

public partial class ThemeWindow : Window
{
    public string? SelectedThemeName { get; private set; }
    private readonly string _currentTheme;

    public ThemeWindow(string currentTheme)
    {
        InitializeComponent();
        _currentTheme = currentTheme;
        lblCurrentTheme.Text = $"ธีมปัจจุบัน: {currentTheme}";
        Background = Brush(ThemeManager.GetTheme(currentTheme).Bg);
        RenderThemes();
    }

    private void RenderThemes()
    {
        // Dark themes
        AddSectionLabel("ธีมมืด");
        foreach (var t in ThemeManager.Themes)
            if (t.IsDark) AddThemeCard(t);

        AddSectionLabel("ธีมสว่าง");
        foreach (var t in ThemeManager.Themes)
            if (!t.IsDark) AddThemeCard(t);
    }

    private void AddSectionLabel(string text)
    {
        panelThemes.Children.Add(new TextBlock
        {
            Text = text.ToUpper(), FontSize = 11, FontWeight = FontWeights.SemiBold,
            Foreground = Brush("#5a5d78"), Margin = new Thickness(4, 16, 0, 8)
        });
    }

    private void AddThemeCard(AppTheme theme)
    {
        var isActive = theme.Name == _currentTheme;

        var card = new Border
        {
            Background = Brush(theme.Surface),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 8),
            Cursor = Cursors.Hand,
            BorderThickness = new Thickness(isActive ? 2 : 1),
            BorderBrush = Brush(isActive ? theme.Accent : theme.Surface2)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Color preview circles
        var colors = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) };
        colors.Children.Add(ColorCircle(theme.Bg, 20));
        colors.Children.Add(ColorCircle(theme.Surface, 20));
        colors.Children.Add(ColorCircle(theme.Accent, 20));
        colors.Children.Add(ColorCircle(theme.Text, 16));
        Grid.SetColumn(colors, 0);
        grid.Children.Add(colors);

        // Name + description
        var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
        titleRow.Children.Add(new TextBlock { Text = $"{theme.Icon} {theme.Name}", FontSize = 14,
            FontWeight = FontWeights.SemiBold, Foreground = Brush(theme.Text) });
        if (isActive)
            titleRow.Children.Add(new TextBlock { Text = "  ✓ ใช้อยู่", FontSize = 11,
                Foreground = Brush(theme.Accent), VerticalAlignment = VerticalAlignment.Center });
        info.Children.Add(titleRow);

        info.Children.Add(new TextBlock { Text = theme.IsDark ? "ธีมมืด" : "ธีมสว่าง",
            FontSize = 11, Foreground = Brush(theme.Text2) });

        // Mini preview bar
        var previewBar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
        var miniCard = new Border
        {
            Background = Brush(theme.Surface2), CornerRadius = new CornerRadius(4),
            Width = 120, Height = 24, Margin = new Thickness(0, 0, 6, 0)
        };
        var miniContent = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0) };
        miniContent.Children.Add(new TextBlock { Text = "🔴 ", FontSize = 10 });
        miniContent.Children.Add(new TextBlock { Text = "ตัวอย่างโน้ต", FontSize = 10, Foreground = Brush(theme.Text) });
        miniCard.Child = miniContent;
        previewBar.Children.Add(miniCard);

        var miniBtn = new Border
        {
            Background = Brush(theme.Accent), CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 3, 8, 3)
        };
        miniBtn.Child = new TextBlock { Text = "ปุ่ม", FontSize = 10, Foreground = Brushes.White };
        previewBar.Children.Add(miniBtn);

        info.Children.Add(previewBar);
        Grid.SetColumn(info, 1);
        grid.Children.Add(info);

        // Select button
        if (!isActive)
        {
            var selectBtn = new Button
            {
                Content = "ใช้ธีมนี้", FontSize = 12, Padding = new Thickness(14, 6, 14, 6),
                Background = Brush(theme.Accent), Foreground = Brushes.White,
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            selectBtn.Template = RoundBtnTemplate(theme.Accent);
            var themeName = theme.Name;
            selectBtn.Click += (_, _) =>
            {
                SelectedThemeName = themeName;
                ThemeManager.SaveTheme(themeName);
                DialogResult = true;
            };
            Grid.SetColumn(selectBtn, 2);
            grid.Children.Add(selectBtn);
        }

        card.Child = grid;
        card.MouseLeftButtonUp += (_, _) =>
        {
            if (!isActive)
            {
                SelectedThemeName = theme.Name;
                ThemeManager.SaveTheme(theme.Name);
                DialogResult = true;
            }
        };

        panelThemes.Children.Add(card);
    }

    private static Ellipse ColorCircle(string hex, double size) => new()
    {
        Width = size, Height = size, Margin = new Thickness(0, 0, 4, 0),
        Fill = Brush(hex), Stroke = new SolidColorBrush(Colors.Gray), StrokeThickness = 0.5
    };

    private static ControlTemplate RoundBtnTemplate(string bgColor)
    {
        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.BackgroundProperty, Brush(bgColor));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
        border.SetValue(Border.PaddingProperty, new Thickness(14, 6, 14, 6));
        var cp = new FrameworkElementFactory(typeof(ContentPresenter));
        cp.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        border.AppendChild(cp);
        template.VisualTree = border;
        return template;
    }

    private static SolidColorBrush Brush(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex));
}
