using DailyReportEmail.Models;

namespace DailyReportEmail.Interfaces
{
    public interface IPdfService
    {
        byte[] GenerateDailyReport(DailyReportModel model);
    }
}