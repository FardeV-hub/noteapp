using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace NoteApp;

public class Subject
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string Teacher { get; set; } = "";
    public string Color { get; set; } = "#6c63ff";
    public int Credits { get; set; } = 3;
}

public class Assignment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SubjectId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string DueDate { get; set; } = "";
    public string Status { get; set; } = "pending";
    public string StatusIcon => Status switch { "done" => "✅", "doing" => "🔄", _ => "⏳" };
    public string StatusLabel => Status switch { "done" => "ส่งแล้ว", "doing" => "กำลังทำ", _ => "ยังไม่ทำ" };
    public bool IsOverdue => Status != "done" && DateTime.TryParse(DueDate, out var d) && d.Date < DateTime.Today;
}

public class Grade
{
    public string SubjectId { get; set; } = "";
    public string ExamName { get; set; } = "";
    public double Score { get; set; }
    public double MaxScore { get; set; } = 100;
    public string Date { get; set; } = "";
}

public class ScheduleSlot
{
    public string SubjectId { get; set; } = "";
    public string Day { get; set; } = "";
    public string StartTime { get; set; } = "";
    public string EndTime { get; set; } = "";
    public string Room { get; set; } = "";
}

public class FlashCard
{
    public string Question { get; set; } = "";
    public string Answer { get; set; } = "";
    public int Level { get; set; }
}

public class StudyData
{
    public List<Subject> Subjects { get; set; } = new();
    public List<Assignment> Assignments { get; set; } = new();
    public List<Grade> Grades { get; set; } = new();
    public List<ScheduleSlot> Schedule { get; set; } = new();
    public List<FlashCard> FlashCards { get; set; } = new();
    public int PomodoroTotal { get; set; }
}

public static class StudyStore
{
    private static readonly string FilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "study.json");
    private static readonly JsonSerializerOptions Opts = new()
    { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static StudyData Load()
    {
        if (!File.Exists(FilePath)) return new StudyData();
        try { return JsonSerializer.Deserialize<StudyData>(File.ReadAllText(FilePath), Opts) ?? new(); }
        catch { return new(); }
    }
    public static void Save(StudyData d) => File.WriteAllText(FilePath, JsonSerializer.Serialize(d, Opts));
}

public static class GPACalculator
{
    public static double CalcGPA(List<(double grade, int credits)> subjects)
    {
        if (!subjects.Any()) return 0;
        var totalPoints = subjects.Sum(s => s.grade * s.credits);
        var totalCredits = subjects.Sum(s => s.credits);
        return totalCredits > 0 ? totalPoints / totalCredits : 0;
    }

    public static double ScoreToGrade(double percent) => percent switch
    {
        >= 80 => 4.0,
        >= 75 => 3.5,
        >= 70 => 3.0,
        >= 65 => 2.5,
        >= 60 => 2.0,
        >= 55 => 1.5,
        >= 50 => 1.0,
        _ => 0
    };

    public static string GradeToLetter(double gpa) => gpa switch
    {
        4.0 => "A", 3.5 => "B+", 3.0 => "B", 2.5 => "C+",
        2.0 => "C", 1.5 => "D+", 1.0 => "D", _ => "F"
    };
}
