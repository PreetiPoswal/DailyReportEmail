namespace DailyReportEmail.Models;

public class MrdDailySummary
{
    public DateTime ReportDate { get; set; }
    public int TotalActiveRecords { get; set; }
    public int TotalInactiveRecords { get; set; }
    public int TotalUploadedFiles { get; set; }
    public int TotalPatientRecords { get; set; }
    public int TotalVerifiedRecords { get; set; }
    public int TotalPendingVerification { get; set; }
    public int TotalAdmissions { get; set; }
    public int TotalDischarges { get; set; }
    public int TodayActiveRecords { get; set; }
    public int TodayInactiveRecords { get; set; }
    public int TodayUploadedFiles { get; set; }
    public int TodayPatientRecords { get; set; }
    public int TodayVerifiedRecords { get; set; }
    public int TodayPendingVerification { get; set; }
    public int TodayAdmissions { get; set; }
    public int TodayDischarges { get; set; }
    public List<MrdDailyActivity> WeeklyActivity { get; set; } = new();
    public bool WeeklyHistoryAvailable { get; set; }
}

public class MrdDailyActivity
{
    public DateTime Date { get; set; }
    public int PatientRecords { get; set; }
    public int UploadedFiles { get; set; }
    public int Admissions { get; set; }
    public int Discharges { get; set; }
}
