using DailyReportEmail.Models;

namespace DailyReportEmail.Interfaces;

public interface IMrdReportService
{
    Task<MrdDailySummary> GetDailySummaryAsync();
}
