using DailyReportEmail.Interfaces;
using DailyReportEmail.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DailyReportEmail.Services
{
    public class PdfService : IPdfService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PdfService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;

            // Initializing Community License natively
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerateDailyReport(DailyReportModel model)
        {
            var logoPath = ResolveLogoPath();
            var reportDateText = model.ReportDate.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture);
            var reportTimeText = DateTime.Now.ToString("hh:mm tt", CultureInfo.InvariantCulture);
            var currencyCulture = CultureInfo.GetCultureInfo("en-IN");
            var onlineCollectionIconPath = ResolveAssetPath("summary-icons", "online-collection.png");
            var overallRevenueIconPath = ResolveAssetPath("summary-icons", "overall-revenue.png");
            var totalPatientsIconPath = ResolveAssetPath("summary-icons", "total-patients.png");
            var phrTotalPrescriptionsIconPath = ResolveAssetPath("summary-icons", "phr-total-prescriptions.png");
            var phrTodayPrescriptionsIconPath = ResolveAssetPath("summary-icons", "phr-today-prescriptions.png");

            // Added .GeneratePdf() at the very end of the Document block to resolve byte[] implicit conversion error.
            return Document.Create(container =>
            {
                // =====================================================================
                // PAGE 1: TOOTHTECH CENTER PERFORMANCE OVERVIEW & FLAG 1 METRICS
                // =====================================================================
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(16);
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Black).FontFamily("Helvetica"));

                    page.Header().PaddingBottom(10).Column(column =>
                    {
                        column.Item().Row(row =>
                        {
                            row.ConstantItem(90).Column(col =>
                            {
                                if (File.Exists(logoPath))
                                    col.Item().Height(42).AlignCenter().Image(logoPath).FitArea();

                                col.Item().PaddingTop(1).AlignCenter().Text("CSSH IT").Bold().FontSize(8).FontColor("#475569");
                            });

                            row.RelativeItem().PaddingHorizontal(12).AlignCenter().Column(col =>
                            {
                                col.Item().AlignCenter().Text("Smile Sync").Bold().FontSize(23).FontColor("#374151");
                                col.Item().AlignCenter().Text("DAILY INSIGHT").Bold().FontSize(13).FontColor("#475569");
                                col.Item().PaddingTop(3).Row(divider =>
                                {
                                    divider.RelativeItem().PaddingTop(4).LineHorizontal(0.8f).LineColor("#475569");
                                    divider.ConstantItem(18).AlignCenter().Text("◆").FontSize(7).FontColor("#475569");
                                    divider.RelativeItem().PaddingTop(4).LineHorizontal(0.8f).LineColor("#475569");
                                });
                                col.Item().PaddingTop(3).AlignCenter().Text($"This report shows today's analytics at Smile Sync of  {reportDateText} {reportTimeText}.").FontSize(10).FontColor(Colors.Grey.Darken2);
                            });

                            row.ConstantItem(80).AlignMiddle().Border(0.7f).BorderColor("#cbd5e1").Padding(4).Column(col =>
                            {
                                col.Item().Text("Report Generation").FontSize(6).FontColor(Colors.Grey.Darken1);
                                col.Item().Text(reportDateText).Bold().FontSize(8).FontColor("#374151");
                                col.Item().Text(reportTimeText).FontSize(7).FontColor("#374151");
                            });
                        });
                        column.Item().PaddingTop(3).LineHorizontal(1.2f).LineColor("#475569");
                    });

                    page.Content().Column(column =>
                    {
                        column.Spacing(12);
                        column.Item().Row(row =>
                        {
                            row.Spacing(7);
                            row.RelativeItem().Border(0.7f).BorderColor("#bbf7d0").Background("#f0fdf4").Padding(8).Column(c => CreateSummaryCard(c, "TODAY'S CASH COLLECTION", FormatCurrency(model.ToothTech_TotalCashPaid, currencyCulture), "Today's counter cash", "₹", "#15803d", "#dcfce7"));
                            row.RelativeItem().Border(0.7f).BorderColor("#bae6fd").Background("#f0f9ff").Padding(8).Column(c => CreateSummaryCard(c, "TODAY'S ONLINE COLLECTION", FormatCurrency(model.ToothTech_TotalOnlinePaid, currencyCulture), "Today's gateway payments", "", "#0369a1", "#e0f2fe", onlineCollectionIconPath));
                            row.RelativeItem().Border(0.7f).BorderColor("#ddd6fe").Background("#f5f3ff").Padding(8).Column(c => CreateOverallRevenueCard(c, model.TodaysRevenue, model.TodaysCollection, model.TodaysDiscount, model.ToothTechDiscountAvailable, currencyCulture, overallRevenueIconPath));
                            row.RelativeItem().Border(0.7f).BorderColor("#fed7aa").Background("#fff7ed").Padding(8).Column(c => CreateSummaryCard(c, "TODAY'S TOTAL PATIENTS", $"{model.ToothTech_TotalFootfall}", $"New: {model.ToothTech_NewPatients} | Old: {model.ToothTech_OldPatients}", "", "#c2410c", "#ffedd5", totalPatientsIconPath));
                        });

                        // --- FLAG 1 Table: Clinical Department Breakdown ---
                        column.Item().PaddingTop(4).Background("#475569").PaddingVertical(6).AlignCenter().Text("DEPARTMENT-WISE PERFORMANCE SUMMARY").Bold().FontSize(11).FontColor(Colors.White);
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(25);   // SNo
                                columns.RelativeColumn(2.0f); // Department Name
                                columns.RelativeColumn(1.0f); // Cash
                                columns.RelativeColumn(1.0f); // Online
                                columns.RelativeColumn(0.7f); // New Pat
                                columns.RelativeColumn(0.7f); // Revisit
                                columns.RelativeColumn(1.1f); // Revenue Created
                                columns.RelativeColumn(1.1f); // Revenue Collected
                                columns.RelativeColumn(0.9f); // Discount
                                columns.RelativeColumn(0.9f); // Prescribed
                                columns.RelativeColumn(1.0f); // Performed
                            });

                            table.Header(header =>
                            {
                                header.Cell().RowSpan(2).Background("#f1f5f9").Border(0.5f).BorderColor("#d1d5db").Padding(4).AlignCenter().AlignMiddle().Text("#").Bold().FontColor("#374151").FontSize(8);
                                header.Cell().RowSpan(2).Background("#f1f5f9").Border(0.5f).BorderColor("#d1d5db").Padding(4).AlignLeft().AlignMiddle().Text("Department").Bold().FontColor("#374151").FontSize(8);
                                header.Cell().ColumnSpan(2).Background("#f1f5f9").Border(0.5f).BorderColor("#d1d5db").Padding(4).AlignCenter().Text("Total Collection").Bold().FontColor("#374151").FontSize(8);
                                header.Cell().ColumnSpan(2).Background("#f1f5f9").Border(0.5f).BorderColor("#d1d5db").Padding(4).AlignCenter().Text("Patient").Bold().FontColor("#374151").FontSize(8);
                                header.Cell().ColumnSpan(3).Background("#f1f5f9").Border(0.5f).BorderColor("#d1d5db").Padding(4).AlignCenter().Text("Revenue").Bold().FontColor("#374151").FontSize(8);
                                header.Cell().ColumnSpan(2).Background("#f1f5f9").Border(0.5f).BorderColor("#d1d5db").Padding(4).AlignCenter().Text("Test").Bold().FontColor("#374151").FontSize(8);

                                foreach (var subHeader in new[] { "Cash", "Online", "New", "Revisit", "Created", "Collected", "Discount", "Prescribed", "Performed" })
                                    header.Cell().Background("#f8fafc").Border(0.5f).BorderColor("#d1d5db").Padding(4).AlignCenter().Text(subHeader).Bold().FontColor("#374151").FontSize(7);
                            });

                            if (model.Departments != null && model.Departments.Any())
                            {
                                foreach (var dept in model.Departments)
                                {
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(4).AlignLeft().Text(dept.SNo.ToString()).FontSize(8);
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(4).AlignLeft().Text(dept.DepartmentName ?? "-").FontSize(8).Bold();
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(4).AlignRight().Text(FormatCurrency(dept.CollectionCash, currencyCulture)).FontSize(8);
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(4).AlignRight().Text(FormatCurrency(dept.CollectionOnline, currencyCulture)).FontSize(8);
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(4).AlignRight().Text(dept.PatientNew.ToString("N0")).FontSize(8);
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(4).AlignRight().Text(dept.PatientReVisit.ToString("N0")).FontSize(8);
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(4).AlignRight().Text(FormatCurrency(dept.RevenueCreated, currencyCulture)).FontSize(8);
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(4).AlignRight().Text(FormatCurrency(dept.RevenueCollected, currencyCulture)).FontSize(8);
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(4).AlignRight().Text(model.ToothTechDiscountAvailable ? FormatCurrency(dept.RevenueDiscount, currencyCulture) : "—").FontSize(8);
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(4).AlignCenter().Text(dept.PrescribedPerformed ?? "-").FontSize(8);
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(4).AlignCenter().Text("-").FontSize(8);
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(11).Border(0.5f).BorderColor("#e9eff8").Padding(10).AlignCenter().Text("No Department Records Available Today").FontColor(Colors.Grey.Darken2).Italic();
                            }
                        });

                        column.Item().PaddingTop(10).Row(row =>
                        {
                            row.Spacing(10);
                            row.RelativeItem(0.9f).Border(0.6f).BorderColor("#cbd5e1").Background(Colors.White).Padding(5).Column(chart =>
                            {
                                chart.Item().AlignCenter().Text("TODAY'S COLLECTION MIX").Bold().FontSize(8).FontColor("#475569");
                                chart.Item().Height(150).Image(CreateSmileSyncPieChart(model.ToothTech_TotalCashPaid, model.ToothTech_TotalOnlinePaid)).FitArea();
                            });
                            row.RelativeItem(1.5f).Border(0.6f).BorderColor("#cbd5e1").Background(Colors.White).Padding(5).Column(chart =>
                            {
                                chart.Item().AlignCenter().Text("7-DAY CASH & ONLINE COLLECTION TREND").Bold().FontSize(8).FontColor("#475569");
                                chart.Item().Height(150).Image(CreateSmileSyncWeeklyChart(model.SmileSyncWeeklyCollections, model.SmileSyncWeeklyHistoryAvailable)).FitArea();
                            });
                        });
                    });
                    page.Footer().Row(row =>
                    {
                        // 1. Left Side: CSSH IT Department
                        row.RelativeItem()
                           .PaddingTop(4)
                           .AlignLeft()
                           .Text("Generated by: CSSH IT Department")
                           .FontColor(Colors.Grey.Darken1)
                           .FontSize(8);

                        // 2. Center Side: Aapka original text (Width thodi badhai hai taaki text ek line mein aaye)
                        row.ConstantItem(250)
                           .PaddingTop(4)
                           .AlignCenter()
                           .Text("Smile Sync Daily Insight • System Generated Report")
                           .FontColor(Colors.Grey.Darken1)
                           .FontSize(8);

                        // 3. Right Side: Page Number (Ya ise khali bhi chhod sakte hain)
                        row.RelativeItem()
                           .PaddingTop(4)
                            .AlignRight()
                            .Text(text =>
                            {
                                text.Span("Page ").FontColor(Colors.Grey.Darken1).FontSize(8);
                                text.CurrentPageNumber().FontColor(Colors.Grey.Darken1).FontSize(8);
                            });
                    });
                    //page.Footer().Row(row =>
                    //{
                    //    row.RelativeItem().PaddingTop(4).LineHorizontal(0.5f).LineColor("#cbd5e1");
                    //    row.ConstantItem(160).AlignCenter().Text("Smile Sync Daily Insight • System Generated Report").FontColor(Colors.Grey.Darken1).FontSize(8);
                    //    row.RelativeItem().PaddingTop(4).LineHorizontal(0.5f).LineColor("#cbd5e1");
                    //});
                });

                // =====================================================================
                // PAGE 2: HMPK SUPPLY CHAIN & PROCUREMENT SUMMARY
                // =====================================================================
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Black).FontFamily("Helvetica"));

                    page.Header().Column(column =>
                    {
                        column.Item().PaddingBottom(6).Row(row =>
                        {
                            row.ConstantItem(90).AlignMiddle().Column(col =>
                            {
                                if (File.Exists(logoPath))
                                    col.Item().Height(42).AlignCenter().Image(logoPath).FitArea();

                                col.Item().PaddingTop(1).AlignCenter().Text("CSSH IT").Bold().FontSize(8).FontColor("#475569");
                            });

                            row.RelativeItem().PaddingHorizontal(12).AlignCenter().AlignMiddle().Column(col =>
                            {
                                col.Item().AlignCenter().Text("HMPK").Bold().FontSize(23).FontColor("#374151");
                                col.Item().AlignCenter().Text("DAILY INSIGHT").Bold().FontSize(13).FontColor("#475569");
                                col.Item().PaddingTop(3).Row(divider =>
                                {
                                    divider.RelativeItem().PaddingTop(4).LineHorizontal(0.8f).LineColor("#475569");
                                    divider.ConstantItem(18).AlignCenter().Text("◆").FontSize(7).FontColor("#475569");
                                    divider.RelativeItem().PaddingTop(4).LineHorizontal(0.8f).LineColor("#475569");
                                });
                                col.Item().PaddingTop(3).AlignCenter().Text($"This report shows today's analytics at HMPK as of {reportDateText} {reportTimeText}.").FontSize(10).FontColor(Colors.Grey.Darken2);
                            });

                            row.ConstantItem(80).AlignMiddle().Border(0.7f).BorderColor("#cbd5e1").Padding(4).Column(col =>
                            {
                                col.Item().Text("Report Generation").FontSize(6).FontColor(Colors.Grey.Darken1);
                                col.Item().Text(reportDateText).Bold().FontSize(8).FontColor("#374151");
                                col.Item().Text(reportTimeText).FontSize(7).FontColor("#374151");
                            });
                        });
                        column.Item().PaddingTop(3).LineHorizontal(1.2f).LineColor("#475569");
                    });

                    page.Content().PaddingTop(12).Column(column =>
                    {
                        column.Spacing(12);
                        column.Item().Row(row =>
                        {
                            row.Spacing(7);
                            row.RelativeItem().Border(0.6f).BorderColor("#bfdbfe").Background("#eff6ff").Padding(8).Column(c => CreateSummaryCard(c, "TODAY'S DEMANDS", $"{model.TodayDemand}", $"Pending: {model.PendingDemand} | Approved: {model.ApprovedDemand} | Rejected: {model.RejectedDemand}", "", "#1d4ed8", "#dbeafe", metricIconSvg: CreateHmpkIcon(1, "#1d4ed8", "#38bdf8")));
                            row.RelativeItem().Border(0.6f).BorderColor("#ddd6fe").Background("#f5f3ff").Padding(8).Column(c => CreateSummaryCard(c, "TODAY'S PO AMOUNT", FormatCurrency(model.TotalPOAmount, currencyCulture), $"POs: {model.TodayPO} | Pending: {model.PendingPO} | Rejected: {model.RejectedPO}", "", "#6d28d9", "#ede9fe", metricIconSvg: CreateHmpkIcon(2, "#6d28d9", "#a78bfa")));
                            row.RelativeItem().Border(0.6f).BorderColor("#a7f3d0").Background("#ecfdf5").Padding(8).Column(c => CreateSummaryCard(c, "TODAY'S DMR AMOUNT", FormatCurrency(model.TotalDMRAmount, currencyCulture), $"DMR: {model.TodayDMR} | Pending: {model.PendingDMR} | Rejected: {model.RejectedDMR}", "", "#047857", "#d1fae5", metricIconSvg: CreateHmpkIcon(3, "#047857", "#34d399")));
                            row.RelativeItem().Border(0.6f).BorderColor("#fed7aa").Background("#fff7ed").Padding(8).Column(c => CreateSummaryCard(c, "TODAY'S BILL AMOUNT", FormatCurrency(model.TotalBillAmount, currencyCulture), $"Payments: {model.TodayBillPayment} | Pending: {model.PendingBillPayment} | Approved: {model.ApprovedBillPayment} | Rejected: {model.RejectedBillPayment}", "", "#c2410c", "#ffedd5", metricIconSvg: CreateHmpkIcon(4, "#c2410c", "#fb923c")));
                        });

                        column.Item().PaddingTop(4).Background("#475569").PaddingVertical(6).AlignCenter().Text("DETAILED SUMMARY").Bold().FontSize(11).FontColor(Colors.White);
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2.1f); // Description
                                columns.RelativeColumn(0.8f); // Total
                                columns.RelativeColumn(0.8f); // Pending
                                columns.RelativeColumn(1.1f); // Completed/Approved
                                columns.RelativeColumn(0.8f); // Rejected
                                columns.RelativeColumn(0.8f); // Active
                                columns.RelativeColumn(0.8f); // Inactive
                            });

                            table.Header(header =>
                            {
                                foreach (var title in new[] { "Description", "Total", "Pending", "Completed /\nApproved", "Rejected", "Active", "Inactive" })
                                    header.Cell().Background("#f1f5f9").Border(0.5f).BorderColor("#d1d5db").Padding(6).AlignCenter().Text(title).Bold().FontSize(8).FontColor("#374151");
                            });

                            void AddRow(string description, string total, string pending, string completed, string rejected, string active, string inactive)
                            {
                                table.Cell().Border(0.5f).BorderColor("#e5e7eb").Padding(6).Text(description).FontSize(9).FontColor("#374151");
                                table.Cell().Border(0.5f).BorderColor("#e5e7eb").Padding(6).AlignCenter().Text(total).FontSize(9);
                                table.Cell().Border(0.5f).BorderColor("#e5e7eb").Padding(6).AlignCenter().Text(pending).FontSize(9);
                                table.Cell().Border(0.5f).BorderColor("#e5e7eb").Padding(6).AlignCenter().Text(completed).FontSize(9);
                                // Rejected column — red color agar value > 0
                                var rejectedInt = int.TryParse(rejected, out var r) ? r : 0;
                                table.Cell().Border(0.5f).BorderColor("#e5e7eb").Padding(6).AlignCenter()
                                    .Text(rejected).FontSize(9)
                                    .FontColor(rejectedInt > 0 ? "#dc2626" : "#374151");
                                table.Cell().Border(0.5f).BorderColor("#e5e7eb").Padding(6).AlignCenter().Text(active).FontSize(9);
                                table.Cell().Border(0.5f).BorderColor("#e5e7eb").Padding(6).AlignCenter().Text(inactive).FontSize(9);
                            }

                            AddRow("Demands",        $"{model.TodayDemand}",       $"{model.PendingDemand}",       $"{model.ApprovedDemand}",       $"{model.RejectedDemand}",       "-",                        "-");
                            AddRow("Purchase Orders",$"{model.TodayPO}",           $"{model.PendingPO}",           $"{model.ApprovedPO}",           $"{model.RejectedPO}",           "-",                        "-");
                            AddRow("DMR",            $"{model.TodayDMR}",          $"{model.PendingDMR}",          $"{model.ApprovedDMR}",          $"{model.RejectedDMR}",          "-",                        "-");
                            AddRow("Bill Payments",  $"{model.TodayBillPayment}",  $"{model.PendingBillPayment}",  $"{model.ApprovedBillPayment}",  $"{model.RejectedBillPayment}",  "-",                        "-");
                            AddRow("Users",          $"{model.ActiveUsers + model.InactiveUsers}", "-",              "-",                             "-",                             $"{model.ActiveUsers}",     $"{model.InactiveUsers}");
                        });

                        column.Item().PaddingTop(10).Row(row =>
                        {
                            row.Spacing(10);
                            row.RelativeItem(0.9f).Border(0.6f).BorderColor("#cbd5e1").Background(Colors.White).Padding(5).Column(chart =>
                            {
                                chart.Item().AlignCenter().Text("TODAY'S DEMAND STATUS MIX").Bold().FontSize(8).FontColor("#475569");
                                chart.Item().Height(150).Image(CreateHmpkPieChart(model.PendingDemand, model.ApprovedDemand, model.RejectedDemand)).FitArea();
                            });
                            row.RelativeItem(1.5f).Border(0.6f).BorderColor("#cbd5e1").Background(Colors.White).Padding(5).Column(chart =>
                            {
                                chart.Item().AlignCenter().Text("7-DAY HMPK ACTIVITY TREND").Bold().FontSize(8).FontColor("#475569");
                                chart.Item().Height(150).Image(CreateHmpkWeeklyChart(model.HmpkWeeklyActivity, model.HmpkWeeklyHistoryAvailable)).FitArea();
                            });
                        });
                    });
                    page.Footer().Row(row =>
                    {
                        // 1. Left Side: CSSH IT Department
                        row.RelativeItem()
                           .PaddingTop(4)
                           .AlignLeft()
                           .Text("Generated by: CSSH IT Department")
                           .FontColor(Colors.Grey.Darken1)
                           .FontSize(8);

                        // 2. Center Side: Aapka original text (Width thodi badhai hai taaki text ek line mein aaye)
                        row.ConstantItem(250)
                           .PaddingTop(4)
                           .AlignCenter()
                           .Text("HMPK Daily Insight • System Generated Report")
                           .FontColor(Colors.Grey.Darken1)
                           .FontSize(8);

                        // 3. Right Side: Page Number (Ya ise khali bhi chhod sakte hain)
                        row.RelativeItem()
                           .PaddingTop(4)
                            .AlignRight()
                            .Text(text =>
                            {
                                text.Span("Page ").FontColor(Colors.Grey.Darken1).FontSize(8);
                                text.CurrentPageNumber().FontColor(Colors.Grey.Darken1).FontSize(8);
                            });
                    });

                    //page.Footer().Row(row =>
                    //{
                    //    row.RelativeItem().PaddingTop(4).LineHorizontal(0.5f).LineColor("#cbd5e1");
                    //    row.ConstantItem(160).AlignCenter().Text("HMPK Daily Insight • System Generated Report").FontColor(Colors.Grey.Darken1).FontSize(8);
                    //    row.RelativeItem().PaddingTop(4).LineHorizontal(0.5f).LineColor("#cbd5e1");
                    //});
                });

                // =====================================================================
                // PAGE 3: FLAG 2 AUDIT CONTROLS (RS1, RS2, RS3 METRICS)
                // =====================================================================
                if (model.Flag2PaymentSummaries.Any() || model.DiscountAlerts.Any() || model.StaffStudentAlerts.Any())
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(20);
                        page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Black).FontFamily("Helvetica"));

                    page.Header().PaddingBottom(10).Column(column =>
                    {
                        column.Item().Text("FLAG 2: Audit Controls & Escalation Governance").Bold().FontSize(16).FontColor("#0b5fff");
                        column.Item().Text("Financial concessions tracking and administrative exceptions report").FontSize(9).FontColor(Colors.Grey.Darken2);
                        column.Item().PaddingTop(4).LineHorizontal(1f).LineColor("#d9e6ff");
                    });

                    page.Content().Column(column =>
                    {
                        column.Spacing(14);

                        // --- RS 1: Department Payment Audit Summary ---
                        column.Item().Text("Result Set 1: Department HOD Financial Remittance Summary").Bold().FontSize(10).FontColor(Colors.Grey.Darken3);
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2.0f); columns.RelativeColumn(2.0f); columns.RelativeColumn(1.0f); columns.RelativeColumn(1.0f); columns.RelativeColumn(1.2f);
                            });

                            table.Header(header =>
                            {
                                string[] headers = { "Department", "HOD Name", "Cash Remitted", "Online Remitted", "Total Settlement" };
                                foreach (var h in headers)
                                {
                                    var cell = header.Cell().Background("#f1f5f9").Border(0.5f).BorderColor("#cbd5e1").Padding(4);
                                    if (h == "Department" || h == "HOD Name")
                                        cell.AlignLeft().Text(h).Bold().FontColor(Colors.Grey.Darken4).FontSize(8);
                                    else
                                        cell.AlignRight().Text(h).Bold().FontColor(Colors.Grey.Darken4).FontSize(8);
                                }
                            });

                            if (model.Flag2PaymentSummaries != null && model.Flag2PaymentSummaries.Any())
                            {
                                foreach (var item in model.Flag2PaymentSummaries)
                                {
                                    table.Cell().Border(0.5f).BorderColor("#e2e8f0").Padding(4).AlignLeft().Text(item.Department).FontSize(8);
                                    table.Cell().Border(0.5f).BorderColor("#e2e8f0").Padding(4).AlignLeft().Text(item.HodName).FontSize(8);
                                    table.Cell().Border(0.5f).BorderColor("#e2e8f0").Padding(4).AlignRight().Text(FormatCurrency(item.CashPaid, currencyCulture)).FontSize(8);
                                    table.Cell().Border(0.5f).BorderColor("#e2e8f0").Padding(4).AlignRight().Text(FormatCurrency(item.OnlinePaid, currencyCulture)).FontSize(8);
                                    table.Cell().Border(0.5f).BorderColor("#e2e8f0").Padding(4).AlignRight().Text(FormatCurrency(item.TotalPaid, currencyCulture)).FontSize(8).Bold();
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(5).Border(0.5f).BorderColor("#e2e8f0").Padding(8).AlignCenter().Text("No Department Payment Overviews Logged").FontColor(Colors.Grey.Darken1).Italic();
                            }
                        });

                        // --- RS 2: General Patient Discount Alert ---
                        column.Item().Text("Result Set 2: Authorized Concessions & Discount Audit Trail").Bold().FontSize(10).FontColor("#b45309");
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.0f); columns.RelativeColumn(1.5f); columns.RelativeColumn(1.5f); columns.RelativeColumn(1.0f); columns.RelativeColumn(1.0f); columns.RelativeColumn(1.0f); columns.RelativeColumn(1.5f);
                            });
                            table.Header(header =>
                            {
                                string[] headers = { "UHID", "Patient Name", "Department", "Gross Amt", "Discount", "Net Paid", "Approved By" };
                                foreach (var h in headers)
                                {
                                    var cell = header.Cell().Background("#fff7ed").Border(0.5f).BorderColor("#fed7aa").Padding(4);
                                    if (h == "Gross Amt" || h == "Discount" || h == "Net Paid")
                                        cell.AlignRight().Text(h).Bold().FontColor("#c2410c").FontSize(8);
                                    else
                                        cell.AlignLeft().Text(h).Bold().FontColor("#c2410c").FontSize(8);
                                }
                            });
                            if (model.DiscountAlerts != null && model.DiscountAlerts.Any())
                            {
                                foreach (var item in model.DiscountAlerts)
                                {
                                    table.Cell().Border(0.5f).BorderColor("#ffedd5").Padding(4).AlignLeft().Text(item.UHID ?? "-").FontSize(8);
                                    table.Cell().Border(0.5f).BorderColor("#ffedd5").Padding(4).AlignLeft().Text(item.PatientName ?? "-").FontSize(8);
                                    table.Cell().Border(0.5f).BorderColor("#ffedd5").Padding(4).AlignLeft().Text(item.DepartmentName ?? "-").FontSize(8);
                                    table.Cell().Border(0.5f).BorderColor("#ffedd5").Padding(4).AlignRight().Text(FormatCurrency(item.Amount, currencyCulture)).FontSize(8);
                                    table.Cell().Border(0.5f).BorderColor("#ffedd5").Padding(4).AlignRight().Text(FormatCurrency(item.Discount, currencyCulture)).FontSize(8).Bold().FontColor("#c2410c");
                                    table.Cell().Border(0.5f).BorderColor("#ffedd5").Padding(4).AlignRight().Text(FormatCurrency(item.PaidAmount, currencyCulture)).FontSize(8);
                                    table.Cell().Border(0.5f).BorderColor("#ffedd5").Padding(4).AlignLeft().Text(item.ApprovedBy ?? "System Auto").FontSize(8);
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(7).Border(0.5f).BorderColor("#ffedd5").Padding(8).AlignCenter().Text("Zero Discount Exceptions Processed Today").FontColor(Colors.Grey.Darken1).Italic();
                            }
                        });
                        // --- RS 3: Staff / Student Exception Alert ---
                        column.Item().Text("Result Set 3: Institutional Subsidy Waivers (Staff & Student Exemptions)").Bold().FontSize(10).FontColor("#047857");
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.0f); columns.RelativeColumn(1.8f); columns.RelativeColumn(1.5f); columns.RelativeColumn(1.2f); columns.RelativeColumn(1.2f);
                            });
                            table.Header(header =>
                            {
                                string[] headers = { "UHID", "Beneficiary Name", "Department Module", "Institutional Class", "Actual Remittance" };
                                foreach (var h in headers)
                                {
                                    var cell = header.Cell().Background("#ecfdf5").Border(0.5f).BorderColor("#a7f3d0").Padding(4);
                                    if (h == "Actual Remittance")
                                        cell.AlignRight().Text(h).Bold().FontColor("#065f46").FontSize(8);
                                    else
                                        cell.AlignLeft().Text(h).Bold().FontColor("#065f46").FontSize(8);
                                }
                            });
                            if (model.StaffStudentAlerts != null && model.StaffStudentAlerts.Any())
                            {
                                foreach (var item in model.StaffStudentAlerts)
                                {
                                    table.Cell().Border(0.5f).BorderColor("#d1fae5").Padding(4).AlignLeft().Text(item.UHID ?? "-").FontSize(8);
                                    table.Cell().Border(0.5f).BorderColor("#d1fae5").Padding(4).AlignLeft().Text(item.PatientName ?? "-").FontSize(8);
                                    table.Cell().Border(0.5f).BorderColor("#d1fae5").Padding(4).AlignLeft().Text(item.DepartmentName ?? "-").FontSize(8);
                                    table.Cell().Border(0.5f).BorderColor("#d1fae5").Padding(4).AlignLeft().Text(item.PatientType ?? "Staff/Student").FontSize(8).Bold();
                                    table.Cell().Border(0.5f).BorderColor("#d1fae5").Padding(4).AlignRight().Text(FormatCurrency(item.PaidAmount, currencyCulture)).FontSize(8);
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(5).Border(0.5f).BorderColor("#d1fae5").Padding(8).AlignCenter().Text("No Institutional Classification Discounts Claimed").FontColor(Colors.Grey.Darken1).Italic();
                            }
                        });
                    });

                        page.Footer().Row(row =>
                        {
                            row.RelativeItem().PaddingTop(4).LineHorizontal(0.5f).LineColor("#d9e6ff");
                            row.ConstantItem(120).AlignCenter().Text("Audit Verification Trail").FontColor(Colors.Grey.Darken1).FontSize(8);
                            row.RelativeItem().PaddingTop(4).LineHorizontal(0.5f).LineColor("#d9e6ff");
                        });
                    });
                }

                // =====================================================================
                // PAGE 4: FLAG 3 AGGREGATE PATIENT DAILY ROSTER
                // =====================================================================
                if (model.TodayPatients.Any())
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(20);
                        page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Black).FontFamily("Helvetica"));

                    page.Header().PaddingBottom(10).Column(column =>
                    {
                        column.Item().Text("FLAG 3: Master Roster - Comprehensive Daily Patient Intake").Bold().FontSize(16).FontColor("#0b5fff");
                        column.Item().Text("Complete listing of patient visits, demographic status, and operational reception desks").FontSize(9).FontColor(Colors.Grey.Darken2);
                        column.Item().PaddingTop(4).LineHorizontal(1f).LineColor("#d9e6ff");
                    });

                    page.Content().Column(column =>
                    {
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(0.9f); columns.RelativeColumn(1.5f); columns.ConstantColumn(35); columns.RelativeColumn(1.0f); columns.RelativeColumn(1.5f); columns.RelativeColumn(0.9f); columns.ConstantColumn(45); columns.RelativeColumn(1.2f); columns.RelativeColumn(1.1f);
                            });

                            table.Header(header =>
                            {
                                string[] headers = { "UHID", "Patient Name", "Sex", "Mobile Num", "Assigned Unit", "Type", "Status", "Registered By", "Visit Roster" };
                                foreach (var h in headers)
                                {
                                    header.Cell().Background("#e8f1ff").Border(0.5f).BorderColor("#d6e7ff").Padding(4).AlignLeft().Text(h).Bold().FontColor("#0b5fff").FontSize(8);
                                }
                            });

                            if (model.TodayPatients != null && model.TodayPatients.Any())
                            {
                                foreach (var patient in model.TodayPatients)
                                {
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(3).AlignLeft().Text(patient.UHID ?? "-").FontSize(7);
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(3).AlignLeft().Text(patient.Name ?? "-").FontSize(7).Bold();
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(3).AlignLeft().Text(patient.Gender ?? "-").FontSize(7);
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(3).AlignLeft().Text(patient.Mobile ?? "-").FontSize(7);
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(3).AlignLeft().Text(patient.Department ?? "-").FontSize(7);
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(3).AlignLeft().Text(patient.PatientType ?? "General").FontSize(7);
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(3).AlignLeft().Text(patient.NewOld ?? "-").FontSize(7);
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(3).AlignLeft().Text(patient.RegisteredBy ?? "Desk System").FontSize(7);
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(3).AlignLeft().Text(patient.VisitDate == DateTime.MinValue ? DateTime.Now.ToString("dd-MMM") : patient.VisitDate.ToString("dd-MMM-yyyy")).FontSize(7);
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(9).Border(0.5f).BorderColor("#e9eff8").Padding(12).AlignCenter().Text("Zero Patient Intake Registered for the Day").FontColor(Colors.Grey.Darken1).Italic();
                            }
                        });
                    });

                        page.Footer().Row(row =>
                        {
                            row.RelativeItem().PaddingTop(4).LineHorizontal(0.5f).LineColor("#d9e6ff");
                            row.ConstantItem(120).AlignCenter().Text("Master Traffic Log").FontColor(Colors.Grey.Darken1).FontSize(8);
                            row.RelativeItem().PaddingTop(4).LineHorizontal(0.5f).LineColor("#d9e6ff");
                        });
                    });
                }

                // =====================================================================
                // DYNAMIC SEGMENT: FLAG 4 HOD LEVEL SPECIFIC PATIENT LEDGERS
                // =====================================================================
                if (model.Flag4HodPatients != null && model.Flag4HodPatients.Any())
                {
                    var groupedByHod = model.Flag4HodPatients.GroupBy(x => new { x.HodName, x.Department, x.HodEmail });

                    foreach (var group in groupedByHod)
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(20);
                            page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Black).FontFamily("Helvetica"));

                            page.Header().PaddingBottom(10).Column(column =>
                            {
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text($"FLAG 4: Specialist Clinical Log - {group.Key.Department}").Bold().FontSize(14).FontColor("#1e3a8a");
                                        col.Item().PaddingTop(2).Row(r =>
                                        {
                                            r.ConstantItem(180).Text($"Chief/HOD: {group.Key.HodName}").Bold().FontSize(9).FontColor(Colors.Grey.Darken3);
                                            r.RelativeItem().Text($"Direct Mail Routing: {group.Key.HodEmail}").FontSize(8).FontColor(Colors.Grey.Darken1).Italic();
                                        });
                                    });
                                });
                                column.Item().PaddingTop(4).LineHorizontal(1f).LineColor("#1e3a8a");
                            });

                            page.Content().Column(column =>
                            {
                                column.Spacing(8);
                                column.Item().Text($"The following patients were assigned to internal clinical pathways within the department of {group.Key.Department} under the governance of HOD {group.Key.HodName}.").FontSize(8).FontColor(Colors.Grey.Darken2);

                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(1.0f); columns.RelativeColumn(1.8f); columns.ConstantColumn(40); columns.RelativeColumn(1.2f); columns.RelativeColumn(2.5f); columns.RelativeColumn(1.2f);
                                    });

                                    table.Header(header =>
                                    {
                                        string[] headers = { "Patient UHID", "Patient Name", "Gender", "Contact Mobile", "Clinical Procedure", "Total Cost" };
                                        foreach (var h in headers)
                                        {
                                            var cell = header.Cell().Background("#eff6ff").Border(0.5f).BorderColor("#bfdbfe").Padding(4);
                                            if (h == "Total Cost")
                                                cell.AlignRight().Text(h).Bold().FontColor("#1e40af").FontSize(8);
                                            else
                                                cell.AlignLeft().Text(h).Bold().FontColor("#1e40af").FontSize(8);
                                        }
                                    });

                                    decimal departmentTotalGross = 0m;
                                    foreach (var patient in group)
                                    {
                                        table.Cell().Border(0.5f).BorderColor("#dbeafe").Padding(4).AlignLeft().Text(patient.UHID).FontSize(8);
                                        table.Cell().Border(0.5f).BorderColor("#dbeafe").Padding(4).AlignLeft().Text(patient.PatientName).FontSize(8).Bold();
                                        table.Cell().Border(0.5f).BorderColor("#dbeafe").Padding(4).AlignLeft().Text(patient.Gender).FontSize(8);
                                        table.Cell().Border(0.5f).BorderColor("#dbeafe").Padding(4).AlignLeft().Text(patient.Mobile).FontSize(8);
                                        table.Cell().Border(0.5f).BorderColor("#dbeafe").Padding(4).AlignLeft().Text(!string.IsNullOrWhiteSpace(patient.TreatmentGiven) ? patient.TreatmentGiven : "Observation / Active Checkup").FontSize(8);
                                        table.Cell().Border(0.5f).BorderColor("#dbeafe").Padding(4).AlignRight().Text(FormatCurrency(patient.GrossAmount, currencyCulture)).FontSize(8);

                                        departmentTotalGross += patient.GrossAmount;
                                    }

                                    // Total row for the HOD segment
                                    table.Cell().ColumnSpan(5).Border(0.5f).BorderColor("#dbeafe").Background("#f8fafc").Padding(4).AlignRight().Text("Department Total Summary:").Bold().FontSize(8);
                                    table.Cell().Border(0.5f).BorderColor("#dbeafe").Background("#f8fafc").Padding(4).AlignRight().Text(FormatCurrency(departmentTotalGross, currencyCulture)).Bold().FontSize(8).FontColor("#1e40af");
                                });
                            });

                            page.Footer().Row(row =>
                            {
                                row.RelativeItem().PaddingTop(4).LineHorizontal(0.5f).LineColor("#bfdbfe");
                                row.ConstantItem(150).AlignCenter().Text($"Flag 4 • {group.Key.Department} Ledger").FontColor(Colors.Grey.Darken1).FontSize(8);
                                row.RelativeItem().PaddingTop(4).LineHorizontal(0.5f).LineColor("#bfdbfe");
                            });
                        });
                    }
                }

                // =====================================================================
                // PAGE: PEON BOOK DAILY DELIVERY SUMMARY
                // =====================================================================
                var peonBook = model.PeonBookSummary ?? new PeonBookDailySummary { ReportDate = DateTime.Today };
                if (peonBook.ReportDate == DateTime.MinValue)
                    peonBook.ReportDate = DateTime.Today;
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(24);
                    page.DefaultTextStyle(style => style.FontFamily("Helvetica").FontSize(9));

                    page.Header().Column(header =>
                    {
                        header.Item().Row(row =>
                        {
                            row.ConstantItem(90).Column(col =>
                            {
                                if (File.Exists(logoPath))
                                    col.Item().Height(42).AlignCenter().Image(logoPath).FitArea();

                                col.Item().PaddingTop(1).AlignCenter().Text("CSSH IT").Bold().FontSize(8).FontColor("#475569");
                            });

                            row.RelativeItem().PaddingHorizontal(12).AlignCenter().Column(col =>
                            {
                                col.Item().AlignCenter().Text("PEON BOOK").Bold().FontSize(23).FontColor("#374151");
                                col.Item().AlignCenter().Text("DAILY INSIGHT").Bold().FontSize(13).FontColor("#475569");
                                col.Item().PaddingTop(3).Row(divider =>
                                {
                                    divider.RelativeItem().PaddingTop(4).LineHorizontal(0.8f).LineColor("#475569");
                                    divider.ConstantItem(18).AlignCenter().Text("◆").FontSize(7).FontColor("#475569");
                                    divider.RelativeItem().PaddingTop(4).LineHorizontal(0.8f).LineColor("#475569");
                                });
                                col.Item().PaddingTop(3).AlignCenter().Text($"This report shows today's analytics at PEON BOOK of {reportDateText} {reportTimeText}.").FontSize(10).FontColor(Colors.Grey.Darken2);
                            });

                            row.ConstantItem(80).AlignMiddle().Border(0.7f).BorderColor("#cbd5e1").Padding(4).Column(col =>
                            {
                                col.Item().Text("Report Generation").FontSize(6).FontColor(Colors.Grey.Darken1);
                                col.Item().Text(reportDateText).Bold().FontSize(8).FontColor("#374151");
                                col.Item().Text(reportTimeText).FontSize(7).FontColor("#374151");
                            });
                        });
                        header.Item().PaddingTop(3).LineHorizontal(1.2f).LineColor("#475569");
                    });

                    page.Content().PaddingTop(16).Column(content =>
                    {
                        content.Spacing(14);
                        content.Item().Row(row =>
                        {
                            row.Spacing(7);
                            AddPeonBookCard(row, 1, "TOTAL CREATED", peonBook.TotalCreated, "All-time total records", "#eff6ff", "#1d4ed8");
                            AddPeonBookCard(row, 2, "TOTAL DELIVERED", peonBook.TotalDelivered, "All-time delivered records", "#fef3c7", "#b45309");
                            AddPeonBookCard(row, 3, "TOTAL RECEIVED", peonBook.TotalReceived, "All-time received records", "#ecfdf5", "#047857");
                            AddPeonBookCard(row, 4, "TOTAL INACTIVE", peonBook.TotalInactive, "All-time inactive records", "#f1f5f9", "#475569");
                            AddPeonBookCard(row, 5, "TOTAL PENDING", peonBook.TotalPending, "All-time pending records", "#fef2f2", "#b91c1c");
                        });

                        content.Item().PaddingTop(8).Background("#475569").PaddingVertical(6).AlignCenter().Text("TODAY'S ENTRY STATUS").Bold().FontSize(11).FontColor(Colors.White);
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn();
                            });
                            table.Header(header =>
                            {
                                header.Cell().Background("#1e3a8a").Padding(7).Text("Metric").Bold().FontColor(Colors.White);
                                header.Cell().Background("#1e3a8a").Padding(7).Text("Today").Bold().FontColor(Colors.White);
                            });
                            AddPeonBookRow(table, 1, "Created", peonBook.TodayCreated, "#eff6ff", "#1d4ed8");
                            AddPeonBookRow(table, 2, "Delivered", peonBook.TodayDelivered, "#fef3c7", "#b45309");
                            AddPeonBookRow(table, 3, "Received", peonBook.TodayReceived, "#ecfdf5", "#047857");
                            AddPeonBookRow(table, 4, "Inactive", peonBook.TodayInactive, "#f1f5f9", "#475569");
                            AddPeonBookRow(table, 5, "Pending", peonBook.TodayPending, "#fef2f2", "#b91c1c");
                        });

                        content.Item().PaddingTop(2).Row(row =>
                        {
                            row.Spacing(10);
                            row.RelativeItem(0.9f).Border(0.6f).BorderColor("#cbd5e1").Background(Colors.White).Padding(5).Column(chart =>
                            {
                                chart.Item().AlignCenter().Text("TODAY'S ENTRY STATUS MIX").Bold().FontSize(8).FontColor("#475569");
                                chart.Item().Height(150).Image(CreatePeonBookPieChart(peonBook)).FitArea();
                            });
                            row.RelativeItem(1.5f).Border(0.6f).BorderColor("#cbd5e1").Background(Colors.White).Padding(5).Column(chart =>
                            {
                                chart.Item().AlignCenter().Text("7-DAY PEON BOOK ENTRY TREND").Bold().FontSize(8).FontColor("#475569");
                                chart.Item().Height(150).Image(CreatePeonBookWeeklyChart(peonBook.WeeklyActivity, peonBook.WeeklyHistoryAvailable)).FitArea();
                            });
                        });
                    });
                    page.Footer().Row(row =>
                    {
                        // 1. Left Side: CSSH IT Department
                        row.RelativeItem()
                           .PaddingTop(4)
                           .AlignLeft()
                           .Text("Generated by: CSSH IT Department")
                           .FontColor(Colors.Grey.Darken1)
                           .FontSize(8);

                        // 2. Center Side: Aapka original text (Width thodi badhai hai taaki text ek line mein aaye)
                        row.ConstantItem(250)
                           .PaddingTop(4)
                           .AlignCenter()
                           .Text("PEON BOOK Daily Insight • System Generated Report")
                           .FontColor(Colors.Grey.Darken1)
                           .FontSize(8);

                        // 3. Right Side: Page Number (Ya ise khali bhi chhod sakte hain)
                        row.RelativeItem()
                           .PaddingTop(4)
                            .AlignRight()
                            .Text(text =>
                            {
                                text.Span("Page ").FontColor(Colors.Grey.Darken1).FontSize(8);
                                text.CurrentPageNumber().FontColor(Colors.Grey.Darken1).FontSize(8);
                            });
                    });
                    //page.Footer().AlignCenter().Text("PEON BOOK • System generated daily summary").FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                // MRD page is part of the same combined daily report.
                var mrd = model.MrdSummary ?? new MrdDailySummary { ReportDate = DateTime.Today };
                if (mrd.ReportDate == DateTime.MinValue)
                    mrd.ReportDate = DateTime.Today;

                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(24);
                    page.DefaultTextStyle(style => style.FontFamily("Helvetica").FontSize(9));
                    page.Header().Column(header =>
                    {
                        header.Item().Row(row =>
                        {
                            row.ConstantItem(90).Column(col =>
                            {
                                if (File.Exists(logoPath)) col.Item().Height(42).AlignCenter().Image(logoPath).FitArea();
                                col.Item().PaddingTop(1).AlignCenter().Text("CSSH IT").Bold().FontSize(8).FontColor("#475569");
                            });
                            row.RelativeItem().PaddingHorizontal(12).AlignCenter().Column(col =>
                            {
                                col.Item().AlignCenter().Text("MRD").Bold().FontSize(23).FontColor("#374151");
                                col.Item().AlignCenter().Text("DAILY INSIGHT").Bold().FontSize(13).FontColor("#475569");
                                col.Item().PaddingTop(3).Row(divider =>
                                {
                                    divider.RelativeItem().PaddingTop(4).LineHorizontal(0.8f).LineColor("#475569");
                                    divider.ConstantItem(18).AlignCenter().Text("◆").FontSize(7).FontColor("#475569");
                                    divider.RelativeItem().PaddingTop(4).LineHorizontal(0.8f).LineColor("#475569");
                                });
                                col.Item().PaddingTop(3).AlignCenter().Text($"This report shows today's analytics at MRD as of {reportDateText} {reportTimeText}.").FontSize(10).FontColor(Colors.Grey.Darken2);
                            });
                            row.ConstantItem(80).AlignMiddle().Border(0.7f).BorderColor("#cbd5e1").Padding(4).Column(col =>
                            {
                                col.Item().Text("Report Generation").FontSize(6).FontColor(Colors.Grey.Darken1);
                                col.Item().Text(reportDateText).Bold().FontSize(8).FontColor("#374151");
                                col.Item().Text(reportTimeText).FontSize(7).FontColor("#374151");
                            });
                        });
                        header.Item().PaddingTop(3).LineHorizontal(1.2f).LineColor("#475569");
                    });
                    page.Content().PaddingTop(16).Column(content =>
                    {
                        content.Spacing(8);
                        content.Item().Row(row =>
                        {
                            row.Spacing(7);
                            AddMrdCard(row, 1, "ACTIVE RECORDS", mrd.TotalActiveRecords, "#ecfdf5", "#047857");
                            AddMrdCard(row, 2, "INACTIVE RECORDS", mrd.TotalInactiveRecords, "#f1f5f9", "#475569");
                            AddMrdCard(row, 3, "UPLOADED FILES", mrd.TotalUploadedFiles, "#eff6ff", "#1d4ed8");
                            AddMrdCard(row, 4, "PATIENT RECORDS", mrd.TotalPatientRecords, "#f5f3ff", "#6d28d9");
                        });
                        content.Item().Row(row =>
                        {
                            row.Spacing(7);
                            AddMrdCard(row, 5, "VERIFIED RECORDS", mrd.TotalVerifiedRecords, "#ecfdf5", "#047857");
                            AddMrdCard(row, 6, "PENDING VERIFICATION", mrd.TotalPendingVerification, "#fef2f2", "#b91c1c");
                            AddMrdCard(row, 7, "ADMISSIONS", mrd.TotalAdmissions, "#fef3c7", "#b45309");
                            AddMrdCard(row, 8, "DISCHARGES", mrd.TotalDischarges, "#fff7ed", "#c2410c");
                        });
                        content.Item().PaddingTop(10).Background("#475569").PaddingVertical(6).AlignCenter().Text("TODAY'S MRD STATUS").Bold().FontSize(11).FontColor(Colors.White);
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns => { columns.RelativeColumn(2); columns.RelativeColumn(); });
                            table.Header(header =>
                            {
                                header.Cell().Background("#1e3a8a").Padding(7).Text("Metric").Bold().FontColor(Colors.White);
                                header.Cell().Background("#1e3a8a").Padding(7).Text("Today").Bold().FontColor(Colors.White);
                            });
                            AddMrdRow(table, 1, "Active Records", mrd.TodayActiveRecords, "#ecfdf5", "#047857");
                            AddMrdRow(table, 2, "Inactive Records", mrd.TodayInactiveRecords, "#f1f5f9", "#475569");
                            AddMrdRow(table, 3, "Uploaded Files", mrd.TodayUploadedFiles, "#eff6ff", "#1d4ed8");
                            AddMrdRow(table, 4, "Patient Records", mrd.TodayPatientRecords, "#f5f3ff", "#6d28d9");
                            AddMrdRow(table, 5, "Verified Records", mrd.TodayVerifiedRecords, "#ecfdf5", "#047857");
                            AddMrdRow(table, 6, "Pending Verification", mrd.TodayPendingVerification, "#fef2f2", "#b91c1c");
                            AddMrdRow(table, 7, "Admissions", mrd.TodayAdmissions, "#fef3c7", "#b45309");
                            AddMrdRow(table, 8, "Discharges", mrd.TodayDischarges, "#fff7ed", "#c2410c");
                        });
                        content.Item().PaddingTop(2).Row(row =>
                        {
                            row.Spacing(10);
                            row.RelativeItem(0.9f).Border(0.6f).BorderColor("#cbd5e1").Background(Colors.White).Padding(5).Column(chart =>
                            {
                                chart.Item().AlignCenter().Text("TODAY'S VERIFICATION STATUS").Bold().FontSize(8).FontColor("#475569");
                                chart.Item().Height(135).Image(CreateMrdPieChart(mrd)).FitArea();
                            });
                            row.RelativeItem(1.5f).Border(0.6f).BorderColor("#cbd5e1").Background(Colors.White).Padding(5).Column(chart =>
                            {
                                chart.Item().AlignCenter().Text("7-DAY MRD ACTIVITY TREND").Bold().FontSize(8).FontColor("#475569");
                                chart.Item().Height(135).Image(CreateMrdWeeklyChart(mrd.WeeklyActivity, mrd.WeeklyHistoryAvailable)).FitArea();
                            });
                        });
                    });
                    page.Footer().Row(row =>
                    {
                        // 1. Left Side: CSSH IT Department
                        row.RelativeItem()
                           .PaddingTop(4)
                           .AlignLeft()
                           .Text("Generated by: CSSH IT Department")
                           .FontColor(Colors.Grey.Darken1)
                           .FontSize(8);

                        // 2. Center Side: Aapka original text (Width thodi badhai hai taaki text ek line mein aaye)
                        row.ConstantItem(250)
                           .PaddingTop(4)
                           .AlignCenter()
                           .Text("MRD Daily Insight • System Generated Report")
                           .FontColor(Colors.Grey.Darken1)
                           .FontSize(8);

                        // 3. Right Side: Page Number (Ya ise khali bhi chhod sakte hain)
                        row.RelativeItem()
                           .PaddingTop(4)
                            .AlignRight()
                            .Text(text =>
                            {
                                text.Span("Page ").FontColor(Colors.Grey.Darken1).FontSize(8);
                                text.CurrentPageNumber().FontColor(Colors.Grey.Darken1).FontSize(8);
                            });
                    });
                    //page.Footer().AlignCenter().Text("MRD • System generated daily summary").FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                var marshmallow = model.MarshmallowSummary ?? new MarshmallowDailySummary { ReportDate = DateTime.Today };
                var marshmallowStores = marshmallow.Stores ?? new List<MarshmallowStoreDailySummary>();
                var marshmallowLifetimeStores = marshmallow.LifetimeStores ?? new List<MarshmallowStoreTotalSummary>();
                var marshmallowCulture = CultureInfo.GetCultureInfo("en-IN");

                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(24);
                    page.DefaultTextStyle(style => style.FontFamily("Helvetica").FontSize(9));
                    page.Header().Column(header =>
                    {
                        header.Item().Row(row =>
                        {
                            row.ConstantItem(90).Column(col =>
                            {
                                if (File.Exists(logoPath)) col.Item().Height(42).AlignCenter().Image(logoPath).FitArea();
                                col.Item().PaddingTop(1).AlignCenter().Text("CSSH IT").Bold().FontSize(8).FontColor("#475569");
                            });
                            row.RelativeItem().PaddingHorizontal(12).AlignCenter().Column(col =>
                            {
                                col.Item().AlignCenter().Text("MARSH MALLOW").Bold().FontSize(23).FontColor("#374151");
                                col.Item().AlignCenter().Text("DAILY INSIGHT").Bold().FontSize(13).FontColor("#475569");
                                col.Item().PaddingTop(3).Row(divider =>
                                {
                                    divider.RelativeItem().PaddingTop(4).LineHorizontal(0.8f).LineColor("#475569");
                                    divider.ConstantItem(18).AlignCenter().Text("◆").FontSize(7).FontColor("#475569");
                                    divider.RelativeItem().PaddingTop(4).LineHorizontal(0.8f).LineColor("#475569");
                                });
                                col.Item().PaddingTop(3).AlignCenter().Text($"Cumulative store totals through {marshmallow.ReportDate:dd-MMM-yyyy}.").FontSize(10).FontColor(Colors.Grey.Darken2);
                            });
                            row.ConstantItem(80).AlignMiddle().Border(0.7f).BorderColor("#cbd5e1").Padding(4).Column(col =>
                            {
                                col.Item().Text("Report Generation").FontSize(6).FontColor(Colors.Grey.Darken1);
                                col.Item().Text(reportDateText).Bold().FontSize(8).FontColor("#374151");
                                col.Item().Text(reportTimeText).FontSize(7).FontColor("#374151");
                            });
                        });
                        header.Item().PaddingTop(3).LineHorizontal(1.2f).LineColor("#475569");
                    });

                    page.Content().PaddingTop(16).Column(content =>
                    {
                        content.Spacing(10);
                        content.Item().Row(row =>
                        {
                            row.Spacing(7);
                            row.RelativeItem().Border(0.7f).BorderColor("#bae6fd").Background("#eff6ff").Padding(8).Column(card =>
                                CreateSummaryCard(card, "ACTIVE STORES", marshmallow.ActiveStoreCount.ToString("N0"), "Currently active stores", "", "#1d4ed8", "#dbeafe", metricIconSvg: CreateMarshmallowIcon(0, "#1d4ed8", "#38bdf8")));
                            row.RelativeItem().Border(0.7f).BorderColor("#bbf7d0").Background("#f0fdf4").Padding(8).Column(card =>
                                CreateSummaryCard(card, "TOTAL ORDERS", marshmallow.TotalOrders.ToString("N0"), "All stores to date", "", "#15803d", "#dcfce7", metricIconSvg: CreateMarshmallowIcon(1, "#15803d", "#4ade80")));
                            row.RelativeItem().Border(0.7f).BorderColor("#ddd6fe").Background("#f5f3ff").Padding(8).Column(card =>
                                CreateSummaryCard(card, "TOTAL ORDER VALUE", FormatCurrency(marshmallow.TotalOrderValue, marshmallowCulture), "All stores to date", "", "#6d28d9", "#ede9fe", metricIconSvg: CreateMarshmallowIcon(2, "#6d28d9", "#a78bfa")));
                            row.RelativeItem().Border(0.7f).BorderColor("#fed7aa").Background("#fff7ed").Padding(8).Column(card =>
                                CreateSummaryCard(card, "TOTAL DELIVERED", marshmallow.TotalDelivered.ToString("N0"), "Orders delivered to date", "", "#c2410c", "#ffedd5", metricIconSvg: CreateMarshmallowIcon(3, "#c2410c", "#fb923c")));
                        });

                        content.Item().PaddingTop(8).Background("#475569").PaddingVertical(6).AlignCenter().Text("STORE TOTALS — TO DATE").Bold().FontSize(11).FontColor(Colors.White);
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2.8f);
                                columns.RelativeColumn(1.0f);
                                columns.RelativeColumn(1.0f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.0f);
                            });
                            table.Header(header =>
                            {
                                foreach (var title in new[] { "Store", "Status", "Total Orders", "Total Order Value", "Delivered" })
                                    header.Cell().Background("#1e3a8a").Border(0.4f).BorderColor(Colors.White).Padding(6).AlignCenter().AlignMiddle().Text(title).Bold().FontSize(8).FontColor(Colors.White);
                            });

                            if (marshmallowLifetimeStores.Count == 0)
                            {
                                table.Cell().ColumnSpan(5).Border(0.5f).BorderColor("#e9eff8").Padding(10).AlignCenter().Text("No Store History Available").FontColor(Colors.Grey.Darken2).Italic();
                            }
                            else
                            {
                                foreach (var store in marshmallowLifetimeStores)
                                {
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(6).Text(store.StoreName).FontSize(8).Bold();
                                    var activeStore = string.Equals(store.StoreStatus, "Active", StringComparison.OrdinalIgnoreCase);
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(6).AlignCenter().Text(store.StoreStatus).FontSize(8).FontColor(activeStore ? "#15803d" : "#64748b");
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(6).AlignRight().Text(store.Orders.ToString("N0")).FontSize(8);
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(6).AlignRight().Text(FormatCurrency(store.OrderValue, marshmallowCulture)).FontSize(8);
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(6).AlignRight().Text(store.Delivered.ToString("N0")).FontSize(8);
                                }

                                table.Cell().Background("#f1f5f9").Border(0.5f).BorderColor("#cbd5e1").Padding(6).Text("ALL STORES").Bold().FontSize(8);
                                table.Cell().Background("#f1f5f9").Border(0.5f).BorderColor("#cbd5e1").Padding(6).AlignCenter().Text("To date").Bold().FontSize(8);
                                table.Cell().Background("#f1f5f9").Border(0.5f).BorderColor("#cbd5e1").Padding(6).AlignRight().Text(marshmallow.TotalOrders.ToString("N0")).Bold().FontSize(8);
                                table.Cell().Background("#f1f5f9").Border(0.5f).BorderColor("#cbd5e1").Padding(6).AlignRight().Text(FormatCurrency(marshmallow.TotalOrderValue, marshmallowCulture)).Bold().FontSize(8);
                                table.Cell().Background("#f1f5f9").Border(0.5f).BorderColor("#cbd5e1").Padding(6).AlignRight().Text(marshmallow.TotalDelivered.ToString("N0")).Bold().FontSize(8);
                            }
                        });
                        content.Item().PaddingTop(8).Row(row =>
                        {
                            row.Spacing(10);
                            row.RelativeItem(0.95f).Border(0.6f).BorderColor("#cbd5e1").Background(Colors.White).Padding(5).Column(chart =>
                            {
                                chart.Item().AlignCenter().Text("LIFETIME ORDERS BY STORE").Bold().FontSize(8).FontColor("#475569");
                                chart.Item().Height(185).Image(CreateMarshmallowLifetimeStorePieChart(marshmallowLifetimeStores)).FitArea();
                            });
                            row.RelativeItem(1.45f).Border(0.6f).BorderColor("#cbd5e1").Background(Colors.White).Padding(5).Column(chart =>
                            {
                                chart.Item().AlignCenter().Text("LIFETIME ORDER VALUE BY STORE").Bold().FontSize(8).FontColor("#475569");
                                chart.Item().Height(185).Image(CreateMarshmallowLifetimeStoreBarChart(marshmallowLifetimeStores)).FitArea();
                            });
                        });
                        //content.Item().PaddingTop(6).Text("Totals include all store history through the report date, including stores that are no longer active.").FontSize(7).FontColor(Colors.Grey.Darken2);
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().PaddingTop(4).AlignLeft().Text("Generated by: CSSH IT Department").FontColor(Colors.Grey.Darken1).FontSize(8);
                        row.ConstantItem(250).PaddingTop(4).AlignCenter().Text("Marsh Mallow Daily Insight • System Generated Report").FontColor(Colors.Grey.Darken1).FontSize(8);
                        row.RelativeItem().PaddingTop(4).AlignRight().Text(text =>
                        {
                            text.Span("Page ").FontColor(Colors.Grey.Darken1).FontSize(8);
                            text.CurrentPageNumber().FontColor(Colors.Grey.Darken1).FontSize(8);
                        });
                    });
                });

                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(24);
                    page.DefaultTextStyle(style => style.FontFamily("Helvetica").FontSize(9));
                    page.Header().Column(header =>
                    {
                        header.Item().Row(row =>
                        {
                            row.ConstantItem(90).Column(col =>
                            {
                                if (File.Exists(logoPath)) col.Item().Height(42).AlignCenter().Image(logoPath).FitArea();
                                col.Item().PaddingTop(1).AlignCenter().Text("CSSH IT").Bold().FontSize(8).FontColor("#475569");
                            });
                            row.RelativeItem().PaddingHorizontal(12).AlignCenter().Column(col =>
                            {
                                col.Item().AlignCenter().Text("MARSH MALLOW").Bold().FontSize(23).FontColor("#374151");
                                col.Item().AlignCenter().Text("TODAY'S STORE INSIGHT").Bold().FontSize(13).FontColor("#475569");
                                col.Item().PaddingTop(3).Row(divider =>
                                {
                                    divider.RelativeItem().PaddingTop(4).LineHorizontal(0.8f).LineColor("#475569");
                                    divider.ConstantItem(18).AlignCenter().Text("◆").FontSize(7).FontColor("#475569");
                                    divider.RelativeItem().PaddingTop(4).LineHorizontal(0.8f).LineColor("#475569");
                                });
                                col.Item().PaddingTop(3).AlignCenter().Text($"Store-wise activity for {marshmallow.ReportDate:dd-MMM-yyyy}.").FontSize(10).FontColor(Colors.Grey.Darken2);
                            });
                            row.ConstantItem(80).AlignMiddle().Border(0.7f).BorderColor("#cbd5e1").Padding(4).Column(col =>
                            {
                                col.Item().Text("Report Generation").FontSize(6).FontColor(Colors.Grey.Darken1);
                                col.Item().Text(reportDateText).Bold().FontSize(8).FontColor("#374151");
                                col.Item().Text(reportTimeText).FontSize(7).FontColor("#374151");
                            });
                        });
                        header.Item().PaddingTop(3).LineHorizontal(1.2f).LineColor("#475569");
                    });

                    page.Content().PaddingTop(16).Column(content =>
                    {
                        content.Spacing(9);
                        content.Item().Background("#475569").PaddingVertical(6).AlignCenter().Text("TODAY'S STORE-WISE ORDER SUMMARY").Bold().FontSize(11).FontColor(Colors.White);
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2.5f);
                                columns.RelativeColumn(0.7f);
                                columns.RelativeColumn(1.15f);
                                columns.RelativeColumn(0.8f);
                                columns.RelativeColumn(0.8f);
                                columns.RelativeColumn(0.8f);
                                columns.RelativeColumn(0.8f);
                                columns.RelativeColumn(0.8f);
                                columns.RelativeColumn(0.65f);
                            });
                            table.Header(header =>
                            {
                                foreach (var title in new[] { "Store", "Orders", "Order Value", "Confirmed", "Preparing", "Delivered", "Pending", "Cancelled", "Other" })
                                    header.Cell().Background("#1e3a8a").Border(0.4f).BorderColor(Colors.White).PaddingVertical(5).PaddingHorizontal(2).AlignCenter().AlignMiddle().Text(title).Bold().FontSize(6).FontColor(Colors.White);
                            });

                            if (marshmallowStores.Count == 0)
                            {
                                table.Cell().ColumnSpan(9).Border(0.5f).BorderColor("#e9eff8").Padding(10).AlignCenter().Text("No Active Stores Available").FontColor(Colors.Grey.Darken2).Italic();
                            }
                            else
                            {
                                foreach (var store in marshmallowStores)
                                {
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(4).Text(store.StoreName).FontSize(7).Bold();
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(4).AlignRight().Text(store.Orders.ToString("N0")).FontSize(7);
                                    table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(4).AlignRight().Text(FormatCurrency(store.OrderValue, marshmallowCulture)).FontSize(7);
                                    foreach (var count in new[] { store.Confirmed, store.Preparing, store.Delivered, store.Pending, store.Cancelled, store.Other })
                                        table.Cell().Border(0.5f).BorderColor("#e9eff8").Padding(4).AlignRight().Text(count.ToString("N0")).FontSize(7);
                                }

                                table.Cell().Background("#f1f5f9").Border(0.5f).BorderColor("#cbd5e1").Padding(4).Text("TOTAL").Bold().FontSize(7);
                                table.Cell().Background("#f1f5f9").Border(0.5f).BorderColor("#cbd5e1").Padding(4).AlignRight().Text(marshmallowStores.Sum(store => store.Orders).ToString("N0")).Bold().FontSize(7);
                                table.Cell().Background("#f1f5f9").Border(0.5f).BorderColor("#cbd5e1").Padding(4).AlignRight().Text(FormatCurrency(marshmallowStores.Sum(store => store.OrderValue), marshmallowCulture)).Bold().FontSize(7);
                                foreach (var count in new[]
                                {
                                    marshmallowStores.Sum(store => store.Confirmed), marshmallowStores.Sum(store => store.Preparing),
                                    marshmallowStores.Sum(store => store.Delivered), marshmallowStores.Sum(store => store.Pending),
                                    marshmallowStores.Sum(store => store.Cancelled), marshmallowStores.Sum(store => store.Other)
                                })
                                    table.Cell().Background("#f1f5f9").Border(0.5f).BorderColor("#cbd5e1").Padding(4).AlignRight().Text(count.ToString("N0")).Bold().FontSize(7);
                            }
                        });
                        content.Item().PaddingTop(8).Row(row =>
                        {
                            row.Spacing(10);
                            row.RelativeItem(0.9f).Border(0.6f).BorderColor("#cbd5e1").Background(Colors.White).Padding(5).Column(chart =>
                            {
                                chart.Item().AlignCenter().Text("TODAY'S ORDER STATUS MIX").Bold().FontSize(8).FontColor("#475569");
                                chart.Item().Height(150).Image(CreateMarshmallowPieChart(marshmallowStores)).FitArea();
                            });
                            row.RelativeItem(1.5f).Border(0.6f).BorderColor("#cbd5e1").Background(Colors.White).Padding(5).Column(chart =>
                            {
                                chart.Item().AlignCenter().Text("7-DAY MARSH MALLOW ORDER TREND").Bold().FontSize(8).FontColor("#475569");
                                chart.Item().Height(150).Image(CreateMarshmallowWeeklyChart(marshmallow.WeeklyActivity, marshmallow.WeeklyHistoryAvailable)).FitArea();
                            });
                        });
                        //content.Item().PaddingTop(5).Text("Pending combines payment_pending and pending orders. Order value is the sum of total_amount for orders created on the report date.").FontSize(7).FontColor(Colors.Grey.Darken2);
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().PaddingTop(4).AlignLeft().Text("Generated by: CSSH IT Department").FontColor(Colors.Grey.Darken1).FontSize(8);
                        row.ConstantItem(250).PaddingTop(4).AlignCenter().Text("Marsh Mallow Daily Insight • System Generated Report").FontColor(Colors.Grey.Darken1).FontSize(8);
                        row.RelativeItem().PaddingTop(4).AlignRight().Text(text =>
                        {
                            text.Span("Page ").FontColor(Colors.Grey.Darken1).FontSize(8);
                            text.CurrentPageNumber().FontColor(Colors.Grey.Darken1).FontSize(8);
                        });
                    });
                });

                // =====================================================================
                // PHR: DAILY PRESCRIPTION SUMMARY (appended after existing projects)
                // =====================================================================
                container.Page(page =>
                {
                    var phr = model.PhrSummary ?? new PhrDailySummary { ReportDate = model.ReportDate.Date };
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Black).FontFamily("Helvetica"));

                    page.Header().Column(header =>
                    {
                        header.Item().Row(row =>
                        {
                            row.ConstantItem(90).Column(col =>
                            {
                                if (File.Exists(logoPath)) col.Item().Height(42).AlignCenter().Image(logoPath).FitArea();
                                col.Item().PaddingTop(1).AlignCenter().Text("CSSH IT").Bold().FontSize(8).FontColor("#475569");
                            });
                            row.RelativeItem().PaddingHorizontal(12).AlignCenter().Column(col =>
                            {
                                col.Item().AlignCenter().Text("PATIENT HEALTH RECORD").Bold().FontSize(20).FontColor("#374151");
                                col.Item().PaddingTop(3).AlignCenter().Text($"This report shows today's analytics at PHR of  {reportDateText} {reportTimeText}.").FontSize(10).FontColor(Colors.Grey.Darken2);
                        
                            //col.Item().PaddingTop(3).Row(divider =>
                            //    {
                            //        divider.RelativeItem().PaddingTop(4).LineHorizontal(0.8f).LineColor("#475569");
                            //        divider.ConstantItem(18).AlignCenter().Text("◆").FontSize(7).FontColor("#475569");
                            //        divider.RelativeItem().PaddingTop(4).LineHorizontal(0.8f).LineColor("#475569");
                            //    });
                            //    col.Item().PaddingTop(3).AlignCenter().Text($"Prescription activity for {phr.ReportDate:dd-MMM-yyyy}.").FontSize(9).FontColor(Colors.Grey.Darken2);
                            });
                            row.ConstantItem(80).AlignMiddle().Border(0.7f).BorderColor("#cbd5e1").Padding(4).Column(col =>
                            {
                                col.Item().Text("Report Generation").FontSize(6).FontColor(Colors.Grey.Darken1);
                                col.Item().Text(reportDateText).Bold().FontSize(8).FontColor("#374151");
                                col.Item().Text(reportTimeText).FontSize(7).FontColor("#374151");
                            });
                        });
                        header.Item().PaddingTop(3).LineHorizontal(1.2f).LineColor("#475569");
                    });

                    page.Content().PaddingTop(8).Column(content =>
                    {
                        content.Spacing(5);
                        content.Item().Background("#475569").PaddingVertical(4).AlignCenter()
                            .Text("PRESCRIPTION SUMMARY").Bold().FontSize(9).FontColor(Colors.White);
                        content.Item().Table(cards =>
                        {
                            cards.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });
                            cards.Cell().PaddingRight(4).Border(0.7f).BorderColor("#bae6fd").Background("#eff6ff").Padding(7).Column(card =>
                                CreateSummaryCard(card, "TOTAL PRESCRIPTIONS", phr.TotalPrescriptions.ToString("N0"), "All time", "", "#0f766e", iconPath: phrTotalPrescriptionsIconPath, iconSize: 30));
                            cards.Cell().PaddingLeft(4).Border(0.7f).BorderColor("#bbf7d0").Background("#f0fdf4").Padding(7).Column(card =>
                                CreateSummaryCard(card, "TODAY'S PRESCRIPTIONS", phr.TodayPrescriptions.ToString("N0"), $"Visit date: {phr.ReportDate:dd-MMM-yyyy}", "", "#1d4ed8", iconPath: phrTodayPrescriptionsIconPath, iconSize: 30));
                        });

                        AddPhrCountTable(content, "TODAY'S DEPARTMENT-WISE PRESCRIPTION SUMMARY", "Department", "Prescriptions",
                            (phr.Departments ?? new List<PhrDepartmentCount>()).Select(x => (x.DepartmentName, x.PrescriptionCount)));

                        content.Item().PaddingTop(4).Row(row =>
                        {
                            row.Spacing(8);
                            row.RelativeItem(0.95f).Border(0.6f).BorderColor("#cbd5e1").Background(Colors.White).Padding(4).Column(chart =>
                            {
                                chart.Item().AlignCenter().Text("MONTH-TO-DATE PRESCRIPTIONS BY DEPARTMENT").Bold().FontSize(7).FontColor("#475569");
                                chart.Item().Height(112).Image(CreatePhrDepartmentPieChart(phr.MonthlyDepartments)).FitArea();
                            });
                            row.RelativeItem(1.35f).Border(0.6f).BorderColor("#cbd5e1").Background(Colors.White).Padding(4).Column(chart =>
                            {
                                chart.Item().AlignCenter().Text("DAILY PRESCRIPTIONS — LAST 7 DAYS").Bold().FontSize(7).FontColor("#475569");
                                chart.Item().Height(112).Image(CreatePhrWeeklyChart(phr.WeeklyActivity)).FitArea();
                            });
                        });
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().PaddingTop(4).AlignLeft().Text("Generated by: CSSH IT Department").FontColor(Colors.Grey.Darken1).FontSize(8);
                        row.ConstantItem(250).PaddingTop(4).AlignCenter().Text("PHR Daily Insight • System Generated Report").FontColor(Colors.Grey.Darken1).FontSize(8);
                        row.RelativeItem().PaddingTop(4).AlignRight().Text(text =>
                        {
                            text.Span("Page ").FontColor(Colors.Grey.Darken1).FontSize(8);
                            text.CurrentPageNumber().FontColor(Colors.Grey.Darken1).FontSize(8);
                        });
                    });
                });
            }).GeneratePdf(); // <--- Chain executed properly here to emit byte[] output
        }

        // =====================================================================
        // UTILITY HELPERS DEFINITION
        // =====================================================================
        private static void AddPhrCountTable(ColumnDescriptor content, string title, string firstColumn, string countColumn, IEnumerable<(string Name, long Count)> rows)
        {
            var items = rows.ToList();
            content.Item().PaddingTop(1).Background("#475569").PaddingVertical(4).AlignCenter()
                .Text(title).Bold().FontSize(9).FontColor(Colors.White);
            content.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                });
                table.Header(header =>
                {
                    header.Cell().Background("#1e3a8a").Border(0.5f).BorderColor(Colors.White).PaddingVertical(3).PaddingHorizontal(4).Text(firstColumn).Bold().FontSize(7).FontColor(Colors.White);
                    header.Cell().Background("#1e3a8a").Border(0.5f).BorderColor(Colors.White).PaddingVertical(3).PaddingHorizontal(4).AlignRight().Text(countColumn).Bold().FontSize(7).FontColor(Colors.White);
                });
                if (items.Count == 0)
                {
                    table.Cell().ColumnSpan(2).Border(0.5f).BorderColor("#e2e8f0").Padding(5).AlignCenter()
                        .Text("No records available").Italic().FontColor(Colors.Grey.Darken2);
                    return;
                }
                foreach (var item in items)
                {
                    table.Cell().Border(0.5f).BorderColor("#e2e8f0").PaddingVertical(3).PaddingHorizontal(4).Text(item.Name).FontSize(7);
                    table.Cell().Border(0.5f).BorderColor("#e2e8f0").PaddingVertical(3).PaddingHorizontal(4).AlignRight().Text(item.Count.ToString("N0")).FontSize(7);
                }
            });
        }

        private void CreateSummaryCard(ColumnDescriptor column, string title, string value, string description, string icon, string accentColor, string iconBackground = "#e2e8f0", string? iconPath = null, byte[]? metricIconSvg = null, float iconSize = 22)
        {
            column.Item().Row(r =>
            {
                r.RelativeItem().Text(title).Bold().FontSize(8).FontColor(Colors.Grey.Darken1);
                if (!string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath))
                    r.ConstantItem(iconSize).Height(iconSize).Image(iconPath).FitArea();
                else if (metricIconSvg != null)
                    r.ConstantItem(30).Height(30).Image(metricIconSvg).FitArea();
                else if (!string.IsNullOrWhiteSpace(icon))
                    r.ConstantItem(22).Height(22).AlignCenter().AlignMiddle().Text(icon).Bold().FontSize(12).FontColor(accentColor);
            });
            column.Item().PaddingTop(2).Text(value).Bold().FontSize(12).FontColor(accentColor);
            column.Item().PaddingTop(1).Text(description).FontSize(7).FontColor(Colors.Grey.Darken2);
        }

        private void CreateOverallRevenueCard(ColumnDescriptor column, decimal created, decimal collected, decimal discount, bool discountAvailable, CultureInfo culture, string? iconPath)
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text("TODAY'S REVENUE").Bold().FontSize(8).FontColor(Colors.Grey.Darken1);
                if (!string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath))
                    row.ConstantItem(22).Height(22).Image(iconPath).FitArea();
            });

            column.Item().PaddingTop(3).Row(row =>
            {
                row.RelativeItem().Column(item =>
                {
                    item.Item().Text("CREATED").Bold().FontSize(7).FontColor("#7c3aed");
                    item.Item().PaddingTop(1).Text(FormatCurrency(created, culture)).Bold().FontSize(11).FontColor("#6d28d9");
                });
                row.RelativeItem().Column(item =>
                {
                    item.Item().Text("COLLECTED").Bold().FontSize(7).FontColor("#7c3aed");
                    item.Item().PaddingTop(1).Text(FormatCurrency(collected, culture)).Bold().FontSize(11).FontColor("#6d28d9");
                });
            });

            column.Item().PaddingTop(2).Text(discountAvailable ? $"Discount: {FormatCurrency(discount, culture)}" : "Discount detail unavailable").FontSize(6).FontColor("#7c3aed");
        }

        private string FormatCurrency(decimal amount, CultureInfo culture)
        {
            return amount.ToString("C0", culture);
        }

        private string ResolveLogoPath()
        {
            return ResolveAssetPath("logo.png");
        }

        // Round shares with the largest-remainder method so visible whole
        // percentages always add up to exactly 100%.
        private static int[] GetWholePercentages(IReadOnlyList<double> values)
        {
            var percentages = new int[values.Count];
            var total = values.Sum(value => Math.Max(0d, value));
            if (total <= 0d)
                return percentages;

            var exactShares = values.Select(value => Math.Max(0d, value) / total * 100d).ToArray();
            for (var index = 0; index < exactShares.Length; index++)
                percentages[index] = (int)Math.Floor(exactShares[index]);

            var remaining = 100 - percentages.Sum();
            foreach (var index in Enumerable.Range(0, exactShares.Length)
                         .Where(index => values[index] > 0)
                         .OrderByDescending(index => exactShares[index] - percentages[index])
                         .Take(remaining))
                percentages[index]++;

            return percentages;
        }

        private static byte[] CreateHmpkPieChart(int pending, int approved, int rejected)
        {
            const int width = 520;
            const int height = 360;
            var labels = new[] { "Pending", "Approved", "Rejected" };
            var values = new[] { Math.Max(0, pending), Math.Max(0, approved), Math.Max(0, rejected) };
            // Standard status palette: pending amber, approved green, rejected red.
            var colors = new[] { "#f59e0b", "#16a34a", "#dc2626" }.Select(SKColor.Parse).ToArray();
            var percentages = GetWholePercentages(values.Select(value => (double)value).ToArray());
            var total = values.Sum();
            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            var pie = new SKRect(20, 35, 290, 305);
            if (total == 0)
            {
                using var emptyPaint = new SKPaint { Color = SKColor.Parse("#e2e8f0"), IsAntialias = true };
                canvas.DrawCircle(155, 170, 130, emptyPaint);
            }
            else
            {
                var startAngle = -90f;
                for (var index = 0; index < values.Length; index++)
                {
                    var sweep = (float)(values[index] / (double)total * 360d);
                    using var slicePaint = new SKPaint { Color = colors[index], IsAntialias = true };
                    canvas.DrawArc(pie, startAngle, sweep, true, slicePaint);
                    startAngle += sweep;
                }
                using var separators = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3 };
                startAngle = -90f;
                foreach (var value in values)
                {
                    var sweep = (float)(value / (double)total * 360d);
                    canvas.DrawArc(pie, startAngle, sweep, true, separators);
                    startAngle += sweep;
                }
            }

            var legendIndex = 0;
            for (var index = 0; index < labels.Length; index++)
            {
                if (values[index] <= 0)
                    continue;
                var y = 78 + legendIndex++ * 88;
                using var swatch = new SKPaint { Color = colors[index], IsAntialias = true };
                canvas.DrawRoundRect(new SKRect(310, y - 18, 329, y + 1), 3, 3, swatch);
                DrawChartText(canvas, labels[index], 338, y - 2, 18, colors[index], SKTextAlign.Left, true);
                DrawChartText(canvas, $"{values[index]:N0}  ({percentages[index]}%)", 338, y + 24, 17, SKColor.Parse("#475569"));
            }

            using var image = surface.Snapshot();
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            return encoded.ToArray();
        }

        private static byte[] CreateHmpkWeeklyChart(List<HmpkDailyActivity>? dailyData, bool historyAvailable)
        {
            const int width = 960;
            const int height = 440;
            var seriesLabels = new[] { "Demands", "PO", "DMR", "Bill Payments" };
            var colors = new[] { "#1d4ed8", "#6d28d9", "#047857", "#c2410c" }.Select(SKColor.Parse).ToArray();
            var points = (dailyData ?? new List<HmpkDailyActivity>()).OrderBy(item => item.Date).TakeLast(7).ToList();
            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            if (!historyAvailable)
            {
                DrawChartText(canvas, "7-day history unavailable", width / 2f, height / 2f, 28, SKColor.Parse("#475569"), SKTextAlign.Center, true);
                using var unavailableImage = surface.Snapshot();
                using var unavailablePng = unavailableImage.Encode(SKEncodedImageFormat.Png, 100);
                return unavailablePng.ToArray();
            }
            if (points.Count == 0)
            {
                var today = DateTime.Today;
                points = Enumerable.Range(0, 7).Select(offset => new HmpkDailyActivity { Date = today.AddDays(-6 + offset) }).ToList();
            }

            const float left = 92;
            const float right = 940;
            const float top = 36;
            const float bottom = 335;
            var max = points.SelectMany(point => new[] { point.Demands, point.PurchaseOrders, point.Dmr, point.BillPayments }).DefaultIfEmpty(0).Max();
            var yMax = max <= 0 ? 4 : Math.Max(4, (int)Math.Ceiling(max / 4d) * 4);
            using var gridPaint = new SKPaint { Color = SKColor.Parse("#e2e8f0"), IsAntialias = true, StrokeWidth = 2 };
            using var todayOutline = new SKPaint { Color = SKColor.Parse("#f59e0b"), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            for (var tick = 0; tick <= 4; tick++)
            {
                var y = bottom - (bottom - top) * tick / 4f;
                canvas.DrawLine(left, y, right, y, gridPaint);
                DrawChartText(canvas, (yMax * tick / 4d).ToString("0.#"), left - 12, y + 6, 17, SKColor.Parse("#475569"), SKTextAlign.Right);
            }

            var groupWidth = (right - left) / points.Count;
            var barWidth = Math.Min(17f, groupWidth * 0.14f);
            var barGap = 3f;
            var chartHeight = bottom - top;
            for (var pointIndex = 0; pointIndex < points.Count; pointIndex++)
            {
                var point = points[pointIndex];
                var center = left + groupWidth * (pointIndex + 0.5f);
                var values = new[] { point.Demands, point.PurchaseOrders, point.Dmr, point.BillPayments };
                var totalWidth = values.Length * barWidth + (values.Length - 1) * barGap;
                for (var seriesIndex = 0; seriesIndex < values.Length; seriesIndex++)
                {
                    var x = center - totalWidth / 2 + seriesIndex * (barWidth + barGap);
                    var barHeight = Math.Max(0f, values[seriesIndex]) / yMax * chartHeight;
                    using var barPaint = new SKPaint { Color = colors[seriesIndex], IsAntialias = true };
                    canvas.DrawRoundRect(new SKRect(x, bottom - barHeight, x + barWidth, bottom), 3, 3, barPaint);
                }
                if (pointIndex == points.Count - 1)
                    canvas.DrawRoundRect(new SKRect(center - totalWidth / 2 - 7, top - 9, center + totalWidth / 2 + 7, bottom + 7), 8, 8, todayOutline);
                DrawChartText(canvas, point.Date.ToString("dd MMM", CultureInfo.InvariantCulture), center, bottom + 28, 17, SKColor.Parse("#475569"), SKTextAlign.Center);
                if (pointIndex == points.Count - 1)
                    DrawChartText(canvas, "TODAY", center, bottom + 52, 15, SKColor.Parse("#b45309"), SKTextAlign.Center, true);
            }

            var legendPositions = new[] { 175f, 335f, 475f, 595f };
            for (var index = 0; index < seriesLabels.Length; index++)
            {
                var legendX = legendPositions[index];
                using var swatch = new SKPaint { Color = colors[index], IsAntialias = true };
                canvas.DrawRoundRect(new SKRect(legendX, 402, legendX + 17, 419), 3, 3, swatch);
                DrawChartText(canvas, seriesLabels[index], legendX + 24, 417, 16, SKColor.Parse("#475569"));
            }
            canvas.DrawRoundRect(new SKRect(810, 402, 828, 419), 4, 4, todayOutline);
            DrawChartText(canvas, "Today", 835, 417, 16, SKColor.Parse("#475569"));

            using var image = surface.Snapshot();
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            return encoded.ToArray();
        }

        private static byte[] CreateSmileSyncPieChart(decimal cash, decimal online)
        {
            const int width = 520;
            const int height = 360;
            var cashColor = SKColor.Parse("#15803d");
            var onlineColor = SKColor.Parse("#0369a1");
            var total = Math.Max(0m, cash) + Math.Max(0m, online);
            var percentages = GetWholePercentages(new[] { (double)Math.Max(0m, cash), (double)Math.Max(0m, online) });
            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            using var cashPaint = new SKPaint { Color = cashColor, IsAntialias = true };
            using var onlinePaint = new SKPaint { Color = onlineColor, IsAntialias = true };
            using var emptyPaint = new SKPaint { Color = SKColor.Parse("#e2e8f0"), IsAntialias = true };
            using var separator = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3 };
            var pie = new SKRect(30, 30, 300, 300);
            if (total <= 0)
                canvas.DrawCircle(165, 165, 130, emptyPaint);
            else
            {
                var cashSweep = (float)((double)(Math.Max(0m, cash) / total) * 360d);
                canvas.DrawArc(pie, -90, cashSweep, true, cashPaint);
                canvas.DrawArc(pie, -90 + cashSweep, 360 - cashSweep, true, onlinePaint);
                canvas.DrawArc(pie, -90, cashSweep, true, separator);
            }

            var culture = CultureInfo.GetCultureInfo("en-IN");
            var values = new[] { Math.Max(0m, cash), Math.Max(0m, online) };
            var labels = new[] { "Cash", "Online" };
            var colors = new[] { cashColor, onlineColor };
            var visibleIndex = 0;
            for (var index = 0; index < values.Length; index++)
            {
                if (values[index] <= 0)
                    continue;
                var y = 86 + visibleIndex++ * 119;
                DrawChartText(canvas, labels[index], 330, y, 24, colors[index], SKTextAlign.Left, true);
                DrawChartText(canvas, values[index].ToString("C0", culture), 330, y + 30, 21, SKColor.Parse("#334155"));
                DrawChartText(canvas, $"{percentages[index]}%", 330, y + 57, 17, SKColor.Parse("#64748b"));
            }

            using var image = surface.Snapshot();
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            return encoded.ToArray();
        }

        private static byte[] CreateSmileSyncWeeklyChart(List<SmileSyncDailyCollection>? dailyData, bool historyAvailable)
        {
            const int width = 960;
            const int height = 440;
            var cashColor = SKColor.Parse("#15803d");
            var onlineColor = SKColor.Parse("#0369a1");
            var gridColor = SKColor.Parse("#e2e8f0");
            var textColor = SKColor.Parse("#475569");
            var points = (dailyData ?? new List<SmileSyncDailyCollection>()).OrderBy(item => item.Date).TakeLast(7).ToList();
            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            if (!historyAvailable)
            {
                DrawChartText(canvas, "7-day history unavailable", width / 2f, height / 2f, 28, textColor, SKTextAlign.Center, true);
                using var unavailableImage = surface.Snapshot();
                using var unavailablePng = unavailableImage.Encode(SKEncodedImageFormat.Png, 100);
                return unavailablePng.ToArray();
            }
            if (points.Count == 0)
            {
                var today = DateTime.Today;
                points = Enumerable.Range(0, 7).Select(offset => new SmileSyncDailyCollection { Date = today.AddDays(-6 + offset) }).ToList();
            }

            const float left = 92;
            const float right = 940;
            const float top = 36;
            const float bottom = 335;
            var max = points.SelectMany(point => new[] { point.Cash, point.Online }).DefaultIfEmpty(0m).Max();
            var yMax = max <= 0 ? 1m : decimal.Ceiling(max / 4m) * 4m;
            if (yMax <= 0) yMax = max;
            using var gridPaint = new SKPaint { Color = gridColor, IsAntialias = true, StrokeWidth = 2 };
            using var cashPaint = new SKPaint { Color = cashColor, IsAntialias = true };
            using var onlinePaint = new SKPaint { Color = onlineColor, IsAntialias = true };
            using var todayOutline = new SKPaint { Color = SKColor.Parse("#f59e0b"), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            for (var tick = 0; tick <= 4; tick++)
            {
                var y = bottom - (bottom - top) * tick / 4f;
                canvas.DrawLine(left, y, right, y, gridPaint);
                DrawChartText(canvas, (yMax * tick / 4m).ToString("0.#"), left - 12, y + 6, 17, textColor, SKTextAlign.Right);
            }

            var groupWidth = (right - left) / points.Count;
            var barWidth = Math.Min(35f, groupWidth * 0.29f);
            var chartHeight = bottom - top;
            for (var index = 0; index < points.Count; index++)
            {
                var point = points[index];
                var center = left + groupWidth * (index + 0.5f);
                var cashHeight = (float)(Math.Max(0m, point.Cash) / yMax) * chartHeight;
                var onlineHeight = (float)(Math.Max(0m, point.Online) / yMax) * chartHeight;
                canvas.DrawRoundRect(new SKRect(center - barWidth - 3, bottom - cashHeight, center - 3, bottom), 4, 4, cashPaint);
                canvas.DrawRoundRect(new SKRect(center + 3, bottom - onlineHeight, center + barWidth + 3, bottom), 4, 4, onlinePaint);
                if (index == points.Count - 1)
                    canvas.DrawRoundRect(new SKRect(center - barWidth - 7, top - 9, center + barWidth + 7, bottom + 7), 8, 8, todayOutline);
                DrawChartText(canvas, point.Date.ToString("dd MMM", CultureInfo.InvariantCulture), center, bottom + 28, 17, textColor, SKTextAlign.Center);
                if (index == points.Count - 1)
                    DrawChartText(canvas, "TODAY", center, bottom + 52, 15, SKColor.Parse("#b45309"), SKTextAlign.Center, true);
            }

            canvas.DrawRoundRect(new SKRect(310, 402, 328, 420), 3, 3, cashPaint);
            DrawChartText(canvas, "Cash", 336, 418, 18, textColor);
            canvas.DrawRoundRect(new SKRect(430, 402, 448, 420), 3, 3, onlinePaint);
            DrawChartText(canvas, "Online", 456, 418, 18, textColor);
            canvas.DrawRoundRect(new SKRect(572, 402, 590, 420), 3, 3, todayOutline);
            DrawChartText(canvas, "Today", 598, 418, 18, textColor);
            using var image = surface.Snapshot();
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            return encoded.ToArray();
        }

        private static void DrawChartText(SKCanvas canvas, string text, float x, float y, float size, SKColor color, SKTextAlign alignment = SKTextAlign.Left, bool bold = false)
        {
            using var paint = new SKPaint
            {
                Color = color,
                IsAntialias = true,
                TextSize = size,
                TextAlign = alignment,
                Typeface = SKTypeface.FromFamilyName("Arial", bold ? SKFontStyle.Bold : SKFontStyle.Normal)
            };
            canvas.DrawText(text, x, y, paint);
        }

        private static byte[] CreatePeonBookPieChart(PeonBookDailySummary summary)
        {
            const int width = 520;
            const int height = 360;
            // TodayCreated is the overall row count, not a mutually-exclusive
            // status. Only status buckets belong in a pie to avoid double-counting.
            var labels = new[] { "Pending", "Delivered", "Received", "Inactive" };
            var values = new[] { summary.TodayPending, summary.TodayDelivered, summary.TodayReceived, summary.TodayInactive }
                .Select(value => Math.Max(0, value)).ToArray();
            var colors = new[] { "#f59e0b", "#b45309", "#047857", "#64748b" }.Select(SKColor.Parse).ToArray();
            var total = values.Sum();
            var percentages = GetWholePercentages(values.Select(value => (double)value).ToArray());
            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            var pie = new SKRect(24, 30, 294, 300);
            if (total == 0)
            {
                using var emptyPaint = new SKPaint { Color = SKColor.Parse("#e2e8f0"), IsAntialias = true };
                canvas.DrawCircle(159, 165, 130, emptyPaint);
            }
            else
            {
                var startAngle = -90f;
                for (var index = 0; index < values.Length; index++)
                {
                    var sweep = values[index] / (float)total * 360f;
                    using var slicePaint = new SKPaint { Color = colors[index], IsAntialias = true };
                    canvas.DrawArc(pie, startAngle, sweep, true, slicePaint);
                    startAngle += sweep;
                }
                using var separator = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3 };
                startAngle = -90f;
                foreach (var value in values)
                {
                    var sweep = value / (float)total * 360f;
                    canvas.DrawArc(pie, startAngle, sweep, true, separator);
                    startAngle += sweep;
                }
            }

            var legendIndex = 0;
            for (var index = 0; index < labels.Length; index++)
            {
                if (values[index] <= 0)
                    continue;
                var y = 67 + legendIndex++ * 68;
                using var swatch = new SKPaint { Color = colors[index], IsAntialias = true };
                canvas.DrawRoundRect(new SKRect(310, y - 15, 329, y + 4), 3, 3, swatch);
                DrawChartText(canvas, labels[index], 338, y, 17, colors[index], SKTextAlign.Left, true);
                DrawChartText(canvas, $"{values[index]:N0} ({percentages[index]}%)", 338, y + 23, 15, SKColor.Parse("#475569"));
            }

            using var image = surface.Snapshot();
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            return encoded.ToArray();
        }

        private static byte[] CreatePeonBookWeeklyChart(List<PeonBookDailyActivity>? dailyData, bool historyAvailable)
        {
            const int width = 960;
            const int height = 440;
            var labels = new[] { "Created", "Delivered", "Received", "Inactive", "Pending" };
            var colors = new[] { "#1d4ed8", "#b45309", "#047857", "#64748b", "#b91c1c" }.Select(SKColor.Parse).ToArray();
            var points = (dailyData ?? new List<PeonBookDailyActivity>()).OrderBy(item => item.Date).TakeLast(7).ToList();
            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            if (!historyAvailable)
            {
                DrawChartText(canvas, "7-day history unavailable", width / 2f, height / 2f, 28, SKColor.Parse("#475569"), SKTextAlign.Center, true);
                using var unavailableImage = surface.Snapshot();
                using var unavailablePng = unavailableImage.Encode(SKEncodedImageFormat.Png, 100);
                return unavailablePng.ToArray();
            }
            if (points.Count == 0)
            {
                var today = DateTime.Today;
                points = Enumerable.Range(0, 7).Select(offset => new PeonBookDailyActivity { Date = today.AddDays(-6 + offset) }).ToList();
            }

            const float left = 92;
            const float right = 940;
            const float top = 36;
            const float bottom = 335;
            var max = points.SelectMany(point => new[] { point.Created, point.Delivered, point.Received, point.Inactive, point.Pending }).DefaultIfEmpty(0).Max();
            var yMax = max <= 0 ? 4 : Math.Max(4, (int)Math.Ceiling(max / 4d) * 4);
            using var gridPaint = new SKPaint { Color = SKColor.Parse("#e2e8f0"), IsAntialias = true, StrokeWidth = 2 };
            using var todayOutline = new SKPaint { Color = SKColor.Parse("#f59e0b"), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            for (var tick = 0; tick <= 4; tick++)
            {
                var y = bottom - (bottom - top) * tick / 4f;
                canvas.DrawLine(left, y, right, y, gridPaint);
                DrawChartText(canvas, (yMax * tick / 4d).ToString("0.#"), left - 12, y + 6, 17, SKColor.Parse("#475569"), SKTextAlign.Right);
            }

            var groupWidth = (right - left) / points.Count;
            var barWidth = Math.Min(13f, groupWidth * 0.11f);
            const float barGap = 2f;
            var chartHeight = bottom - top;
            for (var pointIndex = 0; pointIndex < points.Count; pointIndex++)
            {
                var point = points[pointIndex];
                var center = left + groupWidth * (pointIndex + 0.5f);
                var values = new[] { point.Created, point.Delivered, point.Received, point.Inactive, point.Pending };
                var totalWidth = values.Length * barWidth + (values.Length - 1) * barGap;
                for (var seriesIndex = 0; seriesIndex < values.Length; seriesIndex++)
                {
                    var x = center - totalWidth / 2 + seriesIndex * (barWidth + barGap);
                    var barHeight = Math.Max(0f, values[seriesIndex]) / yMax * chartHeight;
                    using var barPaint = new SKPaint { Color = colors[seriesIndex], IsAntialias = true };
                    canvas.DrawRoundRect(new SKRect(x, bottom - barHeight, x + barWidth, bottom), 3, 3, barPaint);
                }
                if (pointIndex == points.Count - 1)
                    canvas.DrawRoundRect(new SKRect(center - totalWidth / 2 - 7, top - 9, center + totalWidth / 2 + 7, bottom + 7), 8, 8, todayOutline);
                DrawChartText(canvas, point.Date.ToString("dd MMM", CultureInfo.InvariantCulture), center, bottom + 28, 17, SKColor.Parse("#475569"), SKTextAlign.Center);
                if (pointIndex == points.Count - 1)
                    DrawChartText(canvas, "TODAY", center, bottom + 52, 15, SKColor.Parse("#b45309"), SKTextAlign.Center, true);
            }

            var legendPositions = new[] { 120f, 265f, 420f, 560f, 690f };
            for (var index = 0; index < labels.Length; index++)
            {
                using var swatch = new SKPaint { Color = colors[index], IsAntialias = true };
                canvas.DrawRoundRect(new SKRect(legendPositions[index], 402, legendPositions[index] + 16, 418), 3, 3, swatch);
                DrawChartText(canvas, labels[index], legendPositions[index] + 22, 416, 15, SKColor.Parse("#475569"));
            }
            canvas.DrawRoundRect(new SKRect(810, 402, 828, 419), 4, 4, todayOutline);
            DrawChartText(canvas, "Today", 835, 417, 15, SKColor.Parse("#475569"));

            using var image = surface.Snapshot();
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            return encoded.ToArray();
        }

        private static void AddPeonBookCard(RowDescriptor row, int iconType, string title, int value, string description, string background, string accent)
        {
            row.RelativeItem().Background(background).Border(0.6f).BorderColor("#cbd5e1").Padding(8).Column(card =>
            {
                card.Item().Row(r =>
                {
                    r.RelativeItem().Text(title).Bold().FontSize(7).FontColor(accent);
                    r.ConstantItem(22).Height(22).Image(CreatePeonBookIcon(iconType, accent, "#60a5fa")).FitArea();
                });
                card.Item().PaddingTop(3).Text(value.ToString("N0")).Bold().FontSize(16).FontColor(accent);
                card.Item().PaddingTop(2).Text(description).FontSize(6).FontColor("#64748b");
            });
        }

        private static void AddPeonBookRow(TableDescriptor table, int iconType, string label, int value, string background, string accent)
        {
            table.Cell().Border(0.5f).BorderColor("#cbd5e1").Background(background).Padding(4).Row(row =>
            {
                row.ConstantItem(23).Height(16).PaddingRight(7).AlignMiddle().AlignCenter().Image(CreatePeonBookIcon(iconType, accent, "#60a5fa")).FitArea();
                row.RelativeItem().AlignMiddle().Text(label).Bold();
            });
            table.Cell().Border(0.5f).BorderColor("#cbd5e1").Padding(7).AlignRight().Text(value.ToString("N0")).Bold();
        }

        private static byte[] CreateMrdPieChart(MrdDailySummary summary)
        {
            const int width = 520;
            const int height = 330;
            var labels = new[] { "Verified", "Pending" };
            var values = new[] { Math.Max(0, summary.TodayVerifiedRecords), Math.Max(0, summary.TodayPendingVerification) };
            var colors = new[] { SKColor.Parse("#059669"), SKColor.Parse("#f59e0b") };
            var total = values.Sum();
            var percentages = GetWholePercentages(values.Select(value => (double)value).ToArray());
            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            var pie = new SKRect(25, 30, 275, 280);
            if (total == 0)
            {
                using var emptyPaint = new SKPaint { Color = SKColor.Parse("#e2e8f0"), IsAntialias = true };
                canvas.DrawCircle(150, 155, 120, emptyPaint);
            }
            else
            {
                var verifiedSweep = values[0] / (float)total * 360f;
                using var verifiedPaint = new SKPaint { Color = colors[0], IsAntialias = true };
                using var pendingPaint = new SKPaint { Color = colors[1], IsAntialias = true };
                using var separator = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3 };
                canvas.DrawArc(pie, -90, verifiedSweep, true, verifiedPaint);
                canvas.DrawArc(pie, -90 + verifiedSweep, 360 - verifiedSweep, true, pendingPaint);
                canvas.DrawArc(pie, -90, verifiedSweep, true, separator);
            }

            var legendIndex = 0;
            for (var index = 0; index < labels.Length; index++)
            {
                if (values[index] <= 0)
                    continue;
                var y = 112 + legendIndex++ * 92;
                using var swatch = new SKPaint { Color = colors[index], IsAntialias = true };
                canvas.DrawRoundRect(new SKRect(300, y - 18, 320, y + 2), 3, 3, swatch);
                DrawChartText(canvas, labels[index], 332, y, 22, colors[index], SKTextAlign.Left, true);
                DrawChartText(canvas, $"{values[index]:N0} ({percentages[index]}%)", 332, y + 28, 18, SKColor.Parse("#475569"));
            }

            using var image = surface.Snapshot();
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            return encoded.ToArray();
        }

        private static byte[] CreateMrdWeeklyChart(List<MrdDailyActivity>? dailyData, bool historyAvailable)
        {
            const int width = 960;
            const int height = 440;
            var labels = new[] { "Patients", "Uploads", "Admissions", "Discharges" };
            var colors = new[] { "#6d28d9", "#1d4ed8", "#b45309", "#c2410c" }.Select(SKColor.Parse).ToArray();
            var points = (dailyData ?? new List<MrdDailyActivity>()).OrderBy(item => item.Date).TakeLast(7).ToList();
            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            if (!historyAvailable)
            {
                DrawChartText(canvas, "7-day history unavailable", width / 2f, height / 2f, 28, SKColor.Parse("#475569"), SKTextAlign.Center, true);
                using var unavailableImage = surface.Snapshot();
                using var unavailablePng = unavailableImage.Encode(SKEncodedImageFormat.Png, 100);
                return unavailablePng.ToArray();
            }
            if (points.Count == 0)
            {
                var today = DateTime.Today;
                points = Enumerable.Range(0, 7).Select(offset => new MrdDailyActivity { Date = today.AddDays(-6 + offset) }).ToList();
            }

            const float left = 92;
            const float right = 940;
            const float top = 36;
            const float bottom = 335;
            var max = points.SelectMany(point => new[] { point.PatientRecords, point.UploadedFiles, point.Admissions, point.Discharges }).DefaultIfEmpty(0).Max();
            var yMax = max <= 0 ? 4 : Math.Max(4, (int)Math.Ceiling(max / 4d) * 4);
            using var gridPaint = new SKPaint { Color = SKColor.Parse("#e2e8f0"), IsAntialias = true, StrokeWidth = 2 };
            using var todayOutline = new SKPaint { Color = SKColor.Parse("#f59e0b"), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            for (var tick = 0; tick <= 4; tick++)
            {
                var y = bottom - (bottom - top) * tick / 4f;
                canvas.DrawLine(left, y, right, y, gridPaint);
                DrawChartText(canvas, (yMax * tick / 4d).ToString("0.#"), left - 12, y + 6, 17, SKColor.Parse("#475569"), SKTextAlign.Right);
            }

            var groupWidth = (right - left) / points.Count;
            var barWidth = Math.Min(17f, groupWidth * 0.14f);
            const float barGap = 3f;
            var chartHeight = bottom - top;
            for (var pointIndex = 0; pointIndex < points.Count; pointIndex++)
            {
                var point = points[pointIndex];
                var center = left + groupWidth * (pointIndex + 0.5f);
                var values = new[] { point.PatientRecords, point.UploadedFiles, point.Admissions, point.Discharges };
                var totalWidth = values.Length * barWidth + (values.Length - 1) * barGap;
                for (var seriesIndex = 0; seriesIndex < values.Length; seriesIndex++)
                {
                    var x = center - totalWidth / 2 + seriesIndex * (barWidth + barGap);
                    var barHeight = Math.Max(0f, values[seriesIndex]) / yMax * chartHeight;
                    using var barPaint = new SKPaint { Color = colors[seriesIndex], IsAntialias = true };
                    canvas.DrawRoundRect(new SKRect(x, bottom - barHeight, x + barWidth, bottom), 3, 3, barPaint);
                }
                if (pointIndex == points.Count - 1)
                    canvas.DrawRoundRect(new SKRect(center - totalWidth / 2 - 7, top - 9, center + totalWidth / 2 + 7, bottom + 7), 8, 8, todayOutline);
                DrawChartText(canvas, point.Date.ToString("dd MMM", CultureInfo.InvariantCulture), center, bottom + 28, 17, SKColor.Parse("#475569"), SKTextAlign.Center);
                if (pointIndex == points.Count - 1)
                    DrawChartText(canvas, "TODAY", center, bottom + 52, 15, SKColor.Parse("#b45309"), SKTextAlign.Center, true);
            }

            var legendPositions = new[] { 190f, 330f, 460f, 610f };
            for (var index = 0; index < labels.Length; index++)
            {
                using var swatch = new SKPaint { Color = colors[index], IsAntialias = true };
                canvas.DrawRoundRect(new SKRect(legendPositions[index], 402, legendPositions[index] + 17, 419), 3, 3, swatch);
                DrawChartText(canvas, labels[index], legendPositions[index] + 24, 417, 16, SKColor.Parse("#475569"));
            }
            canvas.DrawRoundRect(new SKRect(810, 402, 828, 419), 4, 4, todayOutline);
            DrawChartText(canvas, "Today", 835, 417, 16, SKColor.Parse("#475569"));

            using var image = surface.Snapshot();
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            return encoded.ToArray();
        }

        private static byte[] CreateMarshmallowIcon(int type, string accent, string highlight)
        {
            using var surface = SKSurface.Create(new SKImageInfo(96, 96, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            var primary = SKColor.Parse(accent);
            var secondary = SKColor.Parse(highlight);
            using var stroke = new SKPaint { Color = primary, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 5, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round };
            using var thin = new SKPaint { Color = primary, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round };
            using var highlightStroke = new SKPaint { Color = secondary, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 5, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round };
            using var highlightFill = new SKPaint { Color = secondary, IsAntialias = true };

            if (type == 0)
            {
                // Storefront with striped awning and entrance.
                using var roof = new SKPath();
                roof.MoveTo(14, 35); roof.LineTo(22, 18); roof.LineTo(74, 18); roof.LineTo(82, 35);
                canvas.DrawPath(roof, stroke);
                canvas.DrawLine(15, 39, 81, 39, stroke);
                canvas.DrawRoundRect(new SKRect(18, 39, 78, 82), 2, 2, stroke);
                canvas.DrawLine(20, 35, 76, 35, thin);
                for (var panel = 0; panel < 5; panel++)
                {
                    var left = 20 + panel * 11;
                    using var awning = new SKPath();
                    awning.MoveTo(left, 35); awning.LineTo(left + 1, 44); awning.QuadTo(left + 5, 50, left + 9, 44); awning.LineTo(left + 10, 35);
                    canvas.DrawPath(awning, panel % 2 == 0 ? highlightStroke : thin);
                }
                canvas.DrawRoundRect(new SKRect(28, 55, 45, 81), 2, 2, thin);
                canvas.DrawRoundRect(new SKRect(53, 54, 71, 68), 2, 2, thin);
                canvas.DrawLine(57, 59, 67, 59, highlightStroke);
                canvas.DrawLine(57, 64, 64, 64, highlightStroke);
            }
            else if (type == 1)
            {
                // Shopping cart with a confirmed-order check mark.
                using var cart = new SKPath();
                cart.MoveTo(12, 20); cart.LineTo(23, 20); cart.LineTo(31, 60); cart.LineTo(70, 60);
                cart.LineTo(79, 32); cart.LineTo(28, 32);
                canvas.DrawPath(cart, stroke);
                canvas.DrawLine(34, 42, 73, 42, thin);
                canvas.DrawLine(36, 51, 70, 51, thin);
                canvas.DrawCircle(39, 73, 6, stroke);
                canvas.DrawCircle(66, 73, 6, stroke);
                canvas.DrawCircle(74, 23, 13, highlightFill);
                canvas.DrawCircle(74, 23, 13, stroke);
                using var check = new SKPath();
                check.MoveTo(68, 23); check.LineTo(72, 27); check.LineTo(80, 18);
                canvas.DrawPath(check, new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round });
            }
            else if (type == 2)
            {
                // Counter terminal and payment drawer.
                canvas.DrawRoundRect(new SKRect(20, 18, 76, 58), 5, 5, stroke);
                canvas.DrawRoundRect(new SKRect(27, 25, 69, 48), 3, 3, thin);
                using var rupeePaint = new SKPaint { Color = secondary, IsAntialias = true, TextSize = 20, Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) };
                canvas.DrawText("₹", 39, 43, rupeePaint);
                canvas.DrawLine(48, 58, 48, 68, stroke);
                canvas.DrawLine(36, 68, 60, 68, stroke);
                canvas.DrawRoundRect(new SKRect(15, 72, 81, 86), 3, 3, stroke);
                canvas.DrawLine(19, 77, 77, 77, thin);
                canvas.DrawRoundRect(new SKRect(67, 79, 74, 83), 1, 1, highlightFill);
            }
            else
            {
                // Delivery van with parcel and wheels.
                canvas.DrawRoundRect(new SKRect(10, 28, 59, 65), 4, 4, stroke);
                using var cab = new SKPath();
                cab.MoveTo(59, 39); cab.LineTo(72, 39); cab.LineTo(85, 52); cab.LineTo(85, 65); cab.LineTo(59, 65); cab.Close();
                canvas.DrawPath(cab, stroke);
                canvas.DrawLine(64, 43, 71, 43, highlightStroke);
                canvas.DrawLine(71, 43, 80, 52, highlightStroke);
                canvas.DrawCircle(29, 69, 8, stroke);
                canvas.DrawCircle(72, 69, 8, stroke);
                canvas.DrawRoundRect(new SKRect(25, 36, 46, 56), 2, 2, thin);
                canvas.DrawLine(35, 40, 35, 52, highlightStroke);
                canvas.DrawLine(29, 46, 41, 46, highlightStroke);
            }

            using var image = surface.Snapshot();
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            return encoded.ToArray();
        }

        private static byte[] CreateMarshmallowLifetimeStorePieChart(IReadOnlyList<MarshmallowStoreTotalSummary> stores)
        {
            const int width = 600;
            const int height = 390;
            var points = stores.Where(store => store.Orders > 0).OrderByDescending(store => store.Orders).ToList();
            var colors = new[] { "#2563eb", "#059669", "#7c3aed", "#f59e0b", "#0f766e", "#db2777", "#64748b" }.Select(SKColor.Parse).ToArray();
            var values = points.Select(store => store.Orders).ToArray();
            var total = values.Sum();
            var percentages = GetWholePercentages(values.Select(value => (double)value).ToArray());
            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            var pie = new SKRect(12, 62, 272, 322);
            if (total == 0)
            {
                using var emptyPaint = new SKPaint { Color = SKColor.Parse("#e2e8f0"), IsAntialias = true };
                canvas.DrawCircle(142, 192, 126, emptyPaint);
                DrawChartText(canvas, "No orders recorded", 420, 197, 20, SKColor.Parse("#64748b"), SKTextAlign.Center);
            }
            else
            {
                var startAngle = -90f;
                for (var index = 0; index < points.Count; index++)
                {
                    var sweep = values[index] / (float)total * 360f;
                    using var slice = new SKPaint { Color = colors[index % colors.Length], IsAntialias = true };
                    using var separator = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
                    canvas.DrawArc(pie, startAngle, sweep, true, slice);
                    canvas.DrawArc(pie, startAngle, sweep, true, separator);
                    startAngle += sweep;
                }

                for (var index = 0; index < points.Count; index++)
                {
                    var y = 53 + index * 45;
                    using var swatch = new SKPaint { Color = colors[index % colors.Length], IsAntialias = true };
                    canvas.DrawRoundRect(new SKRect(292, y - 13, 307, y + 2), 3, 3, swatch);
                    DrawChartText(canvas, points[index].StoreName, 317, y, 15, colors[index % colors.Length], SKTextAlign.Left, true);
                    DrawChartText(canvas, $"{values[index]:N0} orders  •  {percentages[index]}%", 317, y + 19, 13, SKColor.Parse("#475569"));
                }
            }

            using var image = surface.Snapshot();
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            return encoded.ToArray();
        }

        private static byte[] CreatePhrDepartmentPieChart(IReadOnlyList<PhrDepartmentCount>? departments)
        {
            const int width = 620;
            const int height = 280;
            var points = (departments ?? Array.Empty<PhrDepartmentCount>())
                .Where(item => item.PrescriptionCount > 0)
                .OrderByDescending(item => item.PrescriptionCount)
                .ToList();
            var values = points.Select(item => item.PrescriptionCount).ToArray();
            var total = values.Sum();
            var percentages = GetWholePercentages(values.Select(value => (double)value).ToArray());
            var colors = new[] { "#2563eb", "#059669", "#7c3aed", "#f59e0b", "#0f766e", "#db2777", "#64748b", "#0891b2", "#ea580c", "#4f46e5" }
                .Select(SKColor.Parse).ToArray();

            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            var pie = new SKRect(8, 28, 232, 252);
            if (total == 0)
            {
                using var emptyPaint = new SKPaint { Color = SKColor.Parse("#e2e8f0"), IsAntialias = true };
                canvas.DrawCircle(120, 140, 108, emptyPaint);
                DrawChartText(canvas, "No monthly data", 420, 144, 18, SKColor.Parse("#64748b"), SKTextAlign.Center);
            }
            else
            {
                var startAngle = -90f;
                for (var index = 0; index < points.Count; index++)
                {
                    var sweep = values[index] / (float)total * 360f;
                    using var slice = new SKPaint { Color = colors[index % colors.Length], IsAntialias = true };
                    using var separator = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
                    canvas.DrawArc(pie, startAngle, sweep, true, slice);
                    canvas.DrawArc(pie, startAngle, sweep, true, separator);
                    startAngle += sweep;
                }

                for (var index = 0; index < points.Count; index++)
                {
                    var y = 22 + index * 25;
                    if (y > height - 8) break;
                    using var swatch = new SKPaint { Color = colors[index % colors.Length], IsAntialias = true };
                    canvas.DrawRoundRect(new SKRect(250, y - 10, 263, y + 3), 3, 3, swatch);
                    DrawChartText(canvas, $"{points[index].DepartmentName}  {values[index]:N0} ({percentages[index]}%)", 270, y, 12, SKColor.Parse("#334155"));
                }
            }

            using var image = surface.Snapshot();
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            return encoded.ToArray();
        }

        private static byte[] CreatePhrWeeklyChart(IReadOnlyList<PhrDailyActivity>? dailyData)
        {
            const int width = 760;
            const int height = 280;
            var points = (dailyData ?? Array.Empty<PhrDailyActivity>()).OrderBy(item => item.Date).TakeLast(7).ToList();
            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            if (points.Count == 0)
            {
                DrawChartText(canvas, "7-day history unavailable", width / 2f, height / 2f, 18, SKColor.Parse("#64748b"), SKTextAlign.Center);
            }
            else
            {
                const float left = 48;
                const float right = 742;
                const float top = 24;
                const float bottom = 216;
                var max = points.Max(item => item.PrescriptionCount);
                var yMax = max <= 0 ? 4L : Math.Max(4L, (long)Math.Ceiling(max / 4d) * 4L);
                using var gridPaint = new SKPaint { Color = SKColor.Parse("#e2e8f0"), IsAntialias = true, StrokeWidth = 1.5f };
                for (var tick = 0; tick <= 4; tick++)
                {
                    var y = bottom - (bottom - top) * tick / 4f;
                    canvas.DrawLine(left, y, right, y, gridPaint);
                    DrawChartText(canvas, (yMax * tick / 4d).ToString("0.#"), left - 8, y + 5, 12, SKColor.Parse("#64748b"), SKTextAlign.Right);
                }

                var step = (right - left) / points.Count;
                var barWidth = Math.Min(44f, step * 0.48f);
                var chartHeight = bottom - top;
                for (var index = 0; index < points.Count; index++)
                {
                    var point = points[index];
                    var center = left + step * (index + 0.5f);
                    var barHeight = point.PrescriptionCount / (float)yMax * chartHeight;
                    using var bar = new SKPaint { Color = index == points.Count - 1 ? SKColor.Parse("#7c3aed") : SKColor.Parse("#2563eb"), IsAntialias = true };
                    canvas.DrawRoundRect(new SKRect(center - barWidth / 2, bottom - barHeight, center + barWidth / 2, bottom), 4, 4, bar);
                    DrawChartText(canvas, point.PrescriptionCount.ToString("N0"), center, Math.Max(top + 12, bottom - barHeight - 5), 12, SKColor.Parse("#334155"), SKTextAlign.Center);
                    DrawChartText(canvas, point.Date.ToString("dd MMM", CultureInfo.InvariantCulture), center, bottom + 22, 12, SKColor.Parse("#475569"), SKTextAlign.Center);
                }
            }

            using var chartImage = surface.Snapshot();
            using var chartPng = chartImage.Encode(SKEncodedImageFormat.Png, 100);
            return chartPng.ToArray();
        }

        private static byte[] CreateMarshmallowLifetimeStoreBarChart(IReadOnlyList<MarshmallowStoreTotalSummary> stores)
        {
            const int width = 960;
            const int height = 390;
            var points = stores.OrderByDescending(store => store.OrderValue).ToList();
            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            if (points.Count == 0)
            {
                DrawChartText(canvas, "No store totals available", width / 2f, height / 2f, 22, SKColor.Parse("#64748b"), SKTextAlign.Center);
                using var emptyImage = surface.Snapshot();
                using var emptyPng = emptyImage.Encode(SKEncodedImageFormat.Png, 100);
                return emptyPng.ToArray();
            }

            const float left = 260;
            const float right = 830;
            const float top = 38;
            const float bottom = 345;
            var max = points.Max(store => store.OrderValue);
            var step = max <= 0 ? 1m : (decimal)Math.Ceiling((double)max / 4d);
            var axisMax = step * 4m;
            using var gridPaint = new SKPaint { Color = SKColor.Parse("#e2e8f0"), IsAntialias = true, StrokeWidth = 1.5f };
            for (var tick = 0; tick <= 4; tick++)
            {
                var x = left + (right - left) * tick / 4f;
                canvas.DrawLine(x, top, x, bottom, gridPaint);
                var tickValue = axisMax * tick / 4m;
                var tickLabel = tickValue >= 1000m ? $"₹{tickValue / 1000m:0.#}k" : $"₹{tickValue:0}";
                DrawChartText(canvas, tickLabel, x, bottom + 22, 13, SKColor.Parse("#64748b"), SKTextAlign.Center);
            }

            var rowHeight = (bottom - top) / points.Count;
            var barHeight = Math.Min(21f, rowHeight * 0.52f);
            var culture = CultureInfo.GetCultureInfo("en-IN");
            for (var index = 0; index < points.Count; index++)
            {
                var store = points[index];
                var centerY = top + rowHeight * (index + 0.5f);
                DrawChartText(canvas, store.StoreName, left - 12, centerY + 5, 14, SKColor.Parse("#334155"), SKTextAlign.Right);
                var barWidth = (float)(Math.Max(0m, store.OrderValue) / axisMax) * (right - left);
                using var barPaint = new SKPaint
                {
                    Color = index == 0 ? SKColor.Parse("#7c3aed") : SKColor.Parse("#2563eb"),
                    IsAntialias = true
                };
                canvas.DrawRoundRect(new SKRect(left, centerY - barHeight / 2, left + barWidth, centerY + barHeight / 2), 4, 4, barPaint);
                var labelX = Math.Min(left + barWidth + 8, width - 9);
                DrawChartText(canvas, $"₹{store.OrderValue.ToString("N0", culture)}", labelX, centerY + 5, 13, SKColor.Parse("#334155"));
            }

            using var image = surface.Snapshot();
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            return encoded.ToArray();
        }

        private static byte[] CreateMarshmallowPieChart(IReadOnlyList<MarshmallowStoreDailySummary> stores)
        {
            const int width = 520;
            const int height = 340;
            var labels = new[] { "Confirmed", "Preparing", "Delivered", "Pending", "Cancelled", "Other" };
            var values = new[]
            {
                stores.Sum(store => store.Confirmed), stores.Sum(store => store.Preparing),
                stores.Sum(store => store.Delivered), stores.Sum(store => store.Pending),
                stores.Sum(store => store.Cancelled), stores.Sum(store => store.Other)
            };
            var colors = new[] { "#2563eb", "#7c3aed", "#059669", "#f59e0b", "#dc2626", "#64748b" }.Select(SKColor.Parse).ToArray();
            var total = values.Sum();
            var percentages = GetWholePercentages(values.Select(value => (double)value).ToArray());
            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            var pie = new SKRect(12, 38, 258, 284);
            if (total == 0)
            {
                using var emptyPaint = new SKPaint { Color = SKColor.Parse("#e2e8f0"), IsAntialias = true };
                canvas.DrawCircle(135, 161, 118, emptyPaint);
                DrawChartText(canvas, "No orders today", 345, 165, 19, SKColor.Parse("#64748b"), SKTextAlign.Center);
            }
            else
            {
                var startAngle = -90f;
                for (var index = 0; index < values.Length; index++)
                {
                    if (values[index] <= 0) continue;
                    var sweep = values[index] / (float)total * 360f;
                    using var slice = new SKPaint { Color = colors[index], IsAntialias = true };
                    using var separator = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
                    canvas.DrawArc(pie, startAngle, sweep, true, slice);
                    canvas.DrawArc(pie, startAngle, sweep, true, separator);
                    startAngle += sweep;
                }

                var legendIndex = 0;
                for (var index = 0; index < labels.Length; index++)
                {
                    if (values[index] <= 0) continue;
                    var y = 49 + legendIndex++ * 44;
                    using var swatch = new SKPaint { Color = colors[index], IsAntialias = true };
                    canvas.DrawRoundRect(new SKRect(278, y - 14, 294, y + 2), 3, 3, swatch);
                    DrawChartText(canvas, labels[index], 304, y, 16, colors[index], SKTextAlign.Left, true);
                    DrawChartText(canvas, $"{values[index]:N0} ({percentages[index]}%)", 304, y + 19, 14, SKColor.Parse("#475569"));
                }
            }

            using var image = surface.Snapshot();
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            return encoded.ToArray();
        }

        private static byte[] CreateMarshmallowWeeklyChart(List<MarshmallowDailyActivity>? dailyData, bool historyAvailable)
        {
            const int width = 960;
            const int height = 440;
            var points = (dailyData ?? new List<MarshmallowDailyActivity>()).OrderBy(item => item.Date).TakeLast(7).ToList();
            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            if (!historyAvailable)
            {
                DrawChartText(canvas, "7-day history unavailable", width / 2f, height / 2f, 28, SKColor.Parse("#475569"), SKTextAlign.Center, true);
                using var unavailableImage = surface.Snapshot();
                using var unavailablePng = unavailableImage.Encode(SKEncodedImageFormat.Png, 100);
                return unavailablePng.ToArray();
            }
            if (points.Count == 0)
                points = Enumerable.Range(0, 7).Select(offset => new MarshmallowDailyActivity { Date = DateTime.Today.AddDays(-6 + offset) }).ToList();

            const float left = 92;
            const float right = 940;
            const float top = 36;
            const float bottom = 335;
            var colors = new[] { "#1d4ed8", "#059669", "#f59e0b" }.Select(SKColor.Parse).ToArray();
            var max = points.SelectMany(point => new[] { point.Orders, point.Delivered, point.Pending }).DefaultIfEmpty(0).Max();
            var yMax = max <= 0 ? 4 : Math.Max(4, (int)Math.Ceiling(max / 4d) * 4);
            using var gridPaint = new SKPaint { Color = SKColor.Parse("#e2e8f0"), IsAntialias = true, StrokeWidth = 2 };
            using var todayOutline = new SKPaint { Color = SKColor.Parse("#f59e0b"), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            for (var tick = 0; tick <= 4; tick++)
            {
                var y = bottom - (bottom - top) * tick / 4f;
                canvas.DrawLine(left, y, right, y, gridPaint);
                DrawChartText(canvas, (yMax * tick / 4d).ToString("0.#"), left - 12, y + 6, 17, SKColor.Parse("#475569"), SKTextAlign.Right);
            }

            var groupWidth = (right - left) / points.Count;
            var barWidth = Math.Min(18f, groupWidth * 0.16f);
            const float barGap = 4f;
            var chartHeight = bottom - top;
            for (var pointIndex = 0; pointIndex < points.Count; pointIndex++)
            {
                var point = points[pointIndex];
                var center = left + groupWidth * (pointIndex + 0.5f);
                var values = new[] { point.Orders, point.Delivered, point.Pending };
                var totalWidth = values.Length * barWidth + (values.Length - 1) * barGap;
                for (var seriesIndex = 0; seriesIndex < values.Length; seriesIndex++)
                {
                    var x = center - totalWidth / 2 + seriesIndex * (barWidth + barGap);
                    var barHeight = Math.Max(0f, values[seriesIndex]) / yMax * chartHeight;
                    using var barPaint = new SKPaint { Color = colors[seriesIndex], IsAntialias = true };
                    canvas.DrawRoundRect(new SKRect(x, bottom - barHeight, x + barWidth, bottom), 3, 3, barPaint);
                }
                if (pointIndex == points.Count - 1)
                    canvas.DrawRoundRect(new SKRect(center - totalWidth / 2 - 7, top - 9, center + totalWidth / 2 + 7, bottom + 7), 8, 8, todayOutline);
                DrawChartText(canvas, point.Date.ToString("dd MMM", CultureInfo.InvariantCulture), center, bottom + 28, 17, SKColor.Parse("#475569"), SKTextAlign.Center);
                if (pointIndex == points.Count - 1)
                    DrawChartText(canvas, "TODAY", center, bottom + 52, 15, SKColor.Parse("#b45309"), SKTextAlign.Center, true);
            }

            var labels = new[] { "Orders", "Delivered", "Pending" };
            var legendPositions = new[] { 330f, 455f, 590f };
            for (var index = 0; index < labels.Length; index++)
            {
                using var swatch = new SKPaint { Color = colors[index], IsAntialias = true };
                canvas.DrawRoundRect(new SKRect(legendPositions[index], 402, legendPositions[index] + 17, 419), 3, 3, swatch);
                DrawChartText(canvas, labels[index], legendPositions[index] + 24, 417, 16, SKColor.Parse("#475569"));
            }
            canvas.DrawRoundRect(new SKRect(810, 402, 828, 419), 4, 4, todayOutline);
            DrawChartText(canvas, "Today", 835, 417, 16, SKColor.Parse("#475569"));

            using var image = surface.Snapshot();
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            return encoded.ToArray();
        }

        private static void AddMrdCard(RowDescriptor row, int iconType, string title, int value, string background, string accent)
        {
            row.RelativeItem().Background(background).Border(0.6f).BorderColor("#cbd5e1").Padding(8).Column(card =>
            {
                card.Item().Row(r =>
                {
                    r.RelativeItem().Text(title).Bold().FontSize(7).FontColor(accent);
                    r.ConstantItem(22).Height(22).Image(CreateMrdIcon(iconType, accent, "#38bdf8")).FitArea();
                });
                card.Item().PaddingTop(3).Text(value.ToString("N0")).Bold().FontSize(15).FontColor(accent);
                card.Item().PaddingTop(2).Text("All-time total").FontSize(6).FontColor("#64748b");
            });
        }

        private static void AddMrdRow(TableDescriptor table, int iconType, string label, int value, string background, string accent)
        {
            table.Cell().Border(0.5f).BorderColor("#cbd5e1").Background(background).Padding(3).Row(row =>
            {
                row.ConstantItem(23).Height(16).PaddingRight(7).AlignMiddle().AlignCenter().Image(CreateMrdIcon(iconType, accent, "#38bdf8")).FitArea();
                row.RelativeItem().AlignMiddle().Text(label).Bold().FontSize(8);
            });
            table.Cell().Border(0.5f).BorderColor("#cbd5e1").Padding(5).AlignRight().Text(value.ToString("N0")).Bold().FontSize(8);
        }

        private static byte[] CreateColorIcon(string accent, string highlight)
        {
            using var surface = SKSurface.Create(new SKImageInfo(64, 64, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            var accentColor = SKColor.Parse(accent);
            var highlightColor = SKColor.Parse(highlight);
            using var backgroundPaint = new SKPaint { Color = accentColor.WithAlpha(36), IsAntialias = true };
            using var outlinePaint = new SKPaint { Color = accentColor, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3 };
            using var paperPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
            using var highlightPaint = new SKPaint { Color = highlightColor, IsAntialias = true };
            using var whiteStroke = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3 };
            using var linePaint = new SKPaint { Color = accentColor, IsAntialias = true, StrokeWidth = 3, StrokeCap = SKStrokeCap.Round };

            canvas.DrawCircle(32, 32, 30, backgroundPaint);
            canvas.DrawRoundRect(new SKRect(9, 9, 49, 55), 5, 5, paperPaint);
            canvas.DrawRoundRect(new SKRect(9, 9, 49, 55), 5, 5, outlinePaint);
            canvas.DrawLine(38, 10, 38, 22, outlinePaint);
            canvas.DrawLine(38, 22, 49, 22, outlinePaint);
            canvas.DrawLine(17, 31, 34, 31, linePaint);
            canvas.DrawLine(17, 38, 34, 38, linePaint);
            canvas.DrawLine(17, 45, 27, 45, linePaint);
            canvas.DrawCircle(47, 47, 12, highlightPaint);
            canvas.DrawCircle(47, 47, 12, whiteStroke);
            using var checkPath = new SKPath();
            checkPath.MoveTo(42, 47);
            checkPath.LineTo(45.5f, 50.5f);
            checkPath.LineTo(52, 43.5f);
            using var checkPaint = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round };
            canvas.DrawPath(checkPath, checkPaint);

            using var image = surface.Snapshot();
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            return encoded.ToArray();
        }

        private static byte[] CreatePeonBookIcon(int type, string accent, string highlight)
        {
            using var surface = SKSurface.Create(new SKImageInfo(96, 96, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            var primary = SKColor.Parse(accent);
            var secondary = SKColor.Parse(highlight);
            using var stroke = new SKPaint { Color = primary, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 4, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round };
            using var thin = new SKPaint { Color = primary, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round };
            using var fill = new SKPaint { Color = SKColors.White, IsAntialias = true };
            using var colorFill = new SKPaint { Color = secondary, IsAntialias = true };
            using var line = new SKPaint { Color = primary, IsAntialias = true, StrokeWidth = 3, StrokeCap = SKStrokeCap.Round };

            if (type == 1)
            {
                // Created: open register book with its pen.
                canvas.DrawRoundRect(new SKRect(10, 12, 70, 84), 7, 7, fill);
                canvas.DrawRoundRect(new SKRect(10, 12, 70, 84), 7, 7, stroke);
                canvas.DrawLine(24, 13, 24, 83, stroke);
                canvas.DrawRoundRect(new SKRect(80, 18, 88, 64), 3, 3, colorFill);
                canvas.DrawRoundRect(new SKRect(80, 18, 88, 64), 3, 3, thin);
                canvas.DrawLine(84, 64, 84, 79, stroke);
                canvas.DrawLine(80, 18, 80, 10, stroke);
                canvas.DrawLine(88, 18, 88, 10, stroke);
                canvas.DrawLine(80, 10, 88, 10, stroke);
            }
            else if (type == 2)
            {
                // Delivered: parcel handoff with a delivery vehicle and approval mark.
                canvas.DrawRoundRect(new SKRect(28, 17, 56, 47), 4, 4, fill);
                canvas.DrawRoundRect(new SKRect(28, 17, 56, 47), 4, 4, stroke);
                canvas.DrawLine(42, 18, 42, 46, thin);
                canvas.DrawLine(29, 29, 55, 29, thin);
                using var hand = new SKPath();
                hand.MoveTo(8, 55); hand.LineTo(21, 46); hand.LineTo(29, 52); hand.LineTo(47, 52);
                hand.LineTo(52, 58); hand.LineTo(35, 63); hand.LineTo(22, 61); hand.LineTo(12, 68);
                canvas.DrawPath(hand, new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 4, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round });
                canvas.DrawPath(hand, thin);
                using var truck = new SKPath();
                truck.MoveTo(12, 76); truck.LineTo(64, 76); truck.LineTo(64, 60); truck.LineTo(53, 60);
                truck.LineTo(46, 51); truck.LineTo(30, 51); truck.LineTo(22, 58); truck.LineTo(12, 58); truck.Close();
                canvas.DrawPath(truck, fill); canvas.DrawPath(truck, stroke);
                canvas.DrawCircle(27, 78, 6, fill); canvas.DrawCircle(27, 78, 6, stroke);
                canvas.DrawCircle(57, 78, 6, fill); canvas.DrawCircle(57, 78, 6, stroke);
                canvas.DrawCircle(76, 25, 16, colorFill);
                using var tick = new SKPath(); tick.MoveTo(68, 25); tick.LineTo(74, 31); tick.LineTo(84, 19);
                canvas.DrawPath(tick, new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 4, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round });
            }
            else if (type == 3)
            {
                // Received: record sheet with a large completed check.
                canvas.DrawRoundRect(new SKRect(18, 9, 78, 88), 4, 4, fill);
                canvas.DrawRoundRect(new SKRect(18, 9, 78, 88), 4, 4, stroke);
                canvas.DrawLine(60, 10, 60, 24, stroke); canvas.DrawLine(60, 24, 78, 24, stroke);
                foreach (var y in new[] { 42f, 58f, 74f })
                    canvas.DrawLine(33, y, 66, y, line);
                using var tick = new SKPath(); tick.MoveTo(7, 58); tick.LineTo(30, 81); tick.LineTo(85, 22);
                canvas.DrawPath(tick, new SKPaint { Color = secondary, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 8, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round });
            }
            else if (type == 4)
            {
                // Inactive: archived record sheet marked unavailable.
                using var page = new SKPath(); page.MoveTo(16, 28); page.LineTo(44, 8); page.LineTo(78, 8); page.LineTo(78, 86); page.LineTo(16, 86); page.Close();
                canvas.DrawPath(page, fill); canvas.DrawPath(page, stroke);
                canvas.DrawLine(44, 9, 44, 27, stroke); canvas.DrawLine(44, 27, 17, 27, stroke);
                canvas.DrawLine(28, 47, 63, 47, line); canvas.DrawLine(28, 60, 63, 60, line); canvas.DrawLine(28, 73, 63, 73, line);
                canvas.DrawCircle(74, 27, 20, colorFill);
                canvas.DrawCircle(74, 27, 20, new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 4 });
                canvas.DrawLine(68, 40, 81, 13, stroke);
            }
            else
            {
                // Pending: waiting record with an hourglass.
                canvas.DrawRoundRect(new SKRect(10, 17, 61, 86), 5, 5, fill);
                canvas.DrawRoundRect(new SKRect(10, 17, 61, 86), 5, 5, stroke);
                canvas.DrawRoundRect(new SKRect(20, 31, 40, 47), 2, 2, thin);
                canvas.DrawLine(20, 57, 48, 57, line); canvas.DrawLine(20, 68, 44, 68, line);
                canvas.DrawLine(20, 78, 34, 78, line);
                canvas.DrawLine(68, 15, 90, 15, stroke); canvas.DrawLine(68, 79, 90, 79, stroke);
                using var glass = new SKPath(); glass.MoveTo(71, 17); glass.LineTo(71, 34); glass.LineTo(79, 46); glass.LineTo(71, 59); glass.LineTo(71, 77);
                glass.MoveTo(87, 17); glass.LineTo(87, 34); glass.LineTo(79, 46); glass.LineTo(87, 59); glass.LineTo(87, 77);
                canvas.DrawPath(glass, stroke);
                canvas.DrawLine(74, 67, 84, 67, line);
                canvas.DrawCircle(79, 46, 3, colorFill);
            }

            using var image = surface.Snapshot();
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            return encoded.ToArray();
        }

        private static byte[] CreateMrdIcon(int type, string accent, string highlight)
        {
            using var surface = SKSurface.Create(new SKImageInfo(96, 96, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            var primary = SKColor.Parse(accent);
            var secondary = SKColor.Parse(highlight);
            using var stroke = new SKPaint { Color = primary, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 4, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round };
            using var thin = new SKPaint { Color = primary, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round };
            using var fill = new SKPaint { Color = SKColors.White, IsAntialias = true };
            using var colorFill = new SKPaint { Color = secondary, IsAntialias = true };
            using var line = new SKPaint { Color = primary, IsAntialias = true, StrokeWidth = 3, StrokeCap = SKStrokeCap.Round };

            if (type == 1 || type == 2)
            {
                // Active/inactive patient folder; active includes a pulse and check.
                using var folder = new SKPath();
                folder.MoveTo(10, 30); folder.LineTo(10, 19); folder.QuadTo(10, 14, 15, 14); folder.LineTo(38, 14);
                folder.LineTo(46, 24); folder.LineTo(80, 24); folder.QuadTo(86, 24, 84, 30); folder.LineTo(75, 76);
                folder.QuadTo(74, 82, 68, 82); folder.LineTo(16, 82); folder.QuadTo(9, 82, 10, 75); folder.Close();
                canvas.DrawPath(folder, fill); canvas.DrawPath(folder, stroke);
                if (type == 1)
                {
                    using var pulse = new SKPath();
                    pulse.MoveTo(20, 51); pulse.LineTo(31, 51); pulse.LineTo(36, 43); pulse.LineTo(41, 60);
                    pulse.LineTo(46, 35); pulse.LineTo(52, 54); pulse.LineTo(59, 47); pulse.LineTo(68, 47);
                    canvas.DrawPath(pulse, new SKPaint { Color = secondary, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 4, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round });
                    using var tick = new SKPath(); tick.MoveTo(54, 55); tick.LineTo(60, 61); tick.LineTo(73, 47);
                    canvas.DrawPath(tick, new SKPaint { Color = primary, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 4, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round });
                }
                else
                {
                    canvas.DrawCircle(48, 48, 37, new SKPaint { Color = primary.WithAlpha(45), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3 });
                    canvas.DrawLine(22, 74, 75, 22, stroke);
                }
            }
            else if (type == 3)
            {
                // Uploaded medical file: stacked records with medical cross and upload arrow.
                canvas.DrawRoundRect(new SKRect(14, 20, 72, 84), 4, 4, fill); canvas.DrawRoundRect(new SKRect(14, 20, 72, 84), 4, 4, stroke);
                canvas.DrawRoundRect(new SKRect(21, 14, 79, 78), 4, 4, fill); canvas.DrawRoundRect(new SKRect(21, 14, 79, 78), 4, 4, stroke);
                canvas.DrawLine(31, 62, 67, 62, line); canvas.DrawLine(31, 70, 59, 70, line);
                canvas.DrawLine(50, 26, 50, 48, stroke); canvas.DrawLine(40, 37, 60, 37, stroke);
                using var arrow = new SKPath(); arrow.MoveTo(72, 43); arrow.LineTo(84, 31); arrow.LineTo(92, 39);
                canvas.DrawPath(arrow, thin); canvas.DrawLine(84, 32, 84, 58, thin);
            }
            else if (type == 4)
            {
                // Patient file with a patient silhouette and stethoscope.
                canvas.DrawRoundRect(new SKRect(12, 10, 78, 87), 5, 5, fill); canvas.DrawRoundRect(new SKRect(12, 10, 78, 87), 5, 5, stroke);
                canvas.DrawCircle(45, 34, 10, colorFill); canvas.DrawCircle(45, 34, 10, thin);
                using var shoulders = new SKPath(); shoulders.MoveTo(29, 61); shoulders.QuadTo(30, 45, 45, 45); shoulders.QuadTo(60, 45, 61, 61); shoulders.Close();
                canvas.DrawPath(shoulders, colorFill); canvas.DrawPath(shoulders, thin);
                canvas.DrawLine(23, 69, 55, 69, line); canvas.DrawLine(23, 76, 48, 76, line);
                using var stethoscope = new SKPath(); stethoscope.MoveTo(65, 20); stethoscope.LineTo(65, 34); stethoscope.QuadTo(65, 44, 74, 44); stethoscope.QuadTo(83, 44, 83, 34);
                canvas.DrawPath(stethoscope, stroke); canvas.DrawCircle(83, 31, 4, colorFill); canvas.DrawCircle(83, 31, 4, stroke);
            }
            else if (type == 5)
            {
                // Verified record certificate with a stamped seal.
                canvas.DrawRect(new SKRect(17, 8, 78, 82), fill); canvas.DrawRect(new SKRect(17, 8, 78, 82), stroke);
                using var heading = new SKPaint { Color = primary, IsAntialias = true, StrokeWidth = 3, StrokeCap = SKStrokeCap.Round };
                canvas.DrawLine(28, 24, 68, 24, heading); canvas.DrawLine(28, 35, 68, 35, heading);
                canvas.DrawLine(28, 46, 60, 46, heading); canvas.DrawLine(28, 56, 52, 56, heading);
                canvas.DrawCircle(72, 66, 17, colorFill); canvas.DrawCircle(72, 66, 17, stroke);
                using var tick = new SKPath(); tick.MoveTo(64, 66); tick.LineTo(70, 72); tick.LineTo(81, 59);
                canvas.DrawPath(tick, new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 4, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round });
                using var ribbon = new SKPath(); ribbon.MoveTo(63, 79); ribbon.LineTo(63, 92); ribbon.LineTo(72, 85); ribbon.LineTo(81, 92); ribbon.LineTo(81, 78);
                canvas.DrawPath(ribbon, thin);
            }
            else if (type == 6)
            {
                // Pending review: unverified record, hourglass, and search lens.
                using var page = new SKPath(); page.MoveTo(12, 28); page.LineTo(30, 10); page.LineTo(60, 10); page.LineTo(60, 84); page.LineTo(12, 84); page.Close();
                canvas.DrawPath(page, fill); canvas.DrawPath(page, stroke); canvas.DrawLine(30, 10, 30, 28, thin); canvas.DrawLine(30, 28, 12, 28, thin);
                canvas.DrawLine(21, 45, 48, 45, line); canvas.DrawLine(21, 57, 42, 57, line); canvas.DrawLine(21, 69, 34, 69, line);
                canvas.DrawCircle(64, 62, 19, colorFill); canvas.DrawCircle(64, 62, 19, stroke); canvas.DrawLine(78, 76, 90, 88, stroke);
                canvas.DrawLine(59, 49, 70, 49, thin); canvas.DrawLine(59, 75, 70, 75, thin);
                canvas.DrawLine(60, 50, 60, 56, thin); canvas.DrawLine(69, 50, 69, 56, thin); canvas.DrawLine(60, 74, 60, 68, thin); canvas.DrawLine(69, 74, 69, 68, thin);
                canvas.DrawCircle(64.5f, 62, 2.5f, stroke);
            }
            else if (type == 7)
            {
                // Admission: patient bed with IV pole and inward arrow.
                canvas.DrawLine(15, 26, 15, 78, stroke); canvas.DrawLine(15, 56, 80, 56, stroke); canvas.DrawLine(15, 78, 15, 86, stroke); canvas.DrawLine(80, 56, 80, 86, stroke);
                canvas.DrawCircle(35, 43, 9, colorFill); canvas.DrawCircle(35, 43, 9, stroke);
                canvas.DrawLine(44, 48, 66, 48, stroke); canvas.DrawLine(44, 48, 44, 56, stroke);
                canvas.DrawLine(16, 28, 16, 18, stroke); canvas.DrawLine(16, 18, 39, 18, stroke);
                canvas.DrawLine(39, 18, 39, 36, stroke); canvas.DrawLine(39, 36, 47, 36, stroke);
                using var arrow = new SKPath(); arrow.MoveTo(66, 22); arrow.LineTo(80, 22); arrow.LineTo(74, 16); arrow.MoveTo(80, 22); arrow.LineTo(74, 28);
                canvas.DrawPath(arrow, thin);
            }
            else
            {
                // Discharge: record plus patient leaving through an exit arrow.
                canvas.DrawRoundRect(new SKRect(12, 12, 56, 86), 3, 3, fill); canvas.DrawRoundRect(new SKRect(12, 12, 56, 86), 3, 3, stroke);
                canvas.DrawRoundRect(new SKRect(21, 21, 48, 34), 2, 2, colorFill); canvas.DrawRoundRect(new SKRect(21, 21, 48, 34), 2, 2, thin);
                canvas.DrawLine(21, 46, 47, 46, line); canvas.DrawLine(21, 55, 42, 55, line);
                canvas.DrawCircle(64, 39, 9, colorFill); canvas.DrawCircle(64, 39, 9, thin);
                using var person = new SKPath(); person.MoveTo(56, 73); person.QuadTo(56, 55, 64, 55); person.QuadTo(72, 55, 72, 73);
                canvas.DrawPath(person, thin);
                using var exit = new SKPath(); exit.MoveTo(58, 82); exit.LineTo(86, 82); exit.LineTo(78, 74); exit.MoveTo(86, 82); exit.LineTo(78, 90);
                canvas.DrawPath(exit, stroke);
            }

            using var image = surface.Snapshot();
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            return encoded.ToArray();
        }

        private static byte[] CreateHmpkIcon(int type, string accent, string highlight)
        {
            using var surface = SKSurface.Create(new SKImageInfo(96, 96, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            var primary = SKColor.Parse(accent);
            var secondary = SKColor.Parse(highlight);
            using var stroke = new SKPaint { Color = primary, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 4, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round };
            using var thin = new SKPaint { Color = primary, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round };
            using var fill = new SKPaint { Color = SKColors.White, IsAntialias = true };
            using var colorFill = new SKPaint { Color = secondary, IsAntialias = true };
            using var line = new SKPaint { Color = primary, IsAntialias = true, StrokeWidth = 3, StrokeCap = SKStrokeCap.Round };

            if (type == 1)
            {
                // Demand checklist with a folded page corner and checked task.
                canvas.DrawRoundRect(new SKRect(12, 7, 72, 88), 6, 6, fill);
                canvas.DrawRoundRect(new SKRect(12, 7, 72, 88), 6, 6, stroke);
                canvas.DrawLine(57, 8, 57, 24, stroke);
                canvas.DrawLine(57, 24, 72, 24, stroke);
                foreach (var y in new[] { 39f, 56f, 73f })
                {
                    canvas.DrawRoundRect(new SKRect(23, y - 5, 35, y + 7), 1, 1, thin);
                    canvas.DrawLine(44, y + 1, 64, y + 1, line);
                }
                using var tick = new SKPath();
                tick.MoveTo(24, 55); tick.LineTo(28, 60); tick.LineTo(37, 49);
                canvas.DrawPath(tick, new SKPaint { Color = secondary, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 4, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round });
            }
            else if (type == 2)
            {
                // Purchase order form with a prominent approval seal.
                canvas.DrawRoundRect(new SKRect(10, 8, 68, 88), 6, 6, fill);
                canvas.DrawRoundRect(new SKRect(10, 8, 68, 88), 6, 6, stroke);
                canvas.DrawLine(20, 29, 51, 29, line);
                canvas.DrawLine(20, 40, 59, 40, line);
                canvas.DrawLine(20, 50, 47, 50, line);
                canvas.DrawLine(20, 63, 58, 63, line);
                canvas.DrawLine(20, 74, 42, 74, line);
                canvas.DrawCircle(68, 43, 18, colorFill);
                canvas.DrawCircle(68, 43, 18, new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3 });
                using var seal = new SKPath();
                seal.MoveTo(59, 43); seal.LineTo(66, 50); seal.LineTo(78, 36);
                canvas.DrawPath(seal, new SKPaint { Color = primary, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 4, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round });
            }
            else if (type == 3)
            {
                // DMR delivery boxes behind a checked receiving sheet.
                using var box = new SKPath();
                box.MoveTo(8, 30); box.LineTo(38, 16); box.LineTo(68, 30); box.LineTo(68, 68); box.LineTo(38, 83); box.LineTo(8, 68); box.Close();
                canvas.DrawPath(box, fill); canvas.DrawPath(box, stroke);
                canvas.DrawLine(8, 30, 38, 44, thin); canvas.DrawLine(38, 44, 68, 30, thin); canvas.DrawLine(38, 44, 38, 83, thin);
                canvas.DrawRoundRect(new SKRect(34, 30, 84, 80), 4, 4, fill);
                canvas.DrawRoundRect(new SKRect(34, 30, 84, 80), 4, 4, stroke);
                canvas.DrawRect(new SKRect(41, 39, 49, 47), colorFill); canvas.DrawRect(new SKRect(54, 42, 76, 45), line);
                canvas.DrawRect(new SKRect(41, 53, 49, 61), colorFill); canvas.DrawRect(new SKRect(54, 56, 76, 59), line);
                canvas.DrawRect(new SKRect(41, 67, 49, 75), colorFill); canvas.DrawRect(new SKRect(54, 70, 73, 73), line);
                canvas.DrawCircle(76, 72, 13, colorFill);
                using var verified = new SKPath();
                verified.MoveTo(70, 72); verified.LineTo(74, 76); verified.LineTo(82, 67);
                canvas.DrawPath(verified, new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round });
            }
            else
            {
                // Bill payment register with payment slip, card terminal, and coin.
                canvas.DrawRoundRect(new SKRect(10, 15, 75, 78), 8, 8, fill);
                canvas.DrawRoundRect(new SKRect(10, 15, 75, 78), 8, 8, stroke);
                canvas.DrawLine(22, 29, 59, 29, line);
                canvas.DrawLine(22, 39, 47, 39, line);
                canvas.DrawLine(22, 51, 42, 51, line);
                canvas.DrawLine(22, 60, 50, 60, line);
                canvas.DrawRoundRect(new SKRect(38, 54, 87, 83), 4, 4, colorFill);
                canvas.DrawRoundRect(new SKRect(38, 54, 87, 83), 4, 4, stroke);
                canvas.DrawLine(45, 64, 79, 64, line);
                canvas.DrawLine(45, 72, 59, 72, line);
                canvas.DrawCircle(68, 23, 15, colorFill);
                canvas.DrawCircle(68, 23, 15, stroke);
                using var currency = new SKPath();
                currency.MoveTo(74, 17); currency.LineTo(64, 17); currency.LineTo(61, 21); currency.LineTo(74, 21);
                currency.MoveTo(61, 21); currency.LineTo(71, 29); currency.LineTo(64, 29);
                currency.MoveTo(67, 14); currency.LineTo(67, 32);
                canvas.DrawPath(currency, thin);
            }

            using var image = surface.Snapshot();
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            return encoded.ToArray();
        }

        private string ResolveAssetPath(params string[] assetSegments)
        {
            return Path.Combine(_webHostEnvironment.WebRootPath, "Assets", Path.Combine(assetSegments));
        }
    }
}
