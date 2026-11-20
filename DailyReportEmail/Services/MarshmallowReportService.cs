using System.Data;
using DailyReportEmail.Interfaces;
using DailyReportEmail.Models;
using Microsoft.Data.SqlClient;

namespace DailyReportEmail.Services;

public class MarshmallowReportService : IMarshmallowReportService
{
    private readonly IConfiguration _configuration;

    public MarshmallowReportService(IConfiguration configuration) => _configuration = configuration;

    public async Task<MarshmallowDailySummary> GetDailySummaryAsync()
    {
        var connectionString = _configuration.GetConnectionString("MarshmallowConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("MarshmallowConnection is not configured.");

        var summary = new MarshmallowDailySummary();
        await using var connection = new SqlConnection(connectionString);
        await using var command = new SqlCommand("dbo.sp_GetMarshmallowDailySummary", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            summary.ReportDate = reader.GetDateTime(reader.GetOrdinal("ReportDate"));
            summary.ActiveStoreCount = GetInt(reader, "ActiveStoreCount");
            summary.TotalOrders = GetInt(reader, "TotalOrders");
            summary.TotalOrderValue = GetDecimal(reader, "TotalOrderValue");
            summary.TotalDelivered = GetInt(reader, "TotalDelivered");
        }

        if (await reader.NextResultAsync())
        {
            while (await reader.ReadAsync())
            {
                summary.LifetimeStores.Add(new MarshmallowStoreTotalSummary
                {
                    StoreId = GetInt(reader, "StoreId"),
                    StoreName = GetString(reader, "StoreName").ToUpperInvariant(),
                    StoreStatus = GetString(reader, "StoreStatus"),
                    Orders = GetInt(reader, "Orders"),
                    OrderValue = GetDecimal(reader, "OrderValue"),
                    Delivered = GetInt(reader, "Delivered")
                });
            }
        }

        if (await reader.NextResultAsync())
        {
            while (await reader.ReadAsync())
            {
                summary.Stores.Add(new MarshmallowStoreDailySummary
                {
                    StoreId = GetInt(reader, "StoreId"),
                    StoreName = GetString(reader, "StoreName").ToUpperInvariant(),
                    Orders = GetInt(reader, "Orders"),
                    OrderValue = GetDecimal(reader, "OrderValue"),
                    Confirmed = GetInt(reader, "Confirmed"),
                    Preparing = GetInt(reader, "Preparing"),
                    Delivered = GetInt(reader, "Delivered"),
                    Pending = GetInt(reader, "Pending"),
                    Cancelled = GetInt(reader, "Cancelled"),
                    Other = GetInt(reader, "Other")
                });
            }
        }

        if (await reader.NextResultAsync())
        {
            while (await reader.ReadAsync())
            {
                summary.WeeklyActivity.Add(new MarshmallowDailyActivity
                {
                    Date = reader.GetDateTime(reader.GetOrdinal("ReportDate")),
                    Orders = GetInt(reader, "Orders"),
                    Delivered = GetInt(reader, "Delivered"),
                    Pending = GetInt(reader, "Pending")
                });
            }
            summary.WeeklyHistoryAvailable = true;
        }

        return summary;
    }

    private static int GetInt(SqlDataReader reader, string column) =>
        reader.IsDBNull(reader.GetOrdinal(column)) ? 0 : Convert.ToInt32(reader.GetValue(reader.GetOrdinal(column)));

    private static decimal GetDecimal(SqlDataReader reader, string column) =>
        reader.IsDBNull(reader.GetOrdinal(column)) ? 0m : Convert.ToDecimal(reader.GetValue(reader.GetOrdinal(column)));

    private static string GetString(SqlDataReader reader, string column) =>
        reader.IsDBNull(reader.GetOrdinal(column)) ? string.Empty : reader.GetString(reader.GetOrdinal(column));
}
