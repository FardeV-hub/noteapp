using System;
using System.Collections.Generic;
using System.Linq;

namespace NoteApp;

public class InvestmentPlan
{
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
    public string RiskLevel { get; set; } = "";
    public string RiskColor { get; set; } = "";
    public double ExpectedReturn { get; set; }
    public double AllocationPercent { get; set; }
    public string Description { get; set; } = "";
    public string Pros { get; set; } = "";
    public string Cons { get; set; } = "";
    public string Suitable { get; set; } = "";
}

public class InvestmentProjection
{
    public int Year { get; set; }
    public double Conservative { get; set; }
    public double Moderate { get; set; }
    public double Aggressive { get; set; }
}

public class FinancialHealth
{
    public string Grade { get; set; } = "";
    public string GradeColor { get; set; } = "";
    public int Score { get; set; }
    public List<string> Strengths { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public static class InvestmentAdvisor
{
    private static readonly List<InvestmentPlan> AllPlans = new()
    {
        new() { Name = "เงินฝากประจำ", Icon = "🏦", RiskLevel = "ต่ำมาก", RiskColor = "#27ae60",
            ExpectedReturn = 1.5, Description = "ฝากเงินกับธนาคาร ดอกเบี้ยคงที่ ปลอดภัย 100%",
            Pros = "ปลอดภัยสูงสุด, มีประกันเงินฝาก, สภาพคล่องดี",
            Cons = "ผลตอบแทนต่ำ, อาจไม่ชนะเงินเฟ้อ",
            Suitable = "เงินสำรองฉุกเฉิน, ผู้ที่ไม่ต้องการเสี่ยง" },

        new() { Name = "พันธบัตรรัฐบาล", Icon = "🏛️", RiskLevel = "ต่ำ", RiskColor = "#27ae60",
            ExpectedReturn = 2.8, Description = "ตราสารหนี้ที่รัฐบาลออก มีความมั่นคงสูง",
            Pros = "ปลอดภัยสูง, ผลตอบแทนแน่นอน, ได้ดอกเบี้ยสม่ำเสมอ",
            Cons = "ผลตอบแทนปานกลาง, ขายก่อนครบกำหนดอาจขาดทุน",
            Suitable = "ผู้ที่ต้องการรายได้คงที่, เกษียณ" },

        new() { Name = "กองทุนตราสารหนี้", Icon = "📄", RiskLevel = "ต่ำ-ปานกลาง", RiskColor = "#f39c12",
            ExpectedReturn = 3.5, Description = "กองทุนรวมที่ลงทุนในพันธบัตรและหุ้นกู้",
            Pros = "กระจายความเสี่ยง, มืออาชีพบริหาร, สภาพคล่องดี",
            Cons = "มีค่าธรรมเนียม, ผลตอบแทนไม่การันตี",
            Suitable = "ผู้เริ่มต้นลงทุน, ต้องการผลตอบแทนมากกว่าฝากออม" },

        new() { Name = "กองทุนรวมผสม", Icon = "🎯", RiskLevel = "ปานกลาง", RiskColor = "#f39c12",
            ExpectedReturn = 6.0, Description = "กองทุนที่ลงทุนทั้งหุ้นและตราสารหนี้ตามสัดส่วน",
            Pros = "สมดุลความเสี่ยง-ผลตอบแทน, มืออาชีพบริหาร",
            Cons = "ผลตอบแทนปานกลาง, มีความผันผวนบ้าง",
            Suitable = "ผู้ที่รับความเสี่ยงได้ปานกลาง, ลงทุนระยะกลาง 3-5 ปี" },

        new() { Name = "กองทุน SSF/RMF", Icon = "💎", RiskLevel = "ปานกลาง", RiskColor = "#f39c12",
            ExpectedReturn = 7.0, Description = "กองทุนประหยัดภาษี ลดหย่อนได้สูงสุดตามกฎ",
            Pros = "ลดหย่อนภาษีได้, บังคับออมระยะยาว, หลากหลายนโยบาย",
            Cons = "มีเงื่อนไขถือครอง, ไม่สามารถขายก่อนกำหนด",
            Suitable = "ผู้มีรายได้สูง, ต้องการลดภาษี, วางแผนเกษียณ" },

        new() { Name = "หุ้นไทย (SET)", Icon = "📈", RiskLevel = "สูง", RiskColor = "#e74c3c",
            ExpectedReturn = 8.5, Description = "ลงทุนในหุ้นบริษัทจดทะเบียนในตลาดหลักทรัพย์ไทย",
            Pros = "ผลตอบแทนสูง, ได้เงินปันผล, สภาพคล่องสูง",
            Cons = "ความผันผวนสูง, ต้องมีความรู้, อาจขาดทุน",
            Suitable = "ผู้มีประสบการณ์, ลงทุนระยะยาว 5+ ปี" },

        new() { Name = "หุ้นต่างประเทศ (US/Global)", Icon = "🌍", RiskLevel = "สูง", RiskColor = "#e74c3c",
            ExpectedReturn = 10.0, Description = "ลงทุนในหุ้นต่างประเทศ เช่น S&P 500, NASDAQ",
            Pros = "ผลตอบแทนสูงในระยะยาว, กระจายความเสี่ยงจากตลาดไทย",
            Cons = "ความเสี่ยงอัตราแลกเปลี่ยน, ต้องเข้าใจตลาดโลก",
            Suitable = "ผู้ต้องการกระจายพอร์ต, ลงทุนระยะยาว 5-10 ปี" },

        new() { Name = "ETF ดัชนี", Icon = "📊", RiskLevel = "ปานกลาง-สูง", RiskColor = "#e84393",
            ExpectedReturn = 8.0, Description = "กองทุนดัชนีที่ซื้อขายในตลาดหลักทรัพย์ ค่าธรรมเนียมต่ำ",
            Pros = "ค่าธรรมเนียมต่ำ, กระจายความเสี่ยง, ซื้อขายง่าย",
            Cons = "ผลตอบแทนตามตลาด, ไม่สามารถเอาชนะตลาดได้",
            Suitable = "ผู้เริ่มต้นลงทุนหุ้น, Passive investor" },

        new() { Name = "ทองคำ", Icon = "🥇", RiskLevel = "ปานกลาง", RiskColor = "#f39c12",
            ExpectedReturn = 5.5, Description = "ลงทุนในทองคำแท่งหรือกองทุนทองคำ",
            Pros = "ป้องกันเงินเฟ้อ, สินทรัพย์ปลอดภัยยามวิกฤต",
            Cons = "ไม่มีรายได้ระหว่างถือ, ราคาผันผวนระยะสั้น",
            Suitable = "กระจายพอร์ต, ป้องกันความเสี่ยง" },

        new() { Name = "อสังหาริมทรัพย์/REIT", Icon = "🏠", RiskLevel = "ปานกลาง-สูง", RiskColor = "#e84393",
            ExpectedReturn = 6.5, Description = "กองทุนอสังหาริมทรัพย์ รับเงินปันผลจากค่าเช่า",
            Pros = "รายได้สม่ำเสมอจากเงินปันผล, ป้องกันเงินเฟ้อ",
            Cons = "สภาพคล่องน้อยกว่าหุ้น, อิงเศรษฐกิจ",
            Suitable = "ผู้ต้องการรายได้ passive, ลงทุนระยะยาว" },

        new() { Name = "คริปโตเคอร์เรนซี", Icon = "₿", RiskLevel = "สูงมาก", RiskColor = "#e74c3c",
            ExpectedReturn = 15.0, Description = "สินทรัพย์ดิจิทัล เช่น Bitcoin, Ethereum",
            Pros = "ผลตอบแทนสูงมาก, เทคโนโลยีอนาคต, ซื้อขาย 24 ชม.",
            Cons = "ความผันผวนสูงมาก, อาจขาดทุน 50%+, กฎระเบียบไม่ชัด",
            Suitable = "ผู้รับความเสี่ยงสูงได้, ลงทุนเงินที่เสียได้, สัดส่วนน้อย 5-10%" },
    };

    // ====================================================
    // วิเคราะห์สุขภาพการเงิน
    // ====================================================
    public static FinancialHealth AnalyzeHealth(FinanceData data)
    {
        var health = new FinancialHealth();
        var thisMonth = DateTime.Today.ToString("yyyy-MM");
        var monthTx = data.Transactions.Where(t => t.Date.StartsWith(thisMonth)).ToList();
        var income = monthTx.Where(t => t.Type == "income").Sum(t => t.Amount);
        var expense = monthTx.Where(t => t.Type == "expense").Sum(t => t.Amount);
        var totalBalance = data.Accounts.Sum(a => a.Balance);
        var unpaidBills = data.Bills.Where(b => !b.Paid).Sum(b => b.Amount);
        var overdueBills = data.Bills.Count(b => b.IsOverdue);

        int score = 50;

        // Income > Expense
        if (income > 0 && expense < income * 0.7) { score += 15; health.Strengths.Add("✅ ใช้จ่ายน้อยกว่า 70% ของรายรับ — ดีมาก!"); }
        else if (income > 0 && expense < income) { score += 8; health.Strengths.Add("✅ ใช้จ่ายน้อยกว่ารายรับ"); }
        else if (income > 0) { score -= 10; health.Warnings.Add("⚠️ รายจ่ายมากกว่ารายรับ — ควรลดค่าใช้จ่าย"); }

        // Emergency fund (3-6 months expenses)
        if (expense > 0 && totalBalance >= expense * 6) { score += 15; health.Strengths.Add("✅ มีเงินสำรองฉุกเฉิน 6+ เดือน — ยอดเยี่ยม!"); }
        else if (expense > 0 && totalBalance >= expense * 3) { score += 8; health.Strengths.Add("✅ มีเงินสำรองฉุกเฉิน 3+ เดือน"); }
        else if (expense > 0) { score -= 5; health.Warnings.Add("⚠️ เงินสำรองไม่พอ 3 เดือน — ควรเพิ่มเงินออม"); }

        // Bills
        if (overdueBills > 0) { score -= 15; health.Warnings.Add($"🔴 มีบิลเลยกำหนด {overdueBills} รายการ — จ่ายด่วน!"); }
        else if (unpaidBills == 0 && data.Bills.Any()) { score += 10; health.Strengths.Add("✅ ไม่มีบิลค้างจ่าย"); }

        // Bank accounts
        if (data.Accounts.Count >= 2) { score += 5; health.Strengths.Add("✅ มีหลายบัญชี — กระจายความเสี่ยงดี"); }

        // Savings rate
        if (income > 0)
        {
            var savingsRate = (income - expense) / income * 100;
            if (savingsRate >= 30) health.Recommendations.Add($"💰 อัตราการออม {savingsRate:F0}% — สูงกว่าค่าเฉลี่ย แนะนำนำไปลงทุนเพิ่ม");
            else if (savingsRate >= 10) health.Recommendations.Add($"💰 อัตราการออม {savingsRate:F0}% — ดี ลองเพิ่มเป็น 30%");
            else health.Recommendations.Add($"💰 อัตราการออม {savingsRate:F0}% — ควรเพิ่มการออมอย่างน้อย 10-20%");
        }

        score = Math.Clamp(score, 0, 100);
        health.Score = score;
        health.Grade = score >= 80 ? "A" : score >= 60 ? "B" : score >= 40 ? "C" : "D";
        health.GradeColor = score >= 80 ? "#27ae60" : score >= 60 ? "#f39c12" : score >= 40 ? "#e84393" : "#e74c3c";

        return health;
    }

    // ====================================================
    // แนะนำพอร์ตการลงทุน
    // ====================================================
    public static List<InvestmentPlan> RecommendPortfolio(FinanceData data, string riskProfile)
    {
        var totalBalance = data.Accounts.Sum(a => a.Balance);
        var thisMonth = DateTime.Today.ToString("yyyy-MM");
        var monthExpense = data.Transactions.Where(t => t.Date.StartsWith(thisMonth) && t.Type == "expense").Sum(t => t.Amount);
        var emergency = monthExpense > 0 ? monthExpense * 6 : totalBalance * 0.3;

        var investable = Math.Max(totalBalance - emergency, 0);
        var plans = new List<InvestmentPlan>();

        if (riskProfile == "conservative")
        {
            plans.Add(Clone(AllPlans[0], 20));  // เงินฝาก
            plans.Add(Clone(AllPlans[1], 30));  // พันธบัตร
            plans.Add(Clone(AllPlans[2], 25));  // กองทุนตราสารหนี้
            plans.Add(Clone(AllPlans[8], 15));  // ทองคำ
            plans.Add(Clone(AllPlans[3], 10));  // กองทุนผสม
        }
        else if (riskProfile == "moderate")
        {
            plans.Add(Clone(AllPlans[0], 10));  // เงินฝาก
            plans.Add(Clone(AllPlans[2], 15));  // กองทุนตราสารหนี้
            plans.Add(Clone(AllPlans[3], 20));  // กองทุนผสม
            plans.Add(Clone(AllPlans[4], 15));  // SSF/RMF
            plans.Add(Clone(AllPlans[7], 15));  // ETF
            plans.Add(Clone(AllPlans[8], 10));  // ทองคำ
            plans.Add(Clone(AllPlans[9], 10));  // REIT
            plans.Add(Clone(AllPlans[5], 5));   // หุ้นไทย
        }
        else // aggressive
        {
            plans.Add(Clone(AllPlans[5], 25));  // หุ้นไทย
            plans.Add(Clone(AllPlans[6], 20));  // หุ้นต่างประเทศ
            plans.Add(Clone(AllPlans[7], 15));  // ETF
            plans.Add(Clone(AllPlans[4], 10));  // SSF/RMF
            plans.Add(Clone(AllPlans[10], 10)); // คริปโต
            plans.Add(Clone(AllPlans[8], 10));  // ทองคำ
            plans.Add(Clone(AllPlans[9], 10));  // REIT
        }

        return plans;
    }

    // ====================================================
    // คาดการณ์เงินเติบโต
    // ====================================================
    public static List<InvestmentProjection> ProjectGrowth(double principal, double monthlyAdd, int years)
    {
        var projections = new List<InvestmentProjection>();
        double con = principal, mod = principal, agg = principal;
        double conRate = 0.035, modRate = 0.065, aggRate = 0.10;

        for (int y = 1; y <= years; y++)
        {
            for (int m = 0; m < 12; m++)
            {
                con = con * (1 + conRate / 12) + monthlyAdd;
                mod = mod * (1 + modRate / 12) + monthlyAdd;
                agg = agg * (1 + aggRate / 12) + monthlyAdd;
            }
            projections.Add(new InvestmentProjection
            {
                Year = y, Conservative = con, Moderate = mod, Aggressive = agg
            });
        }
        return projections;
    }

    public static List<InvestmentPlan> GetAllPlans() => AllPlans;

    private static InvestmentPlan Clone(InvestmentPlan p, double allocation) => new()
    {
        Name = p.Name, Icon = p.Icon, RiskLevel = p.RiskLevel, RiskColor = p.RiskColor,
        ExpectedReturn = p.ExpectedReturn, AllocationPercent = allocation,
        Description = p.Description, Pros = p.Pros, Cons = p.Cons, Suitable = p.Suitable,
    };
}
