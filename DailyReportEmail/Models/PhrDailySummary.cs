namespace DailyReportEmail.Models;

public class PhrDailySummary
{
    public DateTime ReportDate { get; set; } = DateTime.Today;
    public long TotalPrescriptions { get; set; }
    public long TodayPrescriptions { get; set; }
    public List<PhrDepartmentCount> Departments { get; set; } = new();
    public List<PhrDepartmentCount> MonthlyDepartments { get; set; } = new();
    public List<PhrDailyActivity> WeeklyActivity { get; set; } = new();
}

public class PhrDailyActivity
{
    public DateTime Date { get; set; }
    public long PrescriptionCount { get; set; }
}

public class PhrDepartmentCount
{
    public string DepartmentName { get; set; } = string.Empty;
    public long PrescriptionCount { get; set; }
}

