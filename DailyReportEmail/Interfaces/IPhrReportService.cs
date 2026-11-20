using DailyReportEmail.Models;

namespace DailyReportEmail.Interfaces;

public interface IPhrReportService
{
    Task<PhrDailySummary> GetDailySummaryAsync(DateTime reportDate);
}
