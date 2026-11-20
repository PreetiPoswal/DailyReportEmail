using DailyReportEmail.Models;
using System.Threading.Tasks;

namespace DailyReportEmail.Interfaces
{
    public interface IDashboardDataService
    {
        Task<DailyReportModel> GetDailyReportDataAsync();
    }
}