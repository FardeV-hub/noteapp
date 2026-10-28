using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NoteApp;

public partial class CommunityWindow : Window
{
    private List<CommunityPost> _posts;
    private CommunityServer? _server;

    public CommunityWindow()
    {
        InitializeComponent();
        _posts = CommunityStore.Load();
        lblInfo.Text = $"{_posts.Count} โพสต์ · {_posts.Sum(p => p.Replies.Count)} ตอบกลับ";
        lblServerDetail.Text = "ยังไม่ได้เปิดแชร์ กดปุ่ม 'เปิดแชร์' เพื่อเริ่ม";
        RenderFeed();
    }

    private void RenderFeed()
    {
        panelFeed.Children.Clear();

        if (!_posts.Any())
        {
            panelFeed.Children.Add(new TextBlock
            {
                Text = "ยังไม่มีโพสต์\n\nเริ่มสร้างโพสต์แรกได้เลย!",
                FontSize = 14, Foreground = B("#7d8298"),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 60, 0, 0)
            });
            return;
        }

        var pinned = _posts.Where(p => p.Pinned).ToList();
        var normal = _posts.Where(p => !p.Pinned).ToList();

        if (pinned.Any())
        {
            panelFeed.Children.Add(new TextBlock
            {
                Text = "ปักหมุด", FontSize = 12, FontWeight = FontWeights.SemiBold,
                Foreground = B("#7d8298"), Margin = new Thickness(4, 0, 0, 6)
            });
            foreach (var p in pinned) AddPostCard(p);
        }

        foreach (var p in normal) AddPostCard(p);

