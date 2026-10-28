using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NoteApp;

public partial class SmartHomeWindow : Window
{
    private SmartHomeData _data;
    private static readonly string[] Rooms = { "ห้องนั่งเล่น", "ห้องนอน", "ห้องครัว", "ห้องน้ำ", "ห้องทำงาน", "ระเบียง", "โรงรถ", "อื่นๆ" };
    private static readonly (string Type, string Name, string Icon)[] DeviceTypes =
    {
        ("light", "ไฟ/หลอดไฟ", "💡"), ("ac", "แอร์", "❄️"), ("fan", "พัดลม", "🌀"),
        ("tv", "ทีวี", "📺"), ("plug", "ปลั๊กอัจฉริยะ", "🔌"), ("lock", "ล็อคประตู", "🔒"),
        ("camera", "กล้อง", "📷"), ("speaker", "ลำโพง", "🔊"), ("curtain", "ม่าน", "🪟"),
    };

    public SmartHomeWindow()
    {
        InitializeComponent();
        _data = SmartHomeStore.Load();
        LoadSettings();
        Render();
    }

    private void LoadSettings()
    {
        txtHAUrl.Text = _data.HomeAssistantUrl;
        txtHAToken.Text = _data.HomeAssistantToken;
        txtTuyaId.Text = _data.TuyaClientId;
        txtTuyaSecret.Text = _data.TuyaSecret;
        txtMqttBroker.Text = _data.MqttBroker;
        txtMqttUser.Text = _data.MqttUser;
        txtMqttPass.Text = _data.MqttPass;
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        _data.HomeAssistantUrl = txtHAUrl.Text.Trim();
        _data.HomeAssistantToken = txtHAToken.Text.Trim();
        _data.TuyaClientId = txtTuyaId.Text.Trim();
        _data.TuyaSecret = txtTuyaSecret.Text.Trim();
        _data.MqttBroker = txtMqttBroker.Text.Trim();
        _data.MqttUser = txtMqttUser.Text.Trim();
        _data.MqttPass = txtMqttPass.Text.Trim();
        SmartHomeStore.Save(_data);
        MessageBox.Show("บันทึกการตั้งค่าสำเร็จ!", "💾");
    }

    private void Render()
    {
        panelDevices.Children.Clear();
        var rooms = _data.Devices.GroupBy(d => d.Room).OrderBy(g => g.Key).ToList();
        var onCount = _data.Devices.Count(d => d.IsOn);
        lblStatus.Text = $"อุปกรณ์ {_data.Devices.Count} ตัว  ·  เปิดอยู่ {onCount}  ·  ปิด {_data.Devices.Count - onCount}";

        if (!rooms.Any())
        {
            panelDevices.Children.Add(new TextBlock
            {
                Text = "🏠 ยังไม่มีอุปกรณ์\n\nกดปุ่ม '+ เพิ่มอุปกรณ์' เพื่อเริ่มต้น",
                FontSize = 14, Foreground = B("#8b8fa7"), HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 40, 0, 0), TextAlignment = TextAlignment.Center
            });
            return;
        }

        // Quick all on/off
        var quickRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        var allOnBtn = MakeBtn("💡 เปิดทั้งหมด", "#27ae60");
        allOnBtn.Click += async (_, _) =>
        {
            foreach (var d in _data.Devices.Where(d => !d.IsOn))
            {
                await SmartHomeAPI.ToggleDevice(d, _data);
                d.IsOn = true;
            }
            SmartHomeStore.Save(_data); Render();
        };
        quickRow.Children.Add(allOnBtn);

        var allOffBtn = MakeBtn("🔅 ปิดทั้งหมด", "#e74c3c");
        allOffBtn.Margin = new Thickness(8, 0, 0, 0);
        allOffBtn.Click += async (_, _) =>
        {
            foreach (var d in _data.Devices.Where(d => d.IsOn))
            {
                await SmartHomeAPI.ToggleDevice(d, _data);
                d.IsOn = false;
            }
            SmartHomeStore.Save(_data); Render();
        };
        quickRow.Children.Add(allOffBtn);
        panelDevices.Children.Add(quickRow);

