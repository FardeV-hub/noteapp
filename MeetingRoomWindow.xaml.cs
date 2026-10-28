using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NoteApp;

public partial class MeetingRoomWindow : Window
{
    private MeetingData _data;
    private IntegrationSettings _integrations;
    private readonly List<Note> _notes;
    private readonly Action<Note> _addNote;
    private DateTime _viewDate;

    private static readonly string[] TimeSlots = Enumerable.Range(7, 14)
        .SelectMany(h => new[] { $"{h:D2}:00", $"{h:D2}:30" }).ToArray();

    public MeetingRoomWindow(List<Note> notes, Action<Note> addNote)
    {
        InitializeComponent();
        _notes = notes;
        _addNote = addNote;
        _data = MeetingStore.Load();
        _integrations = IntegrationStore.Load();
        _viewDate = DateTime.Today;

        InitForm();
        LoadIntegrationSettings();
        Refresh();
    }

    private void InitForm()
    {
        dpBookDate.SelectedDate = DateTime.Today;
        cmbStartTime.ItemsSource = TimeSlots;
        cmbStartTime.SelectedIndex = 4;
        cmbEndTime.ItemsSource = TimeSlots;
        cmbEndTime.SelectedIndex = 6;
        RefreshRoomCombo();
    }

    private void RefreshRoomCombo()
    {
        cmbRoom.ItemsSource = _data.Rooms.Select(r => $"{r.Name} (👥 {r.Capacity} คน)").ToList();
        if (cmbRoom.Items.Count > 0) cmbRoom.SelectedIndex = 0;
    }

    private void Refresh()
    {
        var dateStr = _viewDate.ToString("yyyy-MM-dd");
        var todayBookings = _data.Bookings.Where(b => b.Date == dateStr).ToList();
        lblInfo.Text = $"📅 {_viewDate:dd/MM/yyyy (dddd)}  ·  {_data.Rooms.Count} ห้อง  ·  วันนี้ {todayBookings.Count} การจอง";

        RenderSchedule(dateStr, todayBookings);
        RenderAllBookings();
        RenderRooms();
    }

    // ====================================================
    // Tab 1: ตารางวันนี้
    // ====================================================
    private void RenderSchedule(string dateStr, List<Booking> todayBookings)
    {
        panelSchedule.Children.Clear();

        panelSchedule.Children.Add(new TextBlock
        {
            Text = $"📅 ตารางห้องประชุม — {_viewDate:dd/MM/yyyy}",
            FontSize = 16, FontWeight = FontWeights.Bold, Foreground = B("#e8e9f0"),
            Margin = new Thickness(0, 0, 0, 10)
        });

        foreach (var room in _data.Rooms)
        {
            var roomBookings = todayBookings.Where(b => b.RoomId == room.Id)
                .OrderBy(b => b.StartTime).ToList();

            var card = new Border
            {
                Background = B("#161828"), CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14), Margin = new Thickness(0, 0, 0, 10),
                BorderBrush = B(room.Color), BorderThickness = new Thickness(0, 0, 0, 3)
            };

            var sp = new StackPanel();

            // Room header
            var headerRow = new DockPanel();
            headerRow.Children.Add(new TextBlock
            {
                Text = $"🏢 {room.Name}", FontSize = 15, FontWeight = FontWeights.Bold,
                Foreground = B(room.Color)
            });
            var infoBadge = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            infoBadge.Children.Add(Badge($"👥 {room.Capacity} คน"));
            infoBadge.Children.Add(Badge(roomBookings.Any() ? $"📋 {roomBookings.Count} จอง" : "✅ ว่าง"));
            DockPanel.SetDock(infoBadge, Dock.Right);
            headerRow.Children.Insert(0, infoBadge);
            sp.Children.Add(headerRow);

            if (!string.IsNullOrEmpty(room.Facilities))
                sp.Children.Add(new TextBlock { Text = $"🛠️ {room.Facilities}", FontSize = 11, Foreground = B("#8b8fa7"), Margin = new Thickness(0, 4, 0, 0) });

            // Timeline
            if (roomBookings.Any())
            {
                var timeline = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
                foreach (var b in roomBookings)
                {
                    var bCard = new Border
                    {
                        Background = B("#2a2d45"), CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 3, 0, 0)
                    };
                    var bRow = new DockPanel();
                    var timeText = new TextBlock
                    {
                        Text = $"🕐 {b.StartTime}–{b.EndTime}", FontSize = 12,
                        Foreground = B(room.Color), FontWeight = FontWeights.SemiBold
                    };
                    bRow.Children.Add(timeText);

                    var topicText = new TextBlock
                    {
                        Text = $"  {b.Topic}", FontSize = 12, Foreground = B("#e8e9f0"),
                        Margin = new Thickness(8, 0, 0, 0)
                    };
                    bRow.Children.Add(topicText);

                    if (!string.IsNullOrEmpty(b.Organizer))
                    {
                        var orgText = new TextBlock
                        {
                            Text = $"👤 {b.Organizer}", FontSize = 11, Foreground = B("#8b8fa7"),
                            HorizontalAlignment = HorizontalAlignment.Right
                        };
                        DockPanel.SetDock(orgText, Dock.Right);
                        bRow.Children.Insert(0, orgText);
                    }

                    bCard.Child = bRow;
                    timeline.Children.Add(bCard);
                }
                sp.Children.Add(timeline);
            }
            else
            {
                sp.Children.Add(new TextBlock
                {
                    Text = "✅ ว่างทั้งวัน", FontSize = 12, Foreground = B("#27ae60"),
                    Margin = new Thickness(0, 8, 0, 0)
                });
            }

