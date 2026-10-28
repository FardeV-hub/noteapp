using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace NoteApp;

public class UserProfileData
{
    public string DisplayName { get; set; } = "";
    public string Bio { get; set; } = "";
    public string AvatarPath { get; set; } = "";
    public string Role { get; set; } = "สมาชิก";
    public string Email { get; set; } = "";
    public string School { get; set; } = "";
    public string Company { get; set; } = "";
    public string JoinedDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
}

public static class UserProfileStore
{
    private static readonly string FilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "profile.json");
    private static readonly string AvatarDir = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "avatars");
    private static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static UserProfileData Load()
    {
        if (!File.Exists(FilePath)) return new();
        try { return JsonSerializer.Deserialize<UserProfileData>(File.ReadAllText(FilePath), Opts) ?? new(); }
        catch { return new(); }
    }

    public static void Save(UserProfileData profile) =>
        File.WriteAllText(FilePath, JsonSerializer.Serialize(profile, Opts));

    public static string SaveAvatar(string sourcePath)
    {
        Directory.CreateDirectory(AvatarDir);
        var ext = Path.GetExtension(sourcePath);
        var destPath = Path.Combine(AvatarDir, $"avatar{ext}");
        File.Copy(sourcePath, destPath, true);
        return destPath;
    }
}