        foreach (var roomGroup in rooms)
        {
            // Room header
            panelDevices.Children.Add(new TextBlock
            {
                Text = $"🏠 {roomGroup.Key} ({roomGroup.Count()})",
                FontSize = 14, FontWeight = FontWeights.Bold, Foreground = B("#8b8fa7"),
                Margin = new Thickness(0, 10, 0, 4)
            });

            // Device grid (2 columns)
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            int row = 0;
            var devices = roomGroup.ToList();
            for (int i = 0; i < devices.Count; i += 2)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                AddDeviceCard(grid, devices[i], 0, row);
                if (i + 1 < devices.Count)
                    AddDeviceCard(grid, devices[i + 1], 2, row);
                row++;
            }
            panelDevices.Children.Add(grid);
        }
    }

    private void AddDeviceCard(Grid grid, SmartDevice device, int col, int row)
    {
        var card = new Border
        {
            Background = B(device.IsOn ? "#1a2e1a" : "#161828"),
            CornerRadius = new CornerRadius(12), Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 8),
            BorderBrush = B(device.StatusColor), BorderThickness = new Thickness(0, 0, 0, 3)
        };

        var sp = new StackPanel();

        // Top: icon + name + toggle
        var topRow = new DockPanel();
        topRow.Children.Add(new TextBlock { Text = $"{device.Icon} {device.Name}", FontSize = 14,
            FontWeight = FontWeights.SemiBold, Foreground = B("#e8e9f0") });

        var toggleBtn = MakeBtn(device.IsOn ? "🔴 ปิด" : "🟢 เปิด", device.IsOn ? "#e74c3c" : "#27ae60");
        var devRef = device;
        toggleBtn.Click += async (_, _) =>
        {
            var ok = await SmartHomeAPI.ToggleDevice(devRef, _data);
            SmartHomeStore.Save(_data);
            Render();
            if (!ok && devRef.Platform != "demo")
                MessageBox.Show("ไม่สามารถเชื่อมต่ออุปกรณ์ได้\nตรวจสอบการตั้งค่า", "❌");
        };
        DockPanel.SetDock(toggleBtn, Dock.Right);
        topRow.Children.Insert(0, toggleBtn);
        sp.Children.Add(topRow);

        // Meta
        var meta = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
        meta.Children.Add(Badge(device.Room));
        meta.Children.Add(Badge(device.Platform));
        meta.Children.Add(new TextBlock { Text = device.StatusText, FontSize = 11,
            Foreground = B(device.StatusColor), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) });
        sp.Children.Add(meta);

        // AC temp / light brightness
        if (device.Type == "ac" && device.IsOn)
        {
            var tempRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
            tempRow.Children.Add(new TextBlock { Text = $"🌡️ {device.Temperature}°C", FontSize = 12, Foreground = B("#0984e3"), Margin = new Thickness(0, 0, 8, 0) });
            var minusBtn = MakeBtn("−", "#2a2d45"); minusBtn.Click += (_, _) => { devRef.Temperature = Math.Max(16, devRef.Temperature - 1); SmartHomeStore.Save(_data); Render(); };
            var plusBtn = MakeBtn("+", "#2a2d45"); plusBtn.Click += (_, _) => { devRef.Temperature = Math.Min(30, devRef.Temperature + 1); SmartHomeStore.Save(_data); Render(); };
            tempRow.Children.Add(minusBtn); tempRow.Children.Add(plusBtn);
            sp.Children.Add(tempRow);
        }

        // Delete
        var delBtn = MakeBtn("🗑️", "#e74c3c33");
        delBtn.Click += (_, _) =>
        {
            if (MessageBox.Show($"ลบ \"{devRef.Name}\"?", "ยืนยัน", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _data.Devices.Remove(devRef); SmartHomeStore.Save(_data); Render();
            }
        };
        delBtn.HorizontalAlignment = HorizontalAlignment.Right;
        delBtn.Margin = new Thickness(0, 4, 0, 0);
        sp.Children.Add(delBtn);

        card.Child = sp;
        Grid.SetColumn(card, col);
        Grid.SetRow(card, row);
        grid.Children.Add(card);
    }

    private void AddDevice_Click(object sender, RoutedEventArgs e)
    {
        var win = new Window { Title = "เพิ่มอุปกรณ์", Width = 400, Height = 440,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = this, Background = B("#0f1018") };
        var sp = new StackPanel { Margin = new Thickness(16) };

        sp.Children.Add(Txt("🏠 เพิ่มอุปกรณ์อัจฉริยะ", 16, "#e8e9f0"));

        sp.Children.Add(Txt("ชื่ออุปกรณ์", 12, "#8b8fa7"));
        var txtName = new TextBox { FontSize = 13, Padding = new Thickness(6), Background = B("#2a2d45"), Foreground = B("#e8e9f0"), BorderBrush = B("#2a2d45") };
        sp.Children.Add(txtName);

        sp.Children.Add(Txt("ประเภท", 12, "#8b8fa7"));
        var cmbType = new ComboBox { FontSize = 13, Padding = new Thickness(6) };
        cmbType.ItemsSource = DeviceTypes.Select(t => $"{t.Icon} {t.Name}").ToList();
        cmbType.SelectedIndex = 0;
        sp.Children.Add(cmbType);

        sp.Children.Add(Txt("ห้อง", 12, "#8b8fa7"));
        var cmbRoom = new ComboBox { FontSize = 13, Padding = new Thickness(6) };
        cmbRoom.ItemsSource = Rooms;
        cmbRoom.SelectedIndex = 0;
        sp.Children.Add(cmbRoom);

        sp.Children.Add(Txt("แพลตฟอร์ม", 12, "#8b8fa7"));
        var cmbPlatform = new ComboBox { FontSize = 13, Padding = new Thickness(6) };
        cmbPlatform.ItemsSource = new[] { "demo (ทดสอบ)", "homeassistant", "tuya", "http", "mqtt" };
        cmbPlatform.SelectedIndex = 0;
        sp.Children.Add(cmbPlatform);

        sp.Children.Add(Txt("Device ID / Entity ID", 12, "#8b8fa7"));
        var txtDevId = new TextBox { FontSize = 12, Padding = new Thickness(6), Background = B("#2a2d45"), Foreground = B("#e8e9f0"), BorderBrush = B("#2a2d45") };
        sp.Children.Add(txtDevId);

        sp.Children.Add(Txt("API URL (สำหรับ HTTP)", 12, "#8b8fa7"));
        var txtUrl = new TextBox { FontSize = 12, Padding = new Thickness(6), Background = B("#2a2d45"), Foreground = B("#e8e9f0"), BorderBrush = B("#2a2d45") };
        sp.Children.Add(txtUrl);

        var saveBtn = MakeBtn("💾 เพิ่มอุปกรณ์", "#27ae60");
        saveBtn.Margin = new Thickness(0, 12, 0, 0);
        saveBtn.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("กรุณาใส่ชื่อ"); return; }
            var platform = (cmbPlatform.SelectedItem as string ?? "demo").Split(' ')[0];
            _data.Devices.Add(new SmartDevice
            {
                Name = txtName.Text.Trim(),
                Type = DeviceTypes[cmbType.SelectedIndex].Type,
                Room = cmbRoom.SelectedItem as string ?? "ห้องนั่งเล่น",
                Platform = platform,
                DeviceId = txtDevId.Text.Trim(),
                ApiUrl = txtUrl.Text.Trim(),
            });
            SmartHomeStore.Save(_data); Render(); win.Close();
        };
        sp.Children.Add(saveBtn);
        win.Content = sp;
        win.ShowDialog();
    }

    private static Border Badge(string text) => new()
    {
        Background = B("#2a2d45"), CornerRadius = new CornerRadius(6),
        Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(0, 0, 6, 0),
        Child = new TextBlock { Text = text, FontSize = 10, Foreground = B("#8b8fa7") }
    };
    private static Button MakeBtn(string text, string bg) => new()
    {
        Content = text, Padding = new Thickness(10, 5, 10, 5), FontSize = 11,
        Background = B(bg), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand
    };
    private static TextBlock Txt(string text, double size, string color) => new()
    {
        Text = text, FontSize = size, Foreground = B(color), Margin = new Thickness(0, 6, 0, 2)
    };
    private static SolidColorBrush B(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
}