            card.Child = sp;
            panelSchedule.Children.Add(card);
        }

        // Date navigation
        var nav = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 0) };
        var prevBtn = MakeBtn("◀ วันก่อน", "#2a2d45");
        prevBtn.Click += (_, _) => { _viewDate = _viewDate.AddDays(-1); Refresh(); };
        nav.Children.Add(prevBtn);

        nav.Children.Add(new TextBlock
        {
            Text = $"  {_viewDate:dd/MM/yyyy}  ", FontSize = 13, Foreground = B("#e8e9f0"),
            VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.Bold
        });

        var nextBtn = MakeBtn("วันถัดไป ▶", "#2a2d45");
        nextBtn.Click += (_, _) => { _viewDate = _viewDate.AddDays(1); Refresh(); };
        nav.Children.Add(nextBtn);

        panelSchedule.Children.Add(nav);
    }

    // ====================================================
    // Tab 2: จองห้อง
    // ====================================================
    private void BookRoom_Click(object sender, RoutedEventArgs e)
    {
        var booking = CreateBooking();
        if (booking == null) return;

        _data.Bookings.Add(booking);
        MeetingStore.Save(_data);
        Refresh();
        MessageBox.Show($"จองห้องสำเร็จ!\n\n🏢 {GetRoomName(booking.RoomId)}\n📋 {booking.Topic}\n📅 {booking.Date}\n🕐 {booking.StartTime}–{booking.EndTime}",
            "✅ จองสำเร็จ");
    }

    private void BookAndNote_Click(object sender, RoutedEventArgs e)
    {
        var booking = CreateBooking();
        if (booking == null) return;

        var room = _data.Rooms.FirstOrDefault(r => r.Id == booking.RoomId);
        var note = new Note
        {
            Title = $"ประชุม — {booking.Topic}",
            Content = $"📅 วันที่: {booking.Date}\n🕐 เวลา: {booking.StartTime}–{booking.EndTime}\n🏢 ห้อง: {room?.Name ?? ""}\n👤 ผู้จัด: {booking.Organizer}\n👥 ผู้เข้าร่วม: {booking.Participants}\n\n📋 วาระการประชุม:\n1. \n2. \n3. \n\n📝 สรุปการประชุม:\n- \n\n✅ สิ่งที่ต้องทำต่อ:\n☐ \n☐ ",
            Category = "งาน",
            Priority = "ปานกลาง",
            Reminder = $"{booking.Date} {booking.StartTime}",
            RemindMinutes = 15,
        };

        booking.NoteId = note.Id;
        _data.Bookings.Add(booking);
        MeetingStore.Save(_data);

        _addNote(note);
        Refresh();
        MessageBox.Show($"จองห้อง + สร้างโน้ตประชุมสำเร็จ!\n\n🏢 {room?.Name}\n📋 {booking.Topic}\n📝 โน้ตพร้อมเทมเพลตประชุมแล้ว\n⏰ แจ้งเตือน 15 นาทีก่อน",
            "✅ จอง + โน้ต");
    }

    private Booking? CreateBooking()
    {
        if (cmbRoom.SelectedIndex < 0) { MessageBox.Show("กรุณาเลือกห้อง"); return null; }
        if (string.IsNullOrWhiteSpace(txtTopic.Text)) { MessageBox.Show("กรุณาใส่หัวข้อ"); return null; }
        if (dpBookDate.SelectedDate == null) { MessageBox.Show("กรุณาเลือกวันที่"); return null; }

        var room = _data.Rooms[cmbRoom.SelectedIndex];
        var date = dpBookDate.SelectedDate.Value.ToString("yyyy-MM-dd");
        var start = cmbStartTime.SelectedItem as string ?? "09:00";
        var end = cmbEndTime.SelectedItem as string ?? "10:00";

        if (string.Compare(start, end) >= 0)
        {
            MessageBox.Show("เวลาสิ้นสุดต้องมากกว่าเวลาเริ่มต้น"); return null;
        }

        var conflict = _data.Bookings.FirstOrDefault(b =>
            b.RoomId == room.Id && b.Date == date &&
            string.Compare(b.StartTime, end) < 0 && string.Compare(b.EndTime, start) > 0);
        if (conflict != null)
        {
            MessageBox.Show($"ห้องนี้ไม่ว่างในช่วงเวลาดังกล่าว!\n\nชนกับ: {conflict.Topic}\n🕐 {conflict.StartTime}–{conflict.EndTime}",
                "❌ ห้องไม่ว่าง");
            return null;
        }

        return new Booking
        {
            RoomId = room.Id,
            Topic = txtTopic.Text.Trim(),
            Date = date,
            StartTime = start,
            EndTime = end,
            Organizer = txtOrganizer.Text.Trim(),
            Participants = txtParticipants.Text.Trim(),
        };
    }

    // ====================================================
    // Tab 3: รายการจองทั้งหมด
    // ====================================================
    private void RenderAllBookings()
    {
        panelAllBookings.Children.Clear();
        var upcoming = _data.Bookings
            .Where(b => string.Compare(b.Date, DateTime.Today.ToString("yyyy-MM-dd")) >= 0)
            .OrderBy(b => b.Date).ThenBy(b => b.StartTime).ToList();
        var past = _data.Bookings
            .Where(b => string.Compare(b.Date, DateTime.Today.ToString("yyyy-MM-dd")) < 0)
            .OrderByDescending(b => b.Date).ThenBy(b => b.StartTime).ToList();

        panelAllBookings.Children.Add(new TextBlock
        {
            Text = $"📋 การจองที่กำลังจะมาถึง ({upcoming.Count})",
            FontSize = 15, FontWeight = FontWeights.Bold, Foreground = B("#e8e9f0"),
            Margin = new Thickness(0, 0, 0, 8)
        });

        foreach (var b in upcoming) AddBookingCard(panelAllBookings, b, true);

        if (past.Any())
        {
            panelAllBookings.Children.Add(new TextBlock
            {
                Text = $"\n📜 ประวัติ ({past.Count})",
                FontSize = 14, FontWeight = FontWeights.Bold, Foreground = B("#8b8fa7"),
                Margin = new Thickness(0, 12, 0, 8)
            });
            foreach (var b in past.Take(20)) AddBookingCard(panelAllBookings, b, false);
        }
    }

    private void AddBookingCard(StackPanel panel, Booking b, bool canCancel)
    {
        var room = _data.Rooms.FirstOrDefault(r => r.Id == b.RoomId);
        var color = room?.Color ?? "#6c63ff";
        var card = new Border
        {
            Background = B("#161828"), CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 6),
            BorderBrush = B(color), BorderThickness = new Thickness(2, 0, 0, 0)
        };

        var sp = new StackPanel();
        var topRow = new DockPanel();
        topRow.Children.Add(new TextBlock
        {
            Text = $"📋 {b.Topic}", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = B("#e8e9f0")
        });

        if (canCancel)
        {
            var cancelBtn = MakeBtn("❌ ยกเลิก", "#e74c3c");
            var bRef = b;
            cancelBtn.Click += (_, _) =>
            {
                if (MessageBox.Show($"ยกเลิกการจอง \"{bRef.Topic}\"?", "ยืนยัน", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _data.Bookings.Remove(bRef);
                    MeetingStore.Save(_data);
                    Refresh();
                }
            };
            DockPanel.SetDock(cancelBtn, Dock.Right);
            topRow.Children.Insert(0, cancelBtn);
        }
        sp.Children.Add(topRow);

        var metaRow = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
        metaRow.Children.Add(Badge($"🏢 {room?.Name ?? "?"}"));
        metaRow.Children.Add(Badge($"📅 {b.Date}"));
        metaRow.Children.Add(Badge($"🕐 {b.StartTime}–{b.EndTime}"));
        if (!string.IsNullOrEmpty(b.Organizer)) metaRow.Children.Add(Badge($"👤 {b.Organizer}"));
        sp.Children.Add(metaRow);

        if (!string.IsNullOrEmpty(b.Participants))
            sp.Children.Add(new TextBlock { Text = $"👥 {b.Participants}", FontSize = 11, Foreground = B("#8b8fa7"), Margin = new Thickness(0, 4, 0, 0) });

        card.Child = sp;
        panel.Children.Add(card);
    }

    // ====================================================
    // Tab 4: จัดการห้อง
    // ====================================================
    private void RenderRooms()
    {
        panelRooms.Children.Clear();
        panelRooms.Children.Add(new TextBlock
        {
            Text = "⚙️ จัดการห้องประชุม", FontSize = 16, FontWeight = FontWeights.Bold,
            Foreground = B("#e8e9f0"), Margin = new Thickness(0, 0, 0, 10)
        });

        foreach (var room in _data.Rooms)
        {
            var card = new Border
            {
                Background = B("#161828"), CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14), Margin = new Thickness(0, 0, 0, 8),
                BorderBrush = B(room.Color), BorderThickness = new Thickness(0, 0, 0, 3)
            };

            var sp = new StackPanel();
            var topRow = new DockPanel();
            topRow.Children.Add(new TextBlock
            {
                Text = $"🏢 {room.Name}", FontSize = 15, FontWeight = FontWeights.Bold, Foreground = B(room.Color)
            });

            var bookingCount = _data.Bookings.Count(b => b.RoomId == room.Id);
            var delBtn = MakeBtn("🗑️ ลบ", "#e74c3c");
            var roomRef = room;
            delBtn.Click += (_, _) =>
            {
                if (MessageBox.Show($"ลบห้อง \"{roomRef.Name}\"?\n\nการจองที่เกี่ยวข้องจะถูกลบด้วย",
                    "ยืนยัน", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _data.Bookings.RemoveAll(b => b.RoomId == roomRef.Id);
                    _data.Rooms.Remove(roomRef);
                    MeetingStore.Save(_data);
                    RefreshRoomCombo();
                    Refresh();
                }
            };
            DockPanel.SetDock(delBtn, Dock.Right);
            topRow.Children.Insert(0, delBtn);
            sp.Children.Add(topRow);

            var meta = new WrapPanel { Margin = new Thickness(0, 6, 0, 0) };
            meta.Children.Add(Badge($"👥 {room.Capacity} คน"));
            meta.Children.Add(Badge($"📋 {bookingCount} จอง"));
            if (!string.IsNullOrEmpty(room.Facilities)) meta.Children.Add(Badge($"🛠️ {room.Facilities}"));
            sp.Children.Add(meta);

            card.Child = sp;
            panelRooms.Children.Add(card);
        }
    }

    // ====================================================
    // Actions
    // ====================================================
    private void AddRoom_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new InputDialog("เพิ่มห้องประชุม", "ชื่อห้องประชุม:") { Owner = this };
        if (dlg.ShowDialog() != true || string.IsNullOrWhiteSpace(dlg.Answer)) return;

        var colors = new[] { "#6c63ff", "#e84393", "#00b894", "#fdcb6e", "#e17055", "#0984e3", "#00cec9" };
        _data.Rooms.Add(new MeetingRoom
        {
            Name = dlg.Answer.Trim(),
            Capacity = 10,
            Color = colors[_data.Rooms.Count % colors.Length]
        });
        MeetingStore.Save(_data);
        RefreshRoomCombo();
        Refresh();
    }

    private void Today_Click(object sender, RoutedEventArgs e)
    {
        _viewDate = DateTime.Today;
        Refresh();
    }

    // ====================================================
    // Discord Integration
    // ====================================================
    private void LoadIntegrationSettings()
    {
        txtDiscordWebhook.Text = _integrations.DiscordWebhook;
        txtDiscordBot.Text = _integrations.DiscordBotToken;
        txtDiscordChannel.Text = _integrations.DiscordChannelId;
        txtGoogleApiKey.Text = _integrations.GoogleApiKey;
        txtCourseId.Text = _integrations.CourseId;
    }

    private void SaveDiscord_Click(object sender, RoutedEventArgs e)
    {
        _integrations.DiscordWebhook = txtDiscordWebhook.Text.Trim();
        _integrations.DiscordBotToken = txtDiscordBot.Text.Trim();
        _integrations.DiscordChannelId = txtDiscordChannel.Text.Trim();
        IntegrationStore.Save(_integrations);
        MessageBox.Show("บันทึกการตั้งค่า Discord สำเร็จ!", "💾 Discord");
    }

    private async void TestDiscord_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_integrations.DiscordWebhook))
        {
            MessageBox.Show("กรุณาใส่ Webhook URL และบันทึกก่อน", "🎮 Discord");
            return;
        }
        try
        {
            var ok = await DiscordHelper.SendWebhook(_integrations.DiscordWebhook,
                "🔔 **ทดสอบจาก Note App**\n\nระบบแจ้งเตือนห้องประชุมทำงานปกติ! ✅");
            MessageBox.Show(ok ? "ส่งข้อความทดสอบไป Discord สำเร็จ! ✅" : "ส่งไม่สำเร็จ ตรวจสอบ Webhook URL",
                "🎮 Discord");
        }
        catch (Exception ex) { MessageBox.Show($"เกิดข้อผิดพลาด:\n{ex.Message}", "❌ Discord"); }
    }

    private async void SendDiscord_Click(object sender, RoutedEventArgs e)
    {
        var booking = CreateBooking();
        if (booking == null) return;

        if (string.IsNullOrWhiteSpace(_integrations.DiscordWebhook))
        {
            MessageBox.Show("กรุณาตั้งค่า Discord Webhook ในแท็บ 🎮 Discord ก่อน", "🎮 Discord");
            return;
        }

        var room = _data.Rooms[cmbRoom.SelectedIndex];
        _data.Bookings.Add(booking);
        MeetingStore.Save(_data);
        Refresh();

        try
        {
            var ok = await DiscordHelper.SendBookingNotification(_integrations.DiscordWebhook, booking, room.Name);
            MessageBox.Show(ok
                ? $"จองห้อง + ส่งแจ้งเตือน Discord สำเร็จ!\n\n🏢 {room.Name}\n📋 {booking.Topic}"
                : "จองห้องสำเร็จ แต่ส่ง Discord ไม่สำเร็จ", "🎮 Discord");
        }
        catch (Exception ex) { MessageBox.Show($"จองสำเร็จ แต่ส่ง Discord ผิดพลาด:\n{ex.Message}"); }
    }

    // ====================================================
    // Google Classroom Integration
    // ====================================================
    private void SaveClassroom_Click(object sender, RoutedEventArgs e)
    {
        _integrations.GoogleApiKey = txtGoogleApiKey.Text.Trim();
        _integrations.CourseId = txtCourseId.Text.Trim();
        IntegrationStore.Save(_integrations);
        MessageBox.Show("บันทึกการตั้งค่า Classroom สำเร็จ!", "💾 Classroom");
    }

    private async void FetchClassroom_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_integrations.GoogleApiKey) || string.IsNullOrWhiteSpace(_integrations.CourseId))
        {
            MessageBox.Show("กรุณาใส่ API Key และ Course ID แล้วบันทึกก่อน", "📚 Classroom");
            return;
        }

        panelClassroomItems.Children.Clear();
        panelClassroomItems.Children.Add(new TextBlock
        {
            Text = "⏳ กำลังดึงข้อมูล...", FontSize = 13, Foreground = B("#8b8fa7")
        });

        try
        {
            var announcements = await ClassroomHelper.FetchAnnouncements(_integrations.GoogleApiKey, _integrations.CourseId);
            var courseWork = await ClassroomHelper.FetchCourseWork(_integrations.GoogleApiKey, _integrations.CourseId);
            var allItems = new List<ClassroomItem>();
            allItems.AddRange(courseWork);
            allItems.AddRange(announcements);

            panelClassroomItems.Children.Clear();

            if (!allItems.Any())
            {
                panelClassroomItems.Children.Add(new TextBlock
                {
                    Text = "ไม่พบรายการ\n\nตรวจสอบว่า:\n• API Key ถูกต้อง\n• Course ID ถูกต้อง\n• เปิดใช้ Classroom API แล้ว",
                    FontSize = 12, Foreground = B("#e74c3c"), TextWrapping = TextWrapping.Wrap
                });
                return;
            }

            panelClassroomItems.Children.Add(new TextBlock
            {
                Text = $"พบ {allItems.Count} รายการ ({courseWork.Count} งาน, {announcements.Count} ประกาศ)",
                FontSize = 12, Foreground = B("#27ae60"), Margin = new Thickness(0, 0, 0, 8)
            });

            foreach (var item in allItems)
            {
                var card = new Border
                {
                    Background = B("#161828"), CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 6)
                };
                var sp = new StackPanel();

                var icon = item.Type == "coursework" ? "📝" : "📢";
                sp.Children.Add(new TextBlock
                {
                    Text = $"{icon} {(string.IsNullOrEmpty(item.Title) ? "(ไม่มีหัวข้อ)" : item.Title)}",
                    FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = B("#e8e9f0")
                });

                var meta = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
                meta.Children.Add(Badge(item.Type == "coursework" ? "📝 งาน" : "📢 ประกาศ"));
                if (!string.IsNullOrEmpty(item.DueDate)) meta.Children.Add(Badge($"📅 กำหนด: {item.DueDate}"));
                sp.Children.Add(meta);

                if (!string.IsNullOrEmpty(item.Description))
                {
                    var preview = item.Description.Length > 100 ? item.Description[..100] + "..." : item.Description;
                    sp.Children.Add(new TextBlock { Text = preview, FontSize = 11, Foreground = B("#8b8fa7"),
                        TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) });
                }

                // Import button
                var importBtn = MakeBtn("📥 สร้างโน้ต", "#0984e3");
                var itemRef = item;
                importBtn.Click += (_, _) =>
                {
                    var note = new Note
                    {
                        Title = $"[Classroom] {(string.IsNullOrEmpty(itemRef.Title) ? "ประกาศ" : itemRef.Title)}",
                        Content = itemRef.Description ?? "",
                        Category = "การเรียน",
                        Priority = string.IsNullOrEmpty(itemRef.DueDate) ? "ปกติ" : "ปานกลาง",
                    };
                    if (!string.IsNullOrEmpty(itemRef.DueDate))
                    {
                        note.Reminder = $"{itemRef.DueDate} 08:00";
                        note.RemindMinutes = 60;
                    }
                    _addNote(note);
                    MessageBox.Show($"สร้างโน้ตจาก Classroom สำเร็จ!\n\n📝 {note.Title}", "📚 Classroom");
                };
                importBtn.Margin = new Thickness(0, 6, 0, 0);
                sp.Children.Add(importBtn);

                card.Child = sp;
                panelClassroomItems.Children.Add(card);
            }
        }
        catch (Exception ex)
        {
            panelClassroomItems.Children.Clear();
            panelClassroomItems.Children.Add(new TextBlock
            {
                Text = $"เกิดข้อผิดพลาด:\n{ex.Message}", FontSize = 12,
                Foreground = B("#e74c3c"), TextWrapping = TextWrapping.Wrap
            });
        }
    }

    private async void SendClassroom_Click(object sender, RoutedEventArgs e)
    {
        var booking = CreateBooking();
        if (booking == null) return;

        if (string.IsNullOrWhiteSpace(_integrations.GoogleApiKey) || string.IsNullOrWhiteSpace(_integrations.CourseId))
        {
            MessageBox.Show("กรุณาตั้งค่า Classroom ในแท็บ 📚 Classroom ก่อน", "📚 Classroom");
            return;
        }

        var room = _data.Rooms[cmbRoom.SelectedIndex];
        _data.Bookings.Add(booking);
        MeetingStore.Save(_data);
        Refresh();

        var text = $"📢 แจ้งเตือนการประชุม\n\n📋 {booking.Topic}\n🏢 ห้อง: {room.Name}\n📅 วันที่: {booking.Date}\n🕐 เวลา: {booking.StartTime}–{booking.EndTime}\n👤 ผู้จัด: {booking.Organizer}";

        try
        {
            var ok = await ClassroomHelper.PostAnnouncement(_integrations.GoogleApiKey, _integrations.CourseId, text);
            MessageBox.Show(ok
                ? $"จองห้อง + โพสต์ Classroom สำเร็จ!"
                : "จองสำเร็จ แต่โพสต์ Classroom ไม่สำเร็จ\n(อาจต้องใช้ OAuth2 แทน API Key)", "📚 Classroom");
        }
        catch (Exception ex) { MessageBox.Show($"จองสำเร็จ แต่โพสต์ Classroom ผิดพลาด:\n{ex.Message}"); }
    }

    // ====================================================
    // Helpers
    // ====================================================
    private string GetRoomName(string roomId) =>
        _data.Rooms.FirstOrDefault(r => r.Id == roomId)?.Name ?? "?";

    private static Border Badge(string text) => new()
    {
        Background = B("#2a2d45"), CornerRadius = new CornerRadius(6),
        Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(0, 0, 6, 0),
        Child = new TextBlock { Text = text, FontSize = 10, Foreground = B("#8b8fa7") }
    };

    private static Button MakeBtn(string text, string bg)
    {
        return new Button
        {
            Content = text, Padding = new Thickness(12, 6, 12, 6),
            FontSize = 11, FontWeight = FontWeights.SemiBold,
            Background = B(bg), Foreground = Brushes.White,
            BorderThickness = new Thickness(0), Cursor = Cursors.Hand
        };
    }

    private static SolidColorBrush B(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex));
}
