using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace NoteApp;

public class MeetingRoom
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public int Capacity { get; set; } = 10;
    public string Facilities { get; set; } = "";
    public string Color { get; set; } = "#6c63ff";
}

public class Booking
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RoomId { get; set; } = "";
    public string Topic { get; set; } = "";
    public string Date { get; set; } = "";
    public string StartTime { get; set; } = "";
    public string EndTime { get; set; } = "";
    public string Organizer { get; set; } = "";
    public string Participants { get; set; } = "";
    public string Status { get; set; } = "confirmed";
    public string NoteId { get; set; } = "";
    public string Created { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
}

public class MeetingData
{
    public List<MeetingRoom> Rooms { get; set; } = new();
    public List<Booking> Bookings { get; set; } = new();
}

public static class MeetingStore
{
    private static readonly string FilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "meetings.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static MeetingData Load()
    {
        if (!File.Exists(FilePath))
        {
            var data = new MeetingData();
            data.Rooms.AddRange(new[]
            {
                new MeetingRoom { Name = "ห้องประชุม A", Capacity = 8, Facilities = "โปรเจกเตอร์, ไวท์บอร์ด", Color = "#6c63ff" },
                new MeetingRoom { Name = "ห้องประชุม B", Capacity = 15, Facilities = "โปรเจกเตอร์, จอ TV, ไมค์", Color = "#e84393" },
                new MeetingRoom { Name = "ห้องประชุม C", Capacity = 4, Facilities = "ไวท์บอร์ด", Color = "#00b894" },
                new MeetingRoom { Name = "ห้องประชุมใหญ่", Capacity = 30, Facilities = "โปรเจกเตอร์, ไมค์, ลำโพง, กล้อง", Color = "#fdcb6e" },
            });
            Save(data);
            return data;
        }
        var json = File.ReadAllText(FilePath);
        return JsonSerializer.Deserialize<MeetingData>(json, JsonOpts) ?? new MeetingData();
    }

    public static void Save(MeetingData data)
    {
        File.WriteAllText(FilePath, JsonSerializer.Serialize(data, JsonOpts));
    }
}
