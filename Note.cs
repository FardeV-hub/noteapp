using System;

namespace NoteApp;

public class Note
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string Category { get; set; } = "ทั่วไป";
    public string Priority { get; set; } = "ปกติ";
    public bool Pinned { get; set; }
    public string Created { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
    public string Reminder { get; set; } = "";
    public int RemindMinutes { get; set; } = -1;
    public bool Notified { get; set; }
    public bool Locked { get; set; }
    public string Password { get; set; } = "";
    public bool Deleted { get; set; }
    public string DeletedAt { get; set; } = "";

    public string PriorityIcon => Priority switch
    {
        "สูง" => "🔴",
        "ปานกลาง" => "🟡",
        "ต่ำ" => "🟢",
        _ => "⚪"
    };

    public int PriorityRank => Priority switch
    {
        "สูง" => 1,
        "ปานกลาง" => 2,
        "ต่ำ" => 4,
        _ => 3
    };
}
