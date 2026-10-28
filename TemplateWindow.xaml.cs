using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NoteApp;

public partial class TemplateWindow : Window
{
    public int SelectedIndex { get; private set; } = -1;

    public TemplateWindow()
    {
        InitializeComponent();
        RenderTemplates();
    }

    private void RenderTemplates()
    {
        for (int i = 0; i < NoteTemplates.Templates.Length; i++)
        {
            var t = NoteTemplates.Templates[i];
            var card = new Border
            {
                Background = B("#161828"), CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14), Margin = new Thickness(0, 0, 0, 8),
                Cursor = Cursors.Hand, BorderBrush = B("#2a2d45"), BorderThickness = new Thickness(1)
            };

            var sp = new StackPanel();

            var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
            titleRow.Children.Add(new TextBlock { Text = t.Icon + " ", FontSize = 18 });
            titleRow.Children.Add(new TextBlock
            {
                Text = t.Name, FontSize = 15, FontWeight = FontWeights.SemiBold,
                Foreground = B("#e8e9f0"), VerticalAlignment = VerticalAlignment.Center
            });
            sp.Children.Add(titleRow);

            var metaRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
            metaRow.Children.Add(new Border
            {
                Background = B("#2a2d45"), CornerRadius = new CornerRadius(5),
                Padding = new Thickness(6, 1, 6, 1),
                Child = new TextBlock { Text = t.Category, FontSize = 10, Foreground = B("#8b8fa7") }
            });
            sp.Children.Add(metaRow);

            var preview = t.Content.Length > 80 ? t.Content[..80] + "..." : t.Content;
            sp.Children.Add(new TextBlock
            {
                Text = preview, FontSize = 11, Foreground = B("#5a5d70"),
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0)
            });

            card.Child = sp;
            var idx = i;
            card.MouseLeftButtonUp += (_, _) => { SelectedIndex = idx; DialogResult = true; };
            panelTemplates.Children.Add(card);
        }
    }

    private static SolidColorBrush B(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex));
}
