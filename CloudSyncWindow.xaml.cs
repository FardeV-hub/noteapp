using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace NoteApp;

public partial class CloudSyncWindow : Window
{
    private CloudConfig _config;
    private readonly Action _onNotesChanged;

    public CloudSyncWindow(Action onNotesChanged)
    {
        InitializeComponent();
        _onNotesChanged = onNotesChanged;
        _config = CloudConfigStore.Load();

        txtGistToken.Text = _config.GistToken;
        txtGistId.Text = _config.GistId;
        txtFirebaseUrl.Text = _config.FirebaseUrl;
        txtApiKey.Text = _config.ApiKey;

        UpdateSyncInfo();
        if (!string.IsNullOrEmpty(_config.LastSync))
            lblStatus.Text = $"Sync ล่าสุด: {_config.LastSync}";
    }

    private void UpdateSyncInfo()
    {
        var notes = NoteStore.Load();
        var community = CommunityStore.Load();
        lblSyncInfo.Text = $"โน้ตในเครื่อง: {notes.Count} รายการ  ·  Community: {community.Count} โพสต์";
    }

    private void Log(string msg)
    {
        var time = DateTime.Now.ToString("HH:mm:ss");
        lblLog.Text = $"[{time}] {msg}\n{lblLog.Text}";
        if (lblLog.Text.Length > 800) lblLog.Text = lblLog.Text[..800];
    }

    private void MarkSync()
    {
        _config.LastSync = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        CloudConfigStore.Save(_config);
        lblStatus.Text = $"Sync ล่าสุด: {_config.LastSync}";
    }

