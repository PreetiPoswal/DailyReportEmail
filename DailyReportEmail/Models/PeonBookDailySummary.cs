namespace DailyReportEmail.Models;

public class PeonBookDailySummary
{
    public DateTime ReportDate { get; set; }
    public int TotalCreated { get; set; }
    public int TotalDelivered { get; set; }
    public int TotalReceived { get; set; }
    public int TotalInactive { get; set; }
    public int TotalPending { get; set; }
    public int TodayCreated { get; set; }
    public int TodayDelivered { get; set; }
    public int TodayReceived { get; set; }
    public int TodayInactive { get; set; }
    public int TodayPending { get; set; }
    public List<PeonBookDailyActivity> WeeklyActivity { get; set; } = new();
    public bool WeeklyHistoryAvailable { get; set; }
}

public class PeonBookDailyActivity
{
    public DateTime Date { get; set; }
    public int Created { get; set; }
    public int Delivered { get; set; }
    public int Received { get; set; }
    public int Inactive { get; set; }
    public int Pending { get; set; }
}
