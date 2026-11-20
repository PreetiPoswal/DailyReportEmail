using DailyReportEmail.Models;

namespace DailyReportEmail.Interfaces;

public interface IPeonBookReportService
{
    Task<PeonBookDailySummary> GetDailySummaryAsync();
}