        lblInfo.Text = $"{_posts.Count} โพสต์ · {_posts.Sum(p => p.Replies.Count)} ตอบกลับ";
    }

    private void AddPostCard(CommunityPost post)
    {
        var card = new Border
        {
            Background = B("#1c1f2a"), CornerRadius = new CornerRadius(10),
            Padding = new Thickness(16), Margin = new Thickness(0, 0, 0, 8),
        };

        var sp = new StackPanel();

        // Title
        sp.Children.Add(new TextBlock
        {
            Text = post.Title, FontSize = 15, FontWeight = FontWeights.SemiBold,
            Foreground = B("#d8dae2"), Margin = new Thickness(0, 0, 0, 6),
            TextWrapping = TextWrapping.Wrap,
        });

        // Meta
        var meta = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
        meta.Children.Add(Badge(post.Category, "#7b93f7"));
        meta.Children.Add(Badge(post.Author));
        meta.Children.Add(new TextBlock
        {
            Text = $"{post.Created}  ·  {post.Replies.Count} ตอบ",
            FontSize = 10.5, Foreground = B("#5a5d70"), VerticalAlignment = VerticalAlignment.Center,
        });
        sp.Children.Add(meta);

        // Content
        if (!string.IsNullOrEmpty(post.Content))
        {
            sp.Children.Add(new TextBlock
            {
                Text = post.Content, FontSize = 13, Foreground = B("#b0b3c6"),
                TextWrapping = TextWrapping.Wrap, LineHeight = 21,
                Margin = new Thickness(0, 0, 0, 8),
            });
        }

        // Replies
        foreach (var r in post.Replies)
        {
            var replyBorder = new Border
            {
                Background = B("#252838"), CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(16, 4, 0, 0),
            };
            var rSp = new StackPanel();
            rSp.Children.Add(new TextBlock
            {
                Text = $"{r.Author}  ·  {r.Created}",
                FontSize = 10.5, Foreground = B("#5a5d70"), Margin = new Thickness(0, 0, 0, 4),
            });
            rSp.Children.Add(new TextBlock
            {
                Text = r.Content, FontSize = 12.5, Foreground = B("#9a9db0"),
                TextWrapping = TextWrapping.Wrap,
            });
            replyBorder.Child = rSp;
            sp.Children.Add(replyBorder);
        }

        // Reply input
        var replyRow = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        replyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        replyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var replyBox = new TextBox
        {
            FontSize = 12, Padding = new Thickness(10, 7, 10, 7),
            Background = B("#252838"), Foreground = B("#d8dae2"),
            BorderBrush = B("#303345"), BorderThickness = new Thickness(1),
        };

        var replyBtn = new Button
        {
            Content = "ตอบ", Padding = new Thickness(14, 7, 14, 7),
            FontSize = 12, FontWeight = FontWeights.SemiBold,
            Background = B("#353a50"), Foreground = B("#d8dae2"),
            Margin = new Thickness(6, 0, 0, 0),
        };
        var postRef = post;
        var boxRef = replyBox;
        replyBtn.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(boxRef.Text)) return;
            postRef.Replies.Add(new CommunityReply
            {
                Author = App.CurrentUser ?? "ไม่ระบุชื่อ",
                Content = boxRef.Text.Trim(),
            });
            CommunityStore.Save(_posts);
            RenderFeed();
        };

        replyBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
            {
                replyBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                e.Handled = true;
            }
        };

        Grid.SetColumn(replyBox, 0);
        Grid.SetColumn(replyBtn, 1);
        replyRow.Children.Add(replyBox);
        replyRow.Children.Add(replyBtn);
        sp.Children.Add(replyRow);

        // Actions row
        var actRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };

        var likeBtn = new Button
        {
            Content = $"♡ {post.Likes}", Padding = new Thickness(10, 4, 10, 4),
            FontSize = 11, Background = B("#252838"), Foreground = B("#7d8298"),
        };
        var likeRef = post;
        likeBtn.Click += (_, _) =>
        {
            likeRef.Likes++;
            CommunityStore.Save(_posts);
            RenderFeed();
        };
        actRow.Children.Add(likeBtn);

        var pinBtn = new Button
        {
            Content = post.Pinned ? "เลิกปักหมุด" : "ปักหมุด",
            Padding = new Thickness(10, 4, 10, 4), FontSize = 11,
            Background = B("#252838"), Foreground = B("#7d8298"),
            Margin = new Thickness(4, 0, 0, 0),
        };
        pinBtn.Click += (_, _) =>
        {
            postRef.Pinned = !postRef.Pinned;
            CommunityStore.Save(_posts);
            RenderFeed();
        };
        actRow.Children.Add(pinBtn);

        var delBtn = new Button
        {
            Content = "ลบ", Padding = new Thickness(10, 4, 10, 4),
            FontSize = 11, Background = B("#252838"), Foreground = B("#e5484d"),
            Margin = new Thickness(4, 0, 0, 0),
        };
        delBtn.Click += (_, _) =>
        {
            if (MessageBox.Show($"ลบโพสต์ \"{postRef.Title}\"?", "ยืนยัน", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _posts.Remove(postRef);
                CommunityStore.Save(_posts);
                RenderFeed();
            }
        };
        actRow.Children.Add(delBtn);

        sp.Children.Add(actRow);
        card.Child = sp;
        panelFeed.Children.Add(card);
    }

    private void CreatePost_Click(object sender, RoutedEventArgs e)
    {
        var title = txtPostTitle.Text.Trim();
        if (string.IsNullOrEmpty(title)) { MessageBox.Show("กรุณาใส่หัวข้อ"); return; }

        var post = new CommunityPost
        {
            Author = App.CurrentUser ?? "ไม่ระบุชื่อ",
            Title = title,
            Content = txtPostContent.Text.Trim(),
            Category = (cmbPostCat.SelectedItem as ComboBoxItem)?.Content as string ?? "ทั่วไป",
        };
        _posts.Insert(0, post);
        CommunityStore.Save(_posts);

        txtPostTitle.Text = "";
        txtPostContent.Text = "";
        RenderFeed();
        MessageBox.Show($"โพสต์ \"{title}\" สำเร็จ!", "Community");
    }

    private void ToggleServer_Click(object sender, RoutedEventArgs e)
    {
        if (_server?.IsRunning == true)
        {
            _server.Stop();
            _server = null;
            btnServer.Content = "เปิดแชร์";
            btnServer.Background = B("#7b93f7");
            lblServer.Text = "";
            lblServerDetail.Text = "ปิดแชร์แล้ว";
            return;
        }

        try
        {
            _server = new CommunityServer(8877, _posts, () => Dispatcher.Invoke(RenderFeed));
            _server.Start();
            var ip = GetLocalIP();
            btnServer.Content = "ปิดแชร์";
            btnServer.Background = B("#e5484d");
            lblServer.Text = $"http://{ip}:8877";
            lblServerDetail.Text = $"กำลังแชร์อยู่\n\nURL: http://{ip}:8877\n\nแจ้งเพื่อนให้เปิด URL นี้ใน Browser\n(ต้องอยู่ WiFi เดียวกัน)";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"เปิดแชร์ไม่ได้:\n{ex.Message}\n\nลองรันแอปในฐานะ Administrator");
        }
    }

    private static string GetLocalIP()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            var ip = host.AddressList.FirstOrDefault(a =>
                a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            return ip?.ToString() ?? "localhost";
        }
        catch { return "localhost"; }
    }

    private static Border Badge(string text, string color = "#7d8298") => new()
    {
        Background = B("#252838"), CornerRadius = new CornerRadius(6),
        Padding = new Thickness(7, 2, 7, 2), Margin = new Thickness(0, 0, 6, 0),
        Child = new TextBlock { Text = text, FontSize = 10.5, Foreground = B(color) }
    };

    private static SolidColorBrush B(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex));
}
