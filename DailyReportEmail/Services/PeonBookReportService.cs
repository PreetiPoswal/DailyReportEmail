using System.Data;
using DailyReportEmail.Interfaces;
using DailyReportEmail.Models;
using Microsoft.Data.SqlClient;

namespace DailyReportEmail.Services;

public class PeonBookReportService : IPeonBookReportService
{
    private readonly IConfiguration _configuration;

    public PeonBookReportService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<PeonBookDailySummary> GetDailySummaryAsync()
    {
        var summary = new PeonBookDailySummary { ReportDate = DateTime.Today };
        var connectionString = _configuration.GetConnectionString("PeonBookConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("PeonBookConnection is not configured.");

        await using var connection = new SqlConnection(connectionString);
        await using var command = new SqlCommand("dbo.sp_GetpeonbookDailySummary", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        await connection.OpenAsync();
        {
            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                summary.TotalCreated = GetInt(reader, "TotalCreated");
                summary.TotalDelivered = GetInt(reader, "TotalDelivered");
                summary.TotalReceived = GetInt(reader, "TotalReceived");
                summary.TotalInactive = GetInt(reader, "TotalInactive");
                summary.TotalPending = GetInt(reader, "TotalPending");
            }

            if (await reader.NextResultAsync() && await reader.ReadAsync())
            {
                summary.ReportDate = GetDateTime(reader, "ReportDate", DateTime.Today);
                summary.TodayCreated = GetInt(reader, "TodayCreated");
                summary.TodayDelivered = GetInt(reader, "TodayDelivered");
                summary.TodayReceived = GetInt(reader, "TodayReceived");
                summary.TodayInactive = GetInt(reader, "TodayInactive");
                summary.TodayPending = GetInt(reader, "TodayPending");
            }
        }

        // Supplemental history query for charts only. The stored procedure above
        // remains the sole source for every existing Peon Book report number.
        try
        {
            var reportDay = summary.ReportDate.Date;
            for (var daysAgo = 6; daysAgo >= 0; daysAgo--)
                summary.WeeklyActivity.Add(new PeonBookDailyActivity { Date = reportDay.AddDays(-daysAgo) });

            var dailyRows = summary.WeeklyActivity.ToDictionary(item => item.Date.Date);
            const string weeklyQuery = @"
                SELECT CAST(DateCreated AS DATE) AS ReportDate,
                       COUNT(*) AS Created,
                       SUM(CASE WHEN LOWER(LTRIM(RTRIM(ISNULL([Status], '')))) = 'delivered' THEN 1 ELSE 0 END) AS Delivered,
                       SUM(CASE WHEN LOWER(LTRIM(RTRIM(ISNULL([Status], '')))) = 'received' THEN 1 ELSE 0 END) AS Received,
                       SUM(CASE WHEN LOWER(LTRIM(RTRIM(ISNULL([Status], '')))) = 'inactive' THEN 1 ELSE 0 END) AS Inactive,
                       SUM(CASE WHEN LOWER(LTRIM(RTRIM(ISNULL([Status], '')))) = 'created' THEN 1 ELSE 0 END) AS Pending
                FROM dbo.Entries
                WHERE DateCreated >= @StartDate AND DateCreated < @EndDate
                GROUP BY CAST(DateCreated AS DATE)
                ORDER BY ReportDate;";

            await using var historyCommand = new SqlCommand(weeklyQuery, connection);
            historyCommand.Parameters.Add("@StartDate", SqlDbType.Date).Value = reportDay.AddDays(-6);
            historyCommand.Parameters.Add("@EndDate", SqlDbType.Date).Value = reportDay.AddDays(1);
            await using var historyReader = await historyCommand.ExecuteReaderAsync();
            while (await historyReader.ReadAsync())
            {
                var date = historyReader.GetDateTime(historyReader.GetOrdinal("ReportDate")).Date;
                if (!dailyRows.TryGetValue(date, out var activity))
                    continue;

                activity.Created = GetInt(historyReader, "Created");
                activity.Delivered = GetInt(historyReader, "Delivered");
                activity.Received = GetInt(historyReader, "Received");
                activity.Inactive = GetInt(historyReader, "Inactive");
                activity.Pending = GetInt(historyReader, "Pending");
            }

            // Keep today's chart exactly aligned with the existing stored procedure output.
            var today = summary.WeeklyActivity.LastOrDefault(item => item.Date.Date == reportDay);
            if (today is not null)
            {
                today.Created = summary.TodayCreated;
                today.Delivered = summary.TodayDelivered;
                today.Received = summary.TodayReceived;
                today.Inactive = summary.TodayInactive;
                today.Pending = summary.TodayPending;
            }
            summary.WeeklyHistoryAvailable = true;
        }
        catch (Exception ex)
        {
            summary.WeeklyActivity.Clear();
            Console.WriteLine($"PEON Book weekly chart data unavailable: {ex.Message}");
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