    // ====================================================
    // GitHub Gist
    // ====================================================
    private void SaveGistConfig_Click(object sender, RoutedEventArgs e)
    {
        _config.GistToken = txtGistToken.Text.Trim();
        _config.GistId = txtGistId.Text.Trim();
        try
        {
            CloudConfigStore.Save(_config);
            Log("บันทึกการตั้งค่า Gist แล้ว");
            MessageBox.Show($"บันทึกสำเร็จ!\n\nToken: {(_config.GistToken.Length > 8 ? _config.GistToken[..8] + "..." : "(ว่าง)")}\nGist ID: {(_config.GistId.Length > 0 ? _config.GistId : "(ยังไม่มี — กดสร้าง Gist ใหม่)")}", "Cloud Sync");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"บันทึกไม่ได้:\n{ex.Message}\n\nลองรันแอปในฐานะ Administrator", "Cloud Sync");
        }
    }

    private async void CreateGist_Click(object sender, RoutedEventArgs e)
    {
        var token = txtGistToken.Text.Trim();
        if (string.IsNullOrEmpty(token))
        {
            MessageBox.Show("กรุณาใส่ Personal Access Token ก่อน");
            return;
        }

        Log("กำลังสร้าง Gist ใหม่...");
        var gistId = await GistSyncService.CreateGist(token);
        if (gistId != null)
        {
            txtGistId.Text = gistId;
            _config.GistToken = token;
            _config.GistId = gistId;
            CloudConfigStore.Save(_config);
            Log($"สร้าง Gist สำเร็จ — ID: {gistId}");
            MessageBox.Show($"สร้าง Gist สำเร็จ!\n\nGist ID: {gistId}\n\nข้อมูลเก็บเป็น private Gist\nสามารถ Push/Pull ได้เลย", "Cloud Sync");
        }
        else
        {
            Log("สร้าง Gist ไม่สำเร็จ — ตรวจสอบ Token");
            MessageBox.Show("สร้าง Gist ไม่สำเร็จ\n\nตรวจสอบ:\n• Token ถูกต้อง\n• Token มี scope 'gist'", "Cloud Sync");
        }
    }

    private async void TestGist_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_config.GistToken) || string.IsNullOrEmpty(_config.GistId))
        {
            MessageBox.Show("กรุณาใส่ Token และ Gist ID แล้วบันทึกก่อน");
            return;
        }
        Log("กำลังทดสอบเชื่อมต่อ Gist...");
        var ok = await GistSyncService.TestConnection(_config.GistToken, _config.GistId);
        Log(ok ? "เชื่อมต่อ Gist สำเร็จ" : "เชื่อมต่อไม่สำเร็จ");
        lblStatus.Text = ok ? "เชื่อมต่อ Gist สำเร็จ" : "เชื่อมต่อไม่สำเร็จ";
        MessageBox.Show(ok ? "เชื่อมต่อ Gist สำเร็จ!" : "เชื่อมต่อไม่สำเร็จ ตรวจสอบ Token/Gist ID", "Cloud Sync");
    }

    private async void GistPushNotes_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckGist()) return;
        var notes = NoteStore.Load();
        Log($"กำลัง Push {notes.Count} โน้ตขึ้น Gist...");
        lblStatus.Text = "กำลัง Push...";
        var ok = await GistSyncService.PushNotes(_config.GistToken, _config.GistId, notes);
        if (ok)
        {
            MarkSync();
            Log($"Push {notes.Count} โน้ตสำเร็จ");
            MessageBox.Show($"Push {notes.Count} โน้ตขึ้น Gist สำเร็จ!", "Cloud Sync");
        }
        else
        {
            lblStatus.Text = "Push ไม่สำเร็จ";
            Log("Push ไม่สำเร็จ");
            MessageBox.Show("Push ไม่สำเร็จ ตรวจสอบการเชื่อมต่อ", "Cloud Sync");
        }
    }

    private async void GistPullNotes_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckGist()) return;
        Log("กำลัง Pull โน้ตจาก Gist...");
        lblStatus.Text = "กำลัง Pull...";
        var notes = await GistSyncService.PullNotes(_config.GistToken, _config.GistId);
        if (notes == null)
        {
            lblStatus.Text = "Pull ไม่สำเร็จ";
            MessageBox.Show("Pull ไม่สำเร็จ", "Cloud Sync");
            return;
        }
        if (!notes.Any())
        {
            MessageBox.Show("ไม่มีโน้ตบน Gist\nลอง Push ขึ้นไปก่อน", "Cloud Sync");
            return;
        }

        var result = MessageBox.Show(
            $"พบ {notes.Count} โน้ตบน Gist\n\nYes = แทนที่ทั้งหมด\nNo = รวมเข้ากับข้อมูลเดิม",
            "Pull จาก Gist", MessageBoxButton.YesNoCancel);
        if (result == MessageBoxResult.Cancel) return;

        if (result == MessageBoxResult.Yes)
        {
            NoteStore.Save(notes);
            Log($"แทนที่ด้วย {notes.Count} โน้ตจาก Gist");
        }
        else
        {
            var local = NoteStore.Load();
            var ids = local.Select(n => n.Id).ToHashSet();
            var added = notes.Where(n => !ids.Contains(n.Id)).ToList();
            local.AddRange(added);
            NoteStore.Save(local);
            Log($"รวม {added.Count} โน้ตใหม่ (ซ้ำ {notes.Count - added.Count})");
        }
        MarkSync();
        _onNotesChanged();
        UpdateSyncInfo();
        MessageBox.Show("Pull สำเร็จ!", "Cloud Sync");
    }

    private async void GistPushCommunity_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckGist()) return;
        var posts = CommunityStore.Load();
        Log($"กำลัง Push {posts.Count} โพสต์...");
        var ok = await GistSyncService.PushCommunity(_config.GistToken, _config.GistId, posts);
        if (ok) { MarkSync(); Log("Push Community สำเร็จ"); MessageBox.Show("Push สำเร็จ!", "Cloud Sync"); }
        else { Log("Push Community ไม่สำเร็จ"); MessageBox.Show("Push ไม่สำเร็จ", "Cloud Sync"); }
    }

    private async void GistPullCommunity_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckGist()) return;
        Log("กำลัง Pull Community...");
        var posts = await GistSyncService.PullCommunity(_config.GistToken, _config.GistId);
        if (posts == null) { MessageBox.Show("Pull ไม่สำเร็จ", "Cloud Sync"); return; }
        CommunityStore.Save(posts);
        MarkSync();
        UpdateSyncInfo();
        Log($"Pull {posts.Count} โพสต์สำเร็จ");
        MessageBox.Show($"Pull {posts.Count} โพสต์สำเร็จ!", "Cloud Sync");
    }

    private bool CheckGist()
    {
        if (string.IsNullOrEmpty(_config.GistToken) || string.IsNullOrEmpty(_config.GistId))
        {
            MessageBox.Show("กรุณาตั้งค่า Token + Gist ID แล้วบันทึกก่อน");
            return false;
        }
        return true;
    }

    // ====================================================
    // Firebase (เดิม)
    // ====================================================
    private void SaveConfig_Click(object sender, RoutedEventArgs e)
    {
        _config.FirebaseUrl = txtFirebaseUrl.Text.Trim();
        _config.ApiKey = txtApiKey.Text.Trim();
        CloudConfigStore.Save(_config);
        Log("บันทึกการตั้งค่า Firebase แล้ว");
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_config.FirebaseUrl)) { MessageBox.Show("กรุณาใส่ Firebase URL"); return; }
        Log("กำลังทดสอบ Firebase...");
        var ok = await CloudSyncService.TestConnection(_config);
        Log(ok ? "Firebase สำเร็จ" : "Firebase ไม่สำเร็จ");
        MessageBox.Show(ok ? "เชื่อมต่อ Firebase สำเร็จ!" : "เชื่อมต่อไม่สำเร็จ", "Cloud Sync");
    }

    private async void PushNotes_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_config.FirebaseUrl)) { MessageBox.Show("กรุณาใส่ Firebase URL"); return; }
        var notes = NoteStore.Load();
        var ok = await CloudSyncService.PushNotes(_config, notes, App.CurrentUser ?? "default");
        if (ok) { MarkSync(); MessageBox.Show($"Push {notes.Count} โน้ตสำเร็จ!", "Cloud Sync"); }
        else MessageBox.Show("Push ไม่สำเร็จ", "Cloud Sync");
    }

    private async void PullNotes_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_config.FirebaseUrl)) { MessageBox.Show("กรุณาใส่ Firebase URL"); return; }
        var notes = await CloudSyncService.PullNotes(_config, App.CurrentUser ?? "default");
        if (notes == null || !notes.Any()) { MessageBox.Show("ไม่มีข้อมูลบน Firebase"); return; }
        NoteStore.Save(notes);
        MarkSync();
        _onNotesChanged();
        UpdateSyncInfo();
        MessageBox.Show($"Pull {notes.Count} โน้ตสำเร็จ!", "Cloud Sync");
    }

    private async void PushCommunity_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_config.FirebaseUrl)) { MessageBox.Show("กรุณาใส่ Firebase URL"); return; }
        var posts = CommunityStore.Load();
        var ok = await CloudSyncService.PushCommunity(_config, posts);
        if (ok) { MarkSync(); MessageBox.Show("Push สำเร็จ!", "Cloud Sync"); }
        else MessageBox.Show("Push ไม่สำเร็จ", "Cloud Sync");
    }

    private async void PullCommunity_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_config.FirebaseUrl)) { MessageBox.Show("กรุณาใส่ Firebase URL"); return; }
        var posts = await CloudSyncService.PullCommunity(_config);
        if (posts == null) { MessageBox.Show("Pull ไม่สำเร็จ"); return; }
        CommunityStore.Save(posts);
        MarkSync();
        UpdateSyncInfo();
        MessageBox.Show($"Pull {posts.Count} โพสต์สำเร็จ!", "Cloud Sync");
    }
}
