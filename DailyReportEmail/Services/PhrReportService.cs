using System.Data;
using DailyReportEmail.Interfaces;
using DailyReportEmail.Models;
using Microsoft.Data.SqlClient;

namespace DailyReportEmail.Services;

public class PhrReportService : IPhrReportService
{
    private readonly IConfiguration _configuration;

    public PhrReportService(IConfiguration configuration) => _configuration = configuration;

    public async Task<PhrDailySummary> GetDailySummaryAsync(DateTime reportDate)
    {
        var connectionString = _configuration.GetConnectionString("PHRConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("PHRConnection is not configured.");

        var summary = new PhrDailySummary { ReportDate = reportDate.Date };
        await using var connection = new SqlConnection(connectionString);
        await using var command = new SqlCommand("dbo.sp_GetPHRDailyPrescriptions", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.Add("@ReportDate", SqlDbType.Date).Value = reportDate.Date;

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            summary.ReportDate = reader.GetDateTime(reader.GetOrdinal("ReportDate"));
            summary.TotalPrescriptions = GetLong(reader, "TotalPrescriptions");
            summary.TodayPrescriptions = GetLong(reader, "TodayPrescriptions");
        }

        if (await reader.NextResultAsync())
            while (await reader.ReadAsync())
                summary.Departments.Add(new PhrDepartmentCount
                {
                    DepartmentName = GetString(reader, "DepartmentName"),
                    PrescriptionCount = GetLong(reader, "PrescriptionCount")
                });

        if (await reader.NextResultAsync())
            while (await reader.ReadAsync())
                summary.MonthlyDepartments.Add(new PhrDepartmentCount
                {
                    DepartmentName = GetString(reader, "DepartmentName"),
                    PrescriptionCount = GetLong(reader, "PrescriptionCount")
                });

        if (await reader.NextResultAsync())
            while (await reader.ReadAsync())
                summary.WeeklyActivity.Add(new PhrDailyActivity
                {
                    Date = reader.GetDateTime(reader.GetOrdinal("ActivityDate")),
                    PrescriptionCount = GetLong(reader, "PrescriptionCount")
                });

        return summary;
    }

    private static long GetLong(SqlDataReader reader, string column) =>
        reader.IsDBNull(reader.GetOrdinal(column)) ? 0 : Convert.ToInt64(reader.GetValue(reader.GetOrdinal(column)));

    private static string GetString(SqlDataReader reader, string column) =>
        reader.IsDBNull(reader.GetOrdinal(column)) ? string.Empty : Convert.ToString(reader.GetValue(reader.GetOrdinal(column))) ?? string.Empty;
}
