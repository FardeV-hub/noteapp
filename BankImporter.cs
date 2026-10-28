using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NoteApp;

public class BankTransaction
{
    public string Date { get; set; } = "";
    public string Time { get; set; } = "";
    public string Description { get; set; } = "";
    public double Amount { get; set; }
    public string Type { get; set; } = "expense";
    public string Category { get; set; } = "อื่นๆ";
    public string Bank { get; set; } = "";
    public double Balance { get; set; }
}

// ====================================================
// AI จัดหมวดหมู่อัตโนมัติจากชื่อรายการ
// ====================================================
public static class TransactionCategorizer
{
    private static readonly (string Category, string[] Keywords)[] Rules =
    {
        ("อาหาร", new[] { "7-eleven", "7-11", "makro", "big c", "lotus", "tops", "foodpanda", "grab food",
            "lineman", "robinhood", "แมคโดนัลด์", "mcdonald", "kfc", "pizza", "starbucks", "cafe",
            "ร้านอาหาร", "ข้าว", "กาแฟ", "ชานม", "อาหาร", "เซเว่น", "บิ๊กซี", "โลตัส", "แม็คโคร" }),

        ("เดินทาง", new[] { "grab", "bolt", "bts", "mrt", "airport", "toll", "gas", "shell", "ptt",
            "esso", "caltex", "bangchak", "taxi", "ค่าทางด่วน", "น้ำมัน", "ค่าเดินทาง",
            "รถไฟ", "เครื่องบิน", "ที่จอดรถ", "parking" }),

        ("ช้อปปิ้ง", new[] { "shopee", "lazada", "amazon", "central", "robinson", "uniqlo", "h&m",
            "zara", "nike", "adidas", "apple", "samsung", "เสื้อผ้า", "รองเท้า", "กระเป๋า",
            "ช้อปปิ้ง", "สินค้า", "ออนไลน์" }),

        ("ที่พัก", new[] { "ค่าเช่า", "คอนโด", "หอพัก", "condo", "rent", "agoda", "booking",
            "airbnb", "โรงแรม", "hotel" }),

        ("สาธารณูปโภค", new[] { "ค่าน้ำ", "ค่าไฟ", "pea", "mea", "ประปา", "ค่าเน็ต", "true", "ais",
            "dtac", "3bb", "internet", "ค่าโทรศัพท์", "mobile", "ค่าส่วนกลาง" }),

        ("สุขภาพ", new[] { "โรงพยาบาล", "hospital", "คลินิก", "clinic", "ร้านยา", "pharmacy",
            "ทันตกรรม", "หมอ", "watsons", "boots", "ประกัน", "insurance" }),

        ("การศึกษา", new[] { "ค่าเทอม", "หนังสือ", "คอร์ส", "course", "udemy", "skillane",
            "โรงเรียน", "มหาวิทยาลัย", "tuition" }),

        ("บันเทิง", new[] { "netflix", "youtube", "spotify", "disney", "hbo", "เกม", "game",
            "steam", "cinema", "sf cinema", "major", "โรงหนัง", "คอนเสิร์ต", "ท่องเที่ยว" }),

        ("โอนเงิน", new[] { "โอนเงิน", "transfer", "promptpay", "พร้อมเพย์", "PromptPay" }),

        ("เงินเดือน", new[] { "เงินเดือน", "salary", "payroll", "bonus", "โบนัส" }),
    };

    public static string Categorize(string description)
    {
        var lower = description.ToLower();
        foreach (var (category, keywords) in Rules)
        {
            if (keywords.Any(kw => lower.Contains(kw.ToLower())))
                return category;
        }
        return "อื่นๆ";
    }

    public static string DetermineType(string description, double amount)
    {
        var lower = description.ToLower();
        var incomeKeywords = new[] { "เงินเดือน", "salary", "payroll", "bonus", "โบนัส", "รับเงิน",
            "เงินเข้า", "received", "credit", "refund", "คืนเงิน", "ดอกเบี้ย", "interest", "เงินปันผล" };
        if (incomeKeywords.Any(kw => lower.Contains(kw.ToLower()))) return "income";
        if (amount > 0 && lower.Contains("เข้า")) return "income";
        return "expense";
    }
}

// ====================================================
// CSV Statement Parser (รองรับหลายธนาคาร)
// ====================================================
public static class BankStatementParser
{
    public static List<BankTransaction> ParseCSV(string filePath, string bankName)
    {
        var lines = File.ReadAllLines(filePath);
        var transactions = new List<BankTransaction>();

        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = SplitCsv(line);
            if (parts.Length < 3) continue;

            var tx = TryParseLine(parts, bankName);
            if (tx != null) transactions.Add(tx);
        }

        foreach (var tx in transactions)
        {
            tx.Category = TransactionCategorizer.Categorize(tx.Description);
            tx.Type = TransactionCategorizer.DetermineType(tx.Description, tx.Amount);
            tx.Bank = bankName;
        }

