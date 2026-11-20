using DailyReportEmail.Interfaces;
using DailyReportEmail.Models;

namespace DailyReportEmail.Services
{
    public class DashboardDataService : DailyReportEmail.Interfaces.IDashboardDataService
    {
        private readonly IDatabaseService _databaseService;

        public DashboardDataService(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<DailyReportModel> GetDailyReportDataAsync()
        {
            var report = await _databaseService.GetDailyReportAsync();
            return report ?? new DailyReportModel();
        }
    }
}