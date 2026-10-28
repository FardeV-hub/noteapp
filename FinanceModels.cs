using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace NoteApp;

public class Transaction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = "expense";
    public string Title { get; set; } = "";
    public double Amount { get; set; }
    public string Category { get; set; } = "ทั่วไป";
    public string Date { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");
    public string Note { get; set; } = "";
    public string Created { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

    public string TypeIcon => Type == "income" ? "💵" : "💸";
    public string TypeLabel => Type == "income" ? "รายรับ" : "รายจ่าย";
    public string AmountDisplay => Type == "income" ? $"+{Amount:N0}" : $"-{Amount:N0}";
    public string AmountColor => Type == "income" ? "#27ae60" : "#e74c3c";
}

public class Bill
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "";
    public double Amount { get; set; }
    public string DueDate { get; set; } = "";
    public string Category { get; set; } = "ทั่วไป";
    public bool Paid { get; set; }
    public string PaidDate { get; set; } = "";
    public string Note { get; set; } = "";
    public bool Recurring { get; set; }
    public string RecurringType { get; set; } = "";
    public string Created { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

    public string StatusIcon => Paid ? "✅" : IsOverdue ? "🔴" : "🟡";
    public string StatusLabel => Paid ? "จ่ายแล้ว" : IsOverdue ? "เลยกำหนด!" : "รอจ่าย";
    public bool IsOverdue => !Paid && DateTime.TryParse(DueDate, out var d) && d.Date < DateTime.Today;
}

public class BankAccount
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string BankName { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public double Balance { get; set; }
    public string Color { get; set; } = "#0984e3";
    public string Icon { get; set; } = "🏦";
}

public class FinanceData
{
    public List<Transaction> Transactions { get; set; } = new();
    public List<Bill> Bills { get; set; } = new();
    public List<BankAccount> Accounts { get; set; } = new();
}

public static class FinanceStore
{
    private static readonly string FilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "finance.json");
    private static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static FinanceData Load()
    {
        if (!File.Exists(FilePath)) return new FinanceData();
        return JsonSerializer.Deserialize<FinanceData>(File.ReadAllText(FilePath), Opts) ?? new();
    }

    public static void Save(FinanceData data) =>
        File.WriteAllText(FilePath, JsonSerializer.Serialize(data, Opts));
}
