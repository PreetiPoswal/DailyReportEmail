using DailyReportEmail.Models;

namespace DailyReportEmail.Interfaces;

public interface IMarshmallowReportService
{
    Task<MarshmallowDailySummary> GetDailySummaryAsync();
}
