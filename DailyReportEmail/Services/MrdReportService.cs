using System.Data;
using DailyReportEmail.Interfaces;
using DailyReportEmail.Models;
using Microsoft.Data.SqlClient;

namespace DailyReportEmail.Services;

public class MrdReportService : IMrdReportService
{
    private readonly IConfiguration _configuration;

    public MrdReportService(IConfiguration configuration) => _configuration = configuration;

    public async Task<MrdDailySummary> GetDailySummaryAsync()
    {
        var summary = new MrdDailySummary { ReportDate = DateTime.Today };
        var connectionString = _configuration.GetConnectionString("MrdConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("MrdConnection is not configured.");

        await using var connection = new SqlConnection(connectionString);
        await using var command = new SqlCommand("dbo.sp_GetMRDDailySummary", connection) { CommandType = CommandType.StoredProcedure };
        await connection.OpenAsync();
        {
            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                summary.TotalActiveRecords = GetInt(reader, "TotalActiveRecords");
                summary.TotalInactiveRecords = GetInt(reader, "TotalInactiveRecords");
                summary.TotalUploadedFiles = GetInt(reader, "TotalUploadedFiles");
                summary.TotalPatientRecords = GetInt(reader, "TotalPatientRecords");
                summary.TotalVerifiedRecords = GetInt(reader, "TotalVerifiedRecords");
                summary.TotalPendingVerification = GetInt(reader, "TotalPendingVerification");
                summary.TotalAdmissions = GetInt(reader, "TotalAdmissions");
                summary.TotalDischarges = GetInt(reader, "TotalDischarges");
            }

            if (await reader.NextResultAsync() && await reader.ReadAsync())
            {
                summary.ReportDate = GetDateTime(reader, "ReportDate", DateTime.Today);
                summary.TodayActiveRecords = GetInt(reader, "TodayActiveRecords");
                summary.TodayInactiveRecords = GetInt(reader, "TodayInactiveRecords");
                summary.TodayUploadedFiles = GetInt(reader, "TodayUploadedFiles");
                summary.TodayPatientRecords = GetInt(reader, "TodayPatientRecords");
                summary.TodayVerifiedRecords = GetInt(reader, "TodayVerifiedRecords");
                summary.TodayPendingVerification = GetInt(reader, "TodayPendingVerification");
                summary.TodayAdmissions = GetInt(reader, "TodayAdmissions");
                summary.TodayDischarges = GetInt(reader, "TodayDischarges");
            }
        }

        // Supplemental, read-only data for the charts. Existing MRD metrics
        // continue to come only from dbo.sp_GetMRDDailySummary above.
        try
        {
            var reportDay = summary.ReportDate.Date;
            for (var daysAgo = 6; daysAgo >= 0; daysAgo--)
                summary.WeeklyActivity.Add(new MrdDailyActivity { Date = reportDay.AddDays(-daysAgo) });

            var dailyRows = summary.WeeklyActivity.ToDictionary(item => item.Date.Date);
            const string weeklyQuery = @"
                WITH DailyPatients AS
                (
                    SELECT CAST(DateCreated AS DATE) AS ReportDate, COUNT(*) AS PatientRecords
                    FROM dbo.Entries
                    WHERE DateCreated >= @StartDate AND DateCreated < @EndDate
                    GROUP BY CAST(DateCreated AS DATE)
                ),
                DailyUploads AS
                (
                    SELECT CAST(UploadDate AS DATE) AS ReportDate, COUNT(*) AS UploadedFiles
                    FROM dbo.Files
                    WHERE UploadDate >= @StartDate AND UploadDate < @EndDate
                    GROUP BY CAST(UploadDate AS DATE)
                ),
                DailyAdmissions AS
                (
                    SELECT CAST(AdmissionDate AS DATE) AS ReportDate, COUNT(*) AS Admissions
                    FROM dbo.Entries
                    WHERE AdmissionDate >= @StartDate AND AdmissionDate < @EndDate
                    GROUP BY CAST(AdmissionDate AS DATE)
                ),
                DailyDischarges AS
                (
                    SELECT CAST(DischargeDate AS DATE) AS ReportDate, COUNT(*) AS Discharges
                    FROM dbo.Entries
                    WHERE DischargeDate >= @StartDate AND DischargeDate < @EndDate
                    GROUP BY CAST(DischargeDate AS DATE)
                ),
                ActivityDates AS
                (
                    SELECT ReportDate FROM DailyPatients
                    UNION SELECT ReportDate FROM DailyUploads
                    UNION SELECT ReportDate FROM DailyAdmissions
                    UNION SELECT ReportDate FROM DailyDischarges
                )
                SELECT AD.ReportDate,
                       ISNULL(P.PatientRecords, 0) AS PatientRecords,
                       ISNULL(U.UploadedFiles, 0) AS UploadedFiles,
                       ISNULL(A.Admissions, 0) AS Admissions,
                       ISNULL(D.DischargeCount, 0) AS Discharges
                FROM ActivityDates AD
                LEFT JOIN DailyPatients P ON P.ReportDate = AD.ReportDate
                LEFT JOIN DailyUploads U ON U.ReportDate = AD.ReportDate
                LEFT JOIN DailyAdmissions A ON A.ReportDate = AD.ReportDate
                LEFT JOIN (SELECT ReportDate, Discharges AS DischargeCount FROM DailyDischarges) D ON D.ReportDate = AD.ReportDate
                ORDER BY AD.ReportDate;";

            await using var historyCommand = new SqlCommand(weeklyQuery, connection);
            historyCommand.Parameters.Add("@StartDate", SqlDbType.Date).Value = reportDay.AddDays(-6);
            historyCommand.Parameters.Add("@EndDate", SqlDbType.Date).Value = reportDay.AddDays(1);
            await using var historyReader = await historyCommand.ExecuteReaderAsync();
            while (await historyReader.ReadAsync())
            {
                var date = historyReader.GetDateTime(historyReader.GetOrdinal("ReportDate")).Date;
                if (!dailyRows.TryGetValue(date, out var activity))
                    continue;
                activity.PatientRecords = GetInt(historyReader, "PatientRecords");
                activity.UploadedFiles = GetInt(historyReader, "UploadedFiles");
                activity.Admissions = GetInt(historyReader, "Admissions");
                activity.Discharges = GetInt(historyReader, "Discharges");
            }

            // Align today's plotted values with the existing procedure output.
            var today = summary.WeeklyActivity.LastOrDefault(item => item.Date.Date == reportDay);
            if (today is not null)
            {
                today.PatientRecords = summary.TodayPatientRecords;
                today.UploadedFiles = summary.TodayUploadedFiles;
                today.Admissions = summary.TodayAdmissions;
                today.Discharges = summary.TodayDischarges;
            }
            summary.WeeklyHistoryAvailable = true;
        }
        catch (Exception ex)
        {
            summary.WeeklyActivity.Clear();
            Console.WriteLine($"MRD weekly chart data unavailable: {ex.Message}");
        }

        return summary;
    }

    private static int GetInt(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
    }

    private static DateTime GetDateTime(SqlDataReader reader, string column, DateTime fallback)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? fallback : reader.GetDateTime(ordinal);
    }
}