        return transactions;
    }

    private static BankTransaction? TryParseLine(string[] parts, string bank)
    {
        var tx = new BankTransaction();

        try
        {
            // Generic format: Date, Description, Withdraw/Deposit, Balance
            // Or: Date, Time, Description, Amount, Balance
            if (parts.Length >= 4)
            {
                tx.Date = CleanDate(parts[0].Trim());
                if (parts.Length >= 5)
                {
                    tx.Time = parts[1].Trim();
                    tx.Description = parts[2].Trim();
                    double.TryParse(CleanNumber(parts[3]), out var amt);
                    tx.Amount = Math.Abs(amt);
                    double.TryParse(CleanNumber(parts[4]), out var bal);
                    tx.Balance = bal;
                }
                else
                {
                    tx.Description = parts[1].Trim();
                    double.TryParse(CleanNumber(parts[2]), out var amt);
                    tx.Amount = Math.Abs(amt);
                    double.TryParse(CleanNumber(parts[3]), out var bal);
                    tx.Balance = bal;
                }
            }

            if (string.IsNullOrEmpty(tx.Date) || tx.Amount <= 0) return null;
            return tx;
        }
        catch { return null; }
    }

    private static string CleanDate(string s)
    {
        s = s.Trim('"', ' ');
        if (DateTime.TryParse(s, out var dt)) return dt.ToString("yyyy-MM-dd");
        if (DateTime.TryParseExact(s, new[] { "dd/MM/yyyy", "dd/MM/yy", "yyyy-MM-dd", "d/M/yyyy" },
            CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            return dt.ToString("yyyy-MM-dd");
        return s;
    }

    private static string CleanNumber(string s) =>
        s.Trim('"', ' ').Replace(",", "").Replace("฿", "").Replace("THB", "").Trim();

    private static string[] SplitCsv(string line)
    {
        var result = new List<string>();
        var current = "";
        var inQuotes = false;
        foreach (var ch in line)
        {
            if (ch == '"') inQuotes = !inQuotes;
            else if (ch == ',' && !inQuotes) { result.Add(current); current = ""; }
            else current += ch;
        }
        result.Add(current);
        return result.ToArray();
    }
}

// ====================================================
// SMS/Notification Parser
// ====================================================
public static class SMSParser
{
    public static BankTransaction? Parse(string text)
    {
        var tx = new BankTransaction { Date = DateTime.Today.ToString("yyyy-MM-dd") };

        // Amount patterns: ฿1,234.56 or 1,234.56 บาท or THB 1234
        var amtMatch = Regex.Match(text, @"[฿]?\s*([\d,]+\.?\d*)\s*(บาท|THB|฿)?", RegexOptions.IgnoreCase);
        if (amtMatch.Success)
        {
            var numStr = amtMatch.Groups[1].Value.Replace(",", "");
            double.TryParse(numStr, out var amt);
            tx.Amount = Math.Abs(amt);
        }

        if (tx.Amount <= 0) return null;

        // Balance
        var balMatch = Regex.Match(text, @"คงเหลือ\s*[฿]?\s*([\d,]+\.?\d*)", RegexOptions.IgnoreCase);
        if (!balMatch.Success) balMatch = Regex.Match(text, @"bal[a-z]*\s*[:]?\s*[฿]?\s*([\d,]+\.?\d*)", RegexOptions.IgnoreCase);
        if (balMatch.Success && double.TryParse(balMatch.Groups[1].Value.Replace(",", ""), out var bal)) tx.Balance = bal;

        // Date
        var dateMatch = Regex.Match(text, @"(\d{1,2}[/\-]\d{1,2}[/\-]\d{2,4})");
        if (dateMatch.Success) tx.Date = dateMatch.Groups[1].Value;

        // Bank detection
        if (Regex.IsMatch(text, @"kbank|กสิกร", RegexOptions.IgnoreCase)) tx.Bank = "KBank";
        else if (Regex.IsMatch(text, @"scb|ไทยพาณิชย์", RegexOptions.IgnoreCase)) tx.Bank = "SCB";
        else if (Regex.IsMatch(text, @"bbl|กรุงเทพ", RegexOptions.IgnoreCase)) tx.Bank = "BBL";
        else if (Regex.IsMatch(text, @"ktb|กรุงไทย", RegexOptions.IgnoreCase)) tx.Bank = "KTB";
        else if (Regex.IsMatch(text, @"ttb|ทหารไทย", RegexOptions.IgnoreCase)) tx.Bank = "ttb";
        else if (Regex.IsMatch(text, @"bay|กรุงศรี", RegexOptions.IgnoreCase)) tx.Bank = "BAY";
        else tx.Bank = "ไม่ระบุ";

        // Type
        tx.Type = Regex.IsMatch(text, @"รับ|เข้า|โอนเข้า|received|credit|salary|เงินเดือน", RegexOptions.IgnoreCase)
            ? "income" : "expense";

        // Description
        tx.Description = text.Length > 100 ? text[..100] : text;
        tx.Category = TransactionCategorizer.Categorize(text);

        return tx;
    }
}
