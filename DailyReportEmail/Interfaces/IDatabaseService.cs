using DailyReportEmail.Models;
using System.Threading.Tasks;


namespace DailyReportEmail.Interfaces
{
    public interface IDatabaseService
    {
        Task<DailyReportModel> GetDailyReportAsync();
    }
}