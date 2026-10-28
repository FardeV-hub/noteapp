using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NoteApp;

public partial class TrashWindow : Window
{
    private readonly List<Note> _trash;
    private readonly Action<Note> _onRestore;

    public TrashWindow(List<Note> trash, Action<Note> onRestore)
    {
        InitializeComponent();
        _trash = trash;
        _onRestore = onRestore;
        Render();
    }

    private void Render()
    {
        panelTrash.Children.Clear();
        lblTrashInfo.Text = $"{_trash.Count} รายการ · ลบอัตโนมัติหลัง 30 วัน";

        if (_trash.Count == 0)
        {
            panelTrash.Children.Add(new TextBlock
            {
                Text = "🗑️ ถังขยะว่าง", FontSize = 16,
                Foreground = Brush("#8b8fa7"), HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 40, 0, 0)
            });
            return;
        }

        foreach (var note in _trash.ToArray())
        {
            var card = new Border
            {
                Background = Brush("#161828"), CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 6)
            };

            var sp = new StackPanel();

            // Title
            sp.Children.Add(new TextBlock
            {
                Text = $"{note.PriorityIcon} {note.Title}", FontSize = 14,
                FontWeight = FontWeights.SemiBold, Foreground = Brush("#e8e9f0")
            });

            // Meta
            var meta = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
            meta.Children.Add(SmallBadge(note.Category ?? "ทั่วไป"));
            meta.Children.Add(new TextBlock
            {
                Text = $"ลบเมื่อ: {note.DeletedAt}", FontSize = 11,
                Foreground = Brush("#5a5d70"), VerticalAlignment = VerticalAlignment.Center
            });

            if (DateTime.TryParse(note.DeletedAt, out var deletedAt))
            {
                var daysLeft = 30 - (int)(DateTime.Now - deletedAt).TotalDays;
                meta.Children.Add(new TextBlock
                {
                    Text = $"  ({daysLeft} วันก่อนลบถาวร)", FontSize = 11,
                    Foreground = Brush(daysLeft <= 7 ? "#e74c3c" : "#f39c12"),
                    VerticalAlignment = VerticalAlignment.Center
                });
            }
            sp.Children.Add(meta);

            // Buttons
            var btns = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };

            var restoreBtn = new Button
            {
                Content = "♻️ กู้คืน", Padding = new Thickness(12, 5, 12, 5),
                FontSize = 12, Background = Brush("#27ae60"), Foreground = Brushes.White,
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand
            };
            var noteRef = note;
            restoreBtn.Click += (_, _) =>
            {
                noteRef.Deleted = false;
                noteRef.DeletedAt = "";
                _onRestore(noteRef);
                Render();
                MessageBox.Show($"กู้คืน \"{noteRef.Title}\" สำเร็จ!", "♻️ กู้คืน");
            };
            btns.Children.Add(restoreBtn);

            var deleteBtn = new Button
            {
                Content = "❌ ลบถาวร", Padding = new Thickness(12, 5, 12, 5),
                FontSize = 12, Background = Brush("#e74c3c"), Foreground = Brushes.White,
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
                Margin = new Thickness(6, 0, 0, 0)
            };
            deleteBtn.Click += (_, _) =>
            {
                if (MessageBox.Show($"ลบ \"{noteRef.Title}\" ถาวร?\n\nจะไม่สามารถกู้คืนได้อีก",
                    "❌ ลบถาวร", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _trash.Remove(noteRef);
                    TrashStore.Save(_trash);
                    Render();
                }
            };
            btns.Children.Add(deleteBtn);

            sp.Children.Add(btns);
            card.Child = sp;
            panelTrash.Children.Add(card);
        }
    }

    private static SolidColorBrush Brush(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex));

    private static Border SmallBadge(string text) => new()
    {
        Background = Brush("#2a2d45"), CornerRadius = new CornerRadius(5),
        Padding = new Thickness(6, 1, 6, 1), Margin = new Thickness(0, 0, 8, 0),
        Child = new TextBlock { Text = text, FontSize = 10, Foreground = Brush("#8b8fa7") }
    };

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
