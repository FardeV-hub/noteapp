using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace NoteApp;

public class AppTheme
{
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Bg { get; set; } = "";
    public string Surface { get; set; } = "";
    public string Surface2 { get; set; } = "";
    public string Text { get; set; } = "";
    public string Text2 { get; set; } = "";
    public string Accent { get; set; } = "";
    public string CardBorder { get; set; } = "";
    public bool IsDark { get; set; }
}

public static class ThemeManager
{
    private static readonly string SettingsFile = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "theme.json");

    public static readonly List<AppTheme> Themes = new()
    {
        // ===== Dark =====
        new() { Name = "Midnight", Icon = "🌙", IsDark = true,
            Bg = "#13151c", Surface = "#1c1f2a", Surface2 = "#272b38",
            Text = "#d8dae2", Text2 = "#7d8298", Accent = "#7b93f7", CardBorder = "#2e3242" },

        new() { Name = "Catppuccin Mocha", Icon = "🐱", IsDark = true,
            Bg = "#1e1e2e", Surface = "#28283d", Surface2 = "#363650",
            Text = "#cdd6f4", Text2 = "#a6adc8", Accent = "#cba6f7", CardBorder = "#3a3a52" },

        new() { Name = "Dracula", Icon = "🧛", IsDark = true,
            Bg = "#21222c", Surface = "#2b2d3a", Surface2 = "#3a3d50",
            Text = "#f8f8f2", Text2 = "#6272a4", Accent = "#bd93f9", CardBorder = "#44475a" },

        new() { Name = "Nord", Icon = "❄", IsDark = true,
            Bg = "#242933", Surface = "#2e3440", Surface2 = "#3b4252",
            Text = "#eceff4", Text2 = "#81a1c1", Accent = "#88c0d0", CardBorder = "#434c5e" },

        new() { Name = "Tokyo Night", Icon = "🗼", IsDark = true,
            Bg = "#16161e", Surface = "#1a1b26", Surface2 = "#292e42",
            Text = "#c0caf5", Text2 = "#565f89", Accent = "#7aa2f7", CardBorder = "#33374d" },

        new() { Name = "Ocean", Icon = "🌊", IsDark = true,
            Bg = "#0b1622", Surface = "#132030", Surface2 = "#1e3040",
            Text = "#dce6f0", Text2 = "#6b8ea5", Accent = "#48b5e0", CardBorder = "#253848" },

        new() { Name = "Forest", Icon = "🌲", IsDark = true,
            Bg = "#141c12", Surface = "#1c261a", Surface2 = "#283626",
            Text = "#d0dcc6", Text2 = "#7a9468", Accent = "#5cb85c", CardBorder = "#324430" },

        new() { Name = "Rose Pine", Icon = "🌹", IsDark = true,
            Bg = "#141220", Surface = "#1c1a2a", Surface2 = "#28263a",
            Text = "#e0def4", Text2 = "#908caa", Accent = "#eb6f92", CardBorder = "#332f48" },

        new() { Name = "Ember", Icon = "🔥", IsDark = true,
            Bg = "#181210", Surface = "#221c18", Surface2 = "#302822",
            Text = "#e8ddd4", Text2 = "#a08878", Accent = "#e8734a", CardBorder = "#3a302a" },

        // ===== Light =====
        new() { Name = "Clean", Icon = "☀", IsDark = false,
            Bg = "#f5f6f8", Surface = "#ffffff", Surface2 = "#eef0f4",
            Text = "#1a1d26", Text2 = "#6b7280", Accent = "#4f6df5", CardBorder = "#dfe1e6" },

        new() { Name = "Warm Paper", Icon = "📜", IsDark = false,
            Bg = "#f8f2e4", Surface = "#fffcf5", Surface2 = "#efe5cf",
            Text = "#3a3020", Text2 = "#8a7a5a", Accent = "#b87830", CardBorder = "#e0d5b8" },

        new() { Name = "Sky", Icon = "🌤", IsDark = false,
            Bg = "#eaf3fc", Surface = "#ffffff", Surface2 = "#d6e8f8",
            Text = "#18344e", Text2 = "#5880a0", Accent = "#2f80d0", CardBorder = "#c0d8f0" },

        new() { Name = "Sakura", Icon = "🌸", IsDark = false,
            Bg = "#fdf0f4", Surface = "#ffffff", Surface2 = "#f8dce6",
            Text = "#40202e", Text2 = "#a06880", Accent = "#d84878", CardBorder = "#f0c8d8" },

        new() { Name = "Mint", Icon = "🍃", IsDark = false,
            Bg = "#ecf6ec", Surface = "#ffffff", Surface2 = "#d4ead4",
            Text = "#183418", Text2 = "#508050", Accent = "#28a060", CardBorder = "#b8d8b8" },

        new() { Name = "Lavender", Icon = "💜", IsDark = false,
            Bg = "#f0ecf6", Surface = "#ffffff", Surface2 = "#e0d8ee",
            Text = "#281838", Text2 = "#6a5a8a", Accent = "#7c40b0", CardBorder = "#d0c0e0" },

        // ===== Office =====
        new() { Name = "Office Blue", Icon = "🏢", IsDark = false,
            Bg = "#edf2f7", Surface = "#ffffff", Surface2 = "#e2e8f0",
            Text = "#1a202c", Text2 = "#4a5568", Accent = "#2b6cb0", CardBorder = "#c8daf0" },

        new() { Name = "Office Dark", Icon = "🏢", IsDark = true,
            Bg = "#1a1a1e", Surface = "#242428", Surface2 = "#303036",
            Text = "#d4d4d8", Text2 = "#72727e", Accent = "#3b82f6", CardBorder = "#3a3a42" },

        new() { Name = "Office Green", Icon = "📊", IsDark = false,
            Bg = "#f0f5f0", Surface = "#ffffff", Surface2 = "#dce8dc",
            Text = "#1a2e1a", Text2 = "#4a6a4a", Accent = "#217346", CardBorder = "#b0d4b0" },

        new() { Name = "Office Red", Icon = "📕", IsDark = false,
            Bg = "#fcf2f2", Surface = "#ffffff", Surface2 = "#f8e4e4",
            Text = "#2c1818", Text2 = "#805050", Accent = "#c53030", CardBorder = "#f0b8b8" },

        new() { Name = "Office Purple", Icon = "💼", IsDark = false,
            Bg = "#f4f0ff", Surface = "#ffffff", Surface2 = "#e6ddf0",
            Text = "#281e48", Text2 = "#605080", Accent = "#6b46c1", CardBorder = "#d0c0e8" },
    };

    public static string LoadSavedTheme()
    {
        if (!File.Exists(SettingsFile)) return "Midnight";
        try
        {
            var json = File.ReadAllText(SettingsFile);
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("theme").GetString() ?? "Midnight";
        }
        catch { return "Midnight"; }
    }

    public static void SaveTheme(string name)
    {
        File.WriteAllText(SettingsFile, $"{{\"theme\":\"{name}\"}}");
    }

    public static AppTheme GetTheme(string name)
    {
        return Themes.Find(t => t.Name == name) ?? Themes[0];
    }
}
