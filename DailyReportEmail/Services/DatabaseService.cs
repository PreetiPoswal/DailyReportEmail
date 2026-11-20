using DailyReportEmail.Interfaces;
using DailyReportEmail.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DailyReportEmail.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly IConfiguration _configuration;
        private readonly IPeonBookReportService _peonBookReportService;
        private readonly IMrdReportService _mrdReportService;
        private readonly IMarshmallowReportService _marshmallowReportService;
        private readonly IPhrReportService _phrReportService;

        public DatabaseService(IConfiguration configuration, IPeonBookReportService peonBookReportService, IMrdReportService mrdReportService, IMarshmallowReportService marshmallowReportService, IPhrReportService phrReportService)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _peonBookReportService = peonBookReportService;
            _mrdReportService = mrdReportService;
            _marshmallowReportService = marshmallowReportService;
            _phrReportService = phrReportService;
        }

        public async Task<DailyReportModel> GetDailyReportAsync()
        {
            var model = new DailyReportModel
            {
                ReportDate = DateTime.Now
            };
            var hmpkReportDay = model.ReportDate.Date;

            // 1. Central Store (HMPK) data
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                try
                {
                    await using var connection = new SqlConnection(connectionString);
                    await using var command = new SqlCommand("SP_DailyBackupReport", connection) { CommandType = CommandType.StoredProcedure };
                    await connection.OpenAsync();
                    await using var reader = await command.ExecuteReaderAsync();

                    // ===== RESULT SET 1: Overall Summary =====
                    if (await reader.ReadAsync())
                    {
                        if (!reader.IsDBNull(reader.GetOrdinal("ReportDate")))
                            hmpkReportDay = reader.GetDateTime(reader.GetOrdinal("ReportDate")).Date;
                        model.TodayDemand        = SafeInt(reader, "TodayDemand");
                        model.PendingDemand      = SafeInt(reader, "PendingDemand");
                        model.ApprovedDemand     = SafeInt(reader, "ApprovedDemand");
                        model.RejectedDemand     = SafeInt(reader, "RejectedDemand");
                        model.TodayPO            = SafeInt(reader, "TodayPO");
                        model.PendingPO          = SafeInt(reader, "PendingPO");
                        model.ApprovedPO         = SafeInt(reader, "ApprovedPO");
                        model.RejectedPO         = SafeInt(reader, "RejectedPO");
                        model.LockedPOAmount     = SafeDecimal(reader, "LockedPOAmount");
                        model.TotalPOAmount      = SafeDecimal(reader, "TotalPOAmount");
                        model.TodayDMR           = SafeInt(reader, "TodayDMR");
                        model.PendingDMR         = SafeInt(reader, "PendingDMR");
                        model.ApprovedDMR        = SafeInt(reader, "ApprovedDMR");
                        model.RejectedDMR        = SafeInt(reader, "RejectedDMR");
                        model.TotalDMRAmount     = SafeDecimal(reader, "TotalDMRAmount");
                        model.TodayBillPayment   = SafeInt(reader, "TodayBillPayment");
                        model.PendingBillPayment = SafeInt(reader, "PendingBillPayment");
                        model.ApprovedBillPayment= SafeInt(reader, "ApprovedBillPayment");
                        model.RejectedBillPayment= SafeInt(reader, "RejectedBillPayment");
                        model.TotalBillAmount    = SafeDecimal(reader, "TotalBillAmount");
                        model.NewRegisteredUsers = SafeInt(reader, "NewRegisteredUsers");
                        model.ActiveUsers        = SafeInt(reader, "ActiveUsers");
                        model.InactiveUsers      = SafeInt(reader, "InactiveUsers");
                    }

                    // ===== RESULT SET 2: Department-Wise Summary =====
                    if (await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            model.DepartmentSummaries.Add(new DepartmentWiseSummary
                            {
                                DepartmentID        = SafeInt(reader, "DepartmentID"),
                                DepartmentName      = SafeString(reader, "DepartmentName"),
                                TodayDemand         = SafeInt(reader, "TodayDemand"),
                                PendingDemand       = SafeInt(reader, "PendingDemand"),
                                ApprovedDemand      = SafeInt(reader, "ApprovedDemand"),
                                RejectedDemand      = SafeInt(reader, "RejectedDemand"),
                                TodayPO             = SafeInt(reader, "TodayPO"),
                                PendingPO           = SafeInt(reader, "PendingPO"),
                                ApprovedPO          = SafeInt(reader, "ApprovedPO"),
                                RejectedPO          = SafeInt(reader, "RejectedPO"),
                                POAmount            = SafeDecimal(reader, "POAmount"),
                                TodayDMR            = SafeInt(reader, "TodayDMR"),
                                PendingDMR          = SafeInt(reader, "PendingDMR"),
                                ApprovedDMR         = SafeInt(reader, "ApprovedDMR"),
                                RejectedDMR         = SafeInt(reader, "RejectedDMR"),
                                DMRAmount           = SafeDecimal(reader, "DMRAmount"),
                                TodayBillPayment    = SafeInt(reader, "TodayBillPayment"),
                                PendingBillPayment  = SafeInt(reader, "PendingBillPayment"),
                                ApprovedBillPayment = SafeInt(reader, "ApprovedBillPayment"),
                                RejectedBillPayment = SafeInt(reader, "RejectedBillPayment"),
                                BillAmount          = SafeDecimal(reader, "BillAmount")
                            });
                        }
                    }

                    await reader.DisposeAsync();

                    // Historical chart values only: existing SP output remains the source
                    // of every HMPK value shown in the current report.
                    try
                    {
                        var reportDay = hmpkReportDay;
                        model.HmpkWeeklyActivity.Clear();
                        for (var daysAgo = 6; daysAgo >= 0; daysAgo--)
                            model.HmpkWeeklyActivity.Add(new HmpkDailyActivity { Date = reportDay.AddDays(-daysAgo) });

                        var dailyRows = model.HmpkWeeklyActivity.ToDictionary(item => item.Date.Date);
                        const string weeklyQuery = @"
                            WITH DailyDemand AS
                            (
                                SELECT CAST(Dateofgen AS DATE) AS ReportDate, COUNT(*) AS DemandCount
                                FROM Demands
                                WHERE CAST(Dateofgen AS DATE) >= @StartDate AND CAST(Dateofgen AS DATE) < @EndDate
                                GROUP BY CAST(Dateofgen AS DATE)
                            ),
                            DailyPurchaseOrder AS
                            (
                                SELECT CAST(Po_Gen_Date AS DATE) AS ReportDate, COUNT(*) AS PurchaseOrderCount
                                FROM PurchaseOrders
                                WHERE CAST(Po_Gen_Date AS DATE) >= @StartDate AND CAST(Po_Gen_Date AS DATE) < @EndDate
                                GROUP BY CAST(Po_Gen_Date AS DATE)
                            ),
                            DailyDmr AS
                            (
                                SELECT CAST(DMR_Gen_Date AS DATE) AS ReportDate, COUNT(*) AS DmrCount
                                FROM DMR_Form
                                WHERE CAST(DMR_Gen_Date AS DATE) >= @StartDate AND CAST(DMR_Gen_Date AS DATE) < @EndDate
                                GROUP BY CAST(DMR_Gen_Date AS DATE)
                            ),
                            BillPaymentEvents AS
                            (
                                SELECT CASE WHEN ISDATE(PaymentDate) = 1 THEN CONVERT(DATE, PaymentDate) END AS ReportDate
                                FROM
                                (
                                    SELECT ForwardForPaymentDate AS PaymentDate FROM PurchaseOrders
                                    UNION ALL
                                    SELECT ForwardForPaymentDate AS PaymentDate FROM DMR_Form
                                ) AS PaymentRows
                                WHERE ISDATE(PaymentDate) = 1
                                  AND CONVERT(DATE, PaymentDate) >= @StartDate
                                  AND CONVERT(DATE, PaymentDate) < @EndDate
                            ),
                            DailyBillPayment AS
                            (
                                SELECT ReportDate, COUNT(*) AS BillPaymentCount
                                FROM BillPaymentEvents
                                GROUP BY ReportDate
                            ),
                            ActivityDates AS
                            (
                                SELECT ReportDate FROM DailyDemand
                                UNION SELECT ReportDate FROM DailyPurchaseOrder
                                UNION SELECT ReportDate FROM DailyDmr
                                UNION SELECT ReportDate FROM DailyBillPayment
                            )
                            SELECT AD.ReportDate,
                                   ISNULL(DD.DemandCount, 0) AS DemandCount,
                                   ISNULL(PO.PurchaseOrderCount, 0) AS PurchaseOrderCount,
                                   ISNULL(DM.DmrCount, 0) AS DmrCount,
                                   ISNULL(BP.BillPaymentCount, 0) AS BillPaymentCount
                            FROM ActivityDates AD
                            LEFT JOIN DailyDemand DD ON DD.ReportDate = AD.ReportDate
                            LEFT JOIN DailyPurchaseOrder PO ON PO.ReportDate = AD.ReportDate
                            LEFT JOIN DailyDmr DM ON DM.ReportDate = AD.ReportDate
                            LEFT JOIN DailyBillPayment BP ON BP.ReportDate = AD.ReportDate
                            ORDER BY AD.ReportDate;";

                        await using var historyCommand = new SqlCommand(weeklyQuery, connection);
                        historyCommand.Parameters.Add("@StartDate", SqlDbType.Date).Value = reportDay.AddDays(-6);
                        historyCommand.Parameters.Add("@EndDate", SqlDbType.Date).Value = reportDay;
                        await using var historyReader = await historyCommand.ExecuteReaderAsync();
                        while (await historyReader.ReadAsync())
                        {
                            var date = historyReader.GetDateTime(historyReader.GetOrdinal("ReportDate")).Date;
                            if (dailyRows.TryGetValue(date, out var activity))
                            {
                                activity.Demands = SafeInt(historyReader, "DemandCount");
                                activity.PurchaseOrders = SafeInt(historyReader, "PurchaseOrderCount");
                                activity.Dmr = SafeInt(historyReader, "DmrCount");
                                activity.BillPayments = SafeInt(historyReader, "BillPaymentCount");
                            }
                        }

                        // Today's chart values match the current summary result exactly.
                        if (dailyRows.TryGetValue(reportDay, out var todayActivity))
                        {
                            todayActivity.Demands = model.TodayDemand;
                            todayActivity.PurchaseOrders = model.TodayPO;
                            todayActivity.Dmr = model.TodayDMR;
                            todayActivity.BillPayments = model.TodayBillPayment;
                        }
                        model.HmpkWeeklyHistoryAvailable = true;
                    }
                    catch (Exception ex)
                    {
                        model.HmpkWeeklyActivity.Clear();
                        Console.WriteLine($"HMPK weekly chart data unavailable: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    // Central DB error should NOT stop ToothTech data from loading
                    Console.WriteLine($"Central DB Error: {ex.Message}");
                }
            }

            // 2. ToothTech Production Live Integration Pipeline
            var toothTechConnectionString = _configuration.GetConnectionString("ToothTechConnection");
            if (string.IsNullOrWhiteSpace(toothTechConnectionString)) return model;

            try
            {
                await using var connection = new SqlConnection(toothTechConnectionString);
                await connection.OpenAsync();

                // --- FLAG 1: GET_PAYMENT_SUMMARY ---
                await using (var cmd = new SqlCommand("sp_GetToothTechDailySummary", connection) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("@Flag", "GET_PAYMENT_SUMMARY");
                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var dept = new DepartmentMetric
                        {
                            SNo = reader.IsDBNull(reader.GetOrdinal("SNo")) ? 0 : Convert.ToInt64(reader["SNo"]),
                            DepartmentName = reader.IsDBNull(reader.GetOrdinal("DepartmentName")) ? string.Empty : reader["DepartmentName"].ToString(),
                            CollectionCash = reader.IsDBNull(reader.GetOrdinal("CollectionCash")) ? 0m : Convert.ToDecimal(reader["CollectionCash"]),
                            CollectionOnline = reader.IsDBNull(reader.GetOrdinal("CollectionOnline")) ? 0m : Convert.ToDecimal(reader["CollectionOnline"]),
                            PatientNew = reader.IsDBNull(reader.GetOrdinal("PatientNew")) ? 0 : Convert.ToInt32(reader["PatientNew"]),
                            PatientReVisit = reader.IsDBNull(reader.GetOrdinal("PatientReVisit")) ? 0 : Convert.ToInt32(reader["PatientReVisit"]),
                            RevenueCreated = reader.IsDBNull(reader.GetOrdinal("RevenueCreated")) ? 0m : Convert.ToDecimal(reader["RevenueCreated"]),
                            RevenueCollected = reader.IsDBNull(reader.GetOrdinal("RevenueCollected")) ? 0m : Convert.ToDecimal(reader["RevenueCollected"]),
                            PrescribedPerformed = reader.IsDBNull(reader.GetOrdinal("PrescribedPerformed")) ? "-" : reader["PrescribedPerformed"].ToString()!
                        };

                        // Mapping to Global Summary Cards properties
                        model.ToothTech_TotalCashPaid += dept.CollectionCash;
                        model.ToothTech_TotalOnlinePaid += dept.CollectionOnline;
                        model.ToothTech_NewPatients += dept.PatientNew;
                        model.ToothTech_OldPatients += dept.PatientReVisit;
                        model.TodaysRevenue += dept.RevenueCreated;
                        model.TodaysCollection += dept.RevenueCollected;

                        model.Departments.Add(dept);
                    }
                    model.ToothTech_TotalFootfall = model.ToothTech_NewPatients + model.ToothTech_OldPatients;
                    model.CashCollection = model.ToothTech_TotalCashPaid;
                    model.OnlineCollection = model.ToothTech_TotalOnlinePaid;
                }

                // Discount breakdown from the same live transaction function used
                // by the existing revenue summary. This adds context only; it does
                // not replace or change the stored-procedure revenue totals.
                try
                {
                    var discountByDepartment = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                    await using var discountCommand = new SqlCommand(@"
                        SELECT D.Department_name AS DepartmentName,
                               SUM(ISNULL(Pt.Discountrs, 0.00)) AS DiscountAmount
                        FROM dbo.Udf_PTransaction('0') Pt
                        INNER JOIN dbo.Tbl_Department_Master D ON D.DeptId = Pt.DeptId
                        WHERE Pt.PaymrntType <> 'DISCOUNT'
                          AND Pt.EDate BETWEEN @StartDate AND @EndDate
                          AND Pt.DeptId NOT IN (0, 10)
                          AND D.Department_name NOT IN (N'Account', N'Dental Reception', N'All Department')
                        GROUP BY D.Department_name;", connection);
                    discountCommand.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = model.ReportDate.Date;
                    discountCommand.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = model.ReportDate;
                    await using var discountReader = await discountCommand.ExecuteReaderAsync();
                    while (await discountReader.ReadAsync())
                    {
                        var departmentName = discountReader.IsDBNull(discountReader.GetOrdinal("DepartmentName"))
                            ? string.Empty
                            : discountReader["DepartmentName"].ToString()?.Trim() ?? string.Empty;
                        if (departmentName.Length == 0)
                            continue;

                        var discount = discountReader.IsDBNull(discountReader.GetOrdinal("DiscountAmount"))
                            ? 0m
                            : Convert.ToDecimal(discountReader["DiscountAmount"]);
                        discountByDepartment[departmentName] = discount;
                    }

                    model.ToothTechDiscountAvailable = true;
                    foreach (var department in model.Departments)
                    {
                        if (department.DepartmentName is not null && discountByDepartment.TryGetValue(department.DepartmentName.Trim(), out var discount))
                        {
                            department.RevenueDiscount = discount;
                            model.TodaysDiscount += discount;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Smile Sync discount details unavailable: {ex.Message}");
                }

                // Daily history used only by the new Smile Sync charts. Today's values
                // remain sourced from the existing GET_PAYMENT_SUMMARY result above.
                try
                {
                    model.SmileSyncWeeklyCollections.Clear();
                    var reportDay = model.ReportDate.Date;
                    for (var daysAgo = 6; daysAgo >= 0; daysAgo--)
                        model.SmileSyncWeeklyCollections.Add(new SmileSyncDailyCollection { Date = reportDay.AddDays(-daysAgo) });

                    var dailyHistory = model.SmileSyncWeeklyCollections.ToDictionary(item => item.Date.Date);
                    await using (var cmd = new SqlCommand(@"
                    SELECT CAST(Pt.EDate AS DATE) AS ReportDate,
                           SUM(CASE WHEN Pt.PaymrntType NOT IN ('GATEWAY', 'ONLINE') THEN ISNULL(Pt.PaidAmount, 0.00) ELSE 0.00 END) AS CashPaid,
                           SUM(CASE WHEN Pt.PaymrntType IN ('GATEWAY', 'ONLINE') THEN ISNULL(Pt.PaidAmount, 0.00) ELSE 0.00 END) AS OnlinePaid
                    FROM Udf_PTransaction('0') Pt
                    INNER JOIN Tbl_Department_Master D ON D.DeptId = Pt.DeptId
                    WHERE Pt.PaymrntType <> 'DISCOUNT'
                      AND Pt.DeptId NOT IN (0, 10)
                      AND D.Department_name NOT IN (N'Account', N'Dental Reception', N'All Department')
                      AND Pt.EDate >= @StartDate AND Pt.EDate < @EndDate
                    GROUP BY CAST(Pt.EDate AS DATE)
                    ORDER BY ReportDate;", connection))
                    {
                        cmd.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = reportDay.AddDays(-6);
                        cmd.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = reportDay;
                        await using var reader = await cmd.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            var date = reader.GetDateTime(reader.GetOrdinal("ReportDate")).Date;
                            if (dailyHistory.TryGetValue(date, out var daily))
                            {
                                daily.Cash = reader.IsDBNull(reader.GetOrdinal("CashPaid")) ? 0m : Convert.ToDecimal(reader["CashPaid"]);
                                daily.Online = reader.IsDBNull(reader.GetOrdinal("OnlinePaid")) ? 0m : Convert.ToDecimal(reader["OnlinePaid"]);
                            }
                        }
                    }

                    // Use the same current-day totals that already feed the Smile Sync cards.
                    if (dailyHistory.TryGetValue(reportDay, out var todayCollection))
                    {
                        todayCollection.Cash = model.ToothTech_TotalCashPaid;
                        todayCollection.Online = model.ToothTech_TotalOnlinePaid;
                    }
                    model.SmileSyncWeeklyHistoryAvailable = true;
                }
                catch (Exception ex)
                {
                    model.SmileSyncWeeklyCollections.Clear();
                    Console.WriteLine($"Smile Sync weekly collection chart data unavailable: {ex.Message}");
                }

                // --- FLAG 2: GET_DAILY_PAYMENT_SUMMARY (3 Result Sets) ---
                await using (var cmd = new SqlCommand("sp_GetToothTechDailySummary", connection) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("@Flag", "GET_DAILY_PAYMENT_SUMMARY");
                    await using var reader = await cmd.ExecuteReaderAsync();

                    // RS 1: Department Payment Summary
                    while (await reader.ReadAsync())
                    {
                        model.Flag2PaymentSummaries.Add(new Flag2DepartmentPaymentSummary
                        {
                            Department = reader.IsDBNull(reader.GetOrdinal("Department")) ? string.Empty : reader["Department"].ToString()!,
                            HodName = reader.IsDBNull(reader.GetOrdinal("HOD Name")) ? string.Empty : reader["HOD Name"].ToString()!,
                            CashPaid = reader.IsDBNull(reader.GetOrdinal("Cash Paid")) ? 0m : Convert.ToDecimal(reader["Cash Paid"]),
                            OnlinePaid = reader.IsDBNull(reader.GetOrdinal("Online Paid")) ? 0m : Convert.ToDecimal(reader["Online Paid"]),
                            TotalPaid = reader.IsDBNull(reader.GetOrdinal("Total Paid")) ? 0m : Convert.ToDecimal(reader["Total Paid"])
                        });
                    }

                    // RS 2: General Patient Discount Alert
                    if (await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            model.DiscountAlerts.Add(new DiscountAlert
                            {
                                UHID = reader.IsDBNull(reader.GetOrdinal("UHID")) ? string.Empty : reader["UHID"].ToString(),
                                PatientName = reader.IsDBNull(reader.GetOrdinal("Patient Name")) ? string.Empty : reader["Patient Name"].ToString(),
                                DepartmentName = reader.IsDBNull(reader.GetOrdinal("Department Name")) ? string.Empty : reader["Department Name"].ToString(),
                                Amount = reader.IsDBNull(reader.GetOrdinal("Bill Amount")) ? 0m : Convert.ToDecimal(reader["Bill Amount"]),
                                Discount = reader.IsDBNull(reader.GetOrdinal("Discount Amount")) ? 0m : Convert.ToDecimal(reader["Discount Amount"]),
                                PaidAmount = reader.IsDBNull(reader.GetOrdinal("Paid Amount")) ? 0m : Convert.ToDecimal(reader["Paid Amount"]),
                                ApprovedBy = reader.IsDBNull(reader.GetOrdinal("Approved By")) ? string.Empty : reader["Approved By"].ToString()
                            });
                        }
                    }

                    // RS 3: Staff / Student Collection Alert
                    if (await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            model.StaffStudentAlerts.Add(new StaffStudentAlert
                            {
                                UHID = reader.IsDBNull(reader.GetOrdinal("UHID")) ? string.Empty : reader["UHID"].ToString(),
                                PatientName = reader.IsDBNull(reader.GetOrdinal("Patient Name")) ? string.Empty : reader["Patient Name"].ToString(),
                                PatientType = reader.IsDBNull(reader.GetOrdinal("Patient Type")) ? string.Empty : reader["Patient Type"].ToString(),
                                PaidAmount = reader.IsDBNull(reader.GetOrdinal("Paid Amount")) ? 0m : Convert.ToDecimal(reader["Paid Amount"]),
                                DepartmentName = reader.IsDBNull(reader.GetOrdinal("Department Name")) ? string.Empty : reader["Department Name"].ToString(),
                                Amount = reader.IsDBNull(reader.GetOrdinal("Paid Amount")) ? 0m : Convert.ToDecimal(reader["Paid Amount"]) // Sync fallback
                            });
                        }
                    }
                }

                // --- FLAG 3: GET_PATIENT_LIST ---
                await using (var cmd = new SqlCommand("sp_GetToothTechDailySummary", connection) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("@Flag", "GET_PATIENT_LIST");
                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        model.TodayPatients.Add(new TodayPatient
                        {
                            UHID = reader.IsDBNull(reader.GetOrdinal("UHID")) ? string.Empty : reader["UHID"].ToString(),
                            Name = reader.IsDBNull(reader.GetOrdinal("Patient Name")) ? string.Empty : reader["Patient Name"].ToString(),
                            Gender = reader.IsDBNull(reader.GetOrdinal("Gender")) ? string.Empty : reader["Gender"].ToString(),
                            Mobile = reader.IsDBNull(reader.GetOrdinal("Mobile")) ? string.Empty : reader["Mobile"].ToString(),
                            Department = reader.IsDBNull(reader.GetOrdinal("Department")) ? string.Empty : reader["Department"].ToString(),
                            PatientType = reader.IsDBNull(reader.GetOrdinal("Patient Type")) ? string.Empty : reader["Patient Type"].ToString(),
                            NewOld = reader.IsDBNull(reader.GetOrdinal("New / Old")) ? string.Empty : reader["New / Old"].ToString(),
                            RegisteredBy = reader.IsDBNull(reader.GetOrdinal("Registered By")) ? string.Empty : reader["Registered By"].ToString(),
                            VisitDate = reader.IsDBNull(reader.GetOrdinal("Visit Date")) ? DateTime.Now : Convert.ToDateTime(reader["Visit Date"])
                        });
                    }
                }

                // --- FLAG 4: GET_PATIENT_LIST_FOR_HOD ---
                await using (var cmd = new SqlCommand("sp_GetToothTechDailySummary", connection) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("@Flag", "GET_PATIENT_LIST_FOR_HOD");
                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        model.Flag4HodPatients.Add(new Flag4PatientListForHod
                        {
                            Department = reader.IsDBNull(reader.GetOrdinal("Department")) ? string.Empty : reader["Department"].ToString()!,
                            HodName = reader.IsDBNull(reader.GetOrdinal("HOD Name")) ? string.Empty : reader["HOD Name"].ToString()!,
                            HodEmail = reader.IsDBNull(reader.GetOrdinal("HOD Email")) ? string.Empty : reader["HOD Email"].ToString()!,
                            UHID = reader.IsDBNull(reader.GetOrdinal("UHID")) ? string.Empty : reader["UHID"].ToString()!,
                            PatientName = reader.IsDBNull(reader.GetOrdinal("Patient Name")) ? string.Empty : reader["Patient Name"].ToString()!,
                            Gender = reader.IsDBNull(reader.GetOrdinal("Gender")) ? string.Empty : reader["Gender"].ToString()!,
                            Mobile = reader.IsDBNull(reader.GetOrdinal("Mobile")) ? string.Empty : reader["Mobile"].ToString()!,
                            TreatmentGiven = reader.IsDBNull(reader.GetOrdinal("Treatment Given")) ? string.Empty : reader["Treatment Given"].ToString()!,
                            BillAmount = reader.IsDBNull(reader.GetOrdinal("Bill Amount")) ? 0m : Convert.ToDecimal(reader["Bill Amount"])
                        });
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"ToothTech Data Engine Error: {ex.Message}"); }

            try
            {
                model.PeonBookSummary = await _peonBookReportService.GetDailySummaryAsync();
            }
            catch (Exception ex)
            {
                // PEON Book failure should not prevent the HMPK and ToothTech report from being sent.
                Console.WriteLine($"PEON Book Data Engine Error: {ex.Message}");
            }

            try
            {
                model.MrdSummary = await _mrdReportService.GetDailySummaryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MRD Data Engine Error: {ex.Message}");
            }

            try
            {
                model.MarshmallowSummary = await _marshmallowReportService.GetDailySummaryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MarshmallowDB Data Engine Error: {ex.Message}");
            }

            try
            {
                model.PhrSummary = await _phrReportService.GetDailySummaryAsync(model.ReportDate);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PHR Data Engine Error: {ex.Message}");
            }

            return model;
        }
        // ── Safe reader helpers ─────────────────────────────────────────────
        private static int SafeInt(SqlDataReader r, string col)
        {
            try { return r.IsDBNull(r.GetOrdinal(col)) ? 0 : Convert.ToInt32(r[col]); }
            catch { return 0; }
        }
        private static decimal SafeDecimal(SqlDataReader r, string col)
        {
            try { return r.IsDBNull(r.GetOrdinal(col)) ? 0m : Convert.ToDecimal(r[col]); }
            catch { return 0m; }
        }
        private static string SafeString(SqlDataReader r, string col, string fallback = "")
        {
            try { return r.IsDBNull(r.GetOrdinal(col)) ? fallback : r[col]?.ToString() ?? fallback; }
            catch { return fallback; }
        }
    }
}
