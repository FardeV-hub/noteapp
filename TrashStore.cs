using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace NoteApp;

public static class TrashStore
{
    private static readonly string FilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "trash.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static List<Note> Load()
    {
        if (!File.Exists(FilePath)) return new List<Note>();
        var json = File.ReadAllText(FilePath);
        return JsonSerializer.Deserialize<List<Note>>(json, JsonOpts) ?? new List<Note>();
    }

    public static void Save(List<Note> notes)
    {
        File.WriteAllText(FilePath, JsonSerializer.Serialize(notes, JsonOpts));
    }
}
