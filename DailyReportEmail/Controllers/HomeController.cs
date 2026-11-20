using DailyReportEmail.Interfaces;
using DailyReportEmail.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DailyReportEmail.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDatabaseService _databaseService;
        private readonly IPdfService _pdfService;
        private readonly IEmailService _emailService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IDatabaseService databaseService,
            IPdfService pdfService,
            IEmailService emailService,
            ILogger<HomeController> logger)
        {
            _databaseService = databaseService;
            _pdfService = pdfService;
            _emailService = emailService;
            _logger = logger;
        }


        public async Task<IActionResult> Index()
        {
            var report = await _databaseService.GetDailyReportAsync();
            return View(report ?? new DailyReportModel());
        }

        [HttpPost]
        public async Task<IActionResult> GeneratePdf()
        {
            
            var report = await _databaseService.GetDailyReportAsync();

            if (report == null)
            {
                return Content("No Data Found");
            }

            var pdfBytes = _pdfService.GenerateDailyReport(report);

            Response.Headers["Content-Disposition"] =
                $"inline; filename=DailyReport_{DateTime.Now:ddMMyyyy}.pdf";

            return File(
                pdfBytes,
                "application/pdf");
        }

        [HttpPost]
        public async Task<IActionResult> SendReport()
        {
            try
            {
                Console.WriteLine("[SendReport] Action started");
                _logger?.LogInformation("SendReport action invoked");

                // Database se data lo
                Console.WriteLine("[SendReport] Fetching daily report from database...");
                var report = await _databaseService.GetDailyReportAsync();

                Console.WriteLine($"[SendReport] Report fetched. HasReport={report != null}");
                _logger?.LogInformation("Retrieved report: {HasReport}", report != null);
                if (report == null)
                {
                    Console.WriteLine("[SendReport] No report data found. Stopping.");
                    TempData["Error"] = "No Data Found.";
                    return RedirectToAction("Index");
                }

                // PDF Generate
                Console.WriteLine("[SendReport] Generating PDF report...");
                var pdfBytes = _pdfService.GenerateDailyReport(report);
                Console.WriteLine($"[SendReport] PDF generated. Size={pdfBytes.Length} bytes");

                // Email Body
                string body = $@"
<html>
<head>
<style>
body {{
    font-family: Arial;
}}
table {{
    border-collapse: collapse;
    width:100%;
}}
th,td {{
    border:1px solid #ddd;
    padding:8px;
}}
th {{
    background:#0d6efd;
    color:white;
}}
</style>
</head>

<body>

<h2>Demand Daily Report</h2>

<p>Please find attached today's report.</p>

<table>

<tr>
<th>Metric</th>
<th>Value</th>
</tr>

<tr>
<td>Today's Demand</td>
<td>{report.TodayDemand}</td>
</tr>

<tr>
<td>Approved</td>
<td>{report.ApprovedDemand}</td>
</tr>

<tr>
<td>Pending</td>
<td>{report.PendingDemand}</td>
</tr>

<tr>
<td>Rejected</td>
<td>{report.RejectedDemand}</td>
</tr>

</table>

<br/>

Regards,<br/>
Demand Automation System

</body>

</html>";

                // Email Send
                Console.WriteLine("[SendReport] Calling EmailService.SendEmailAsync...");
                _logger?.LogInformation("Calling EmailService.SendEmailAsync");
                await _emailService.SendEmailAsync(
                    $"Demand Daily Report - {report.ReportDate:dd-MM-yyyy}",
                    body,
                    pdfBytes,
                    $"DailyReport_{DateTime.Now:ddMMyyyy}.pdf");

                Console.WriteLine("[SendReport] EmailService.SendEmailAsync completed successfully");
                _logger?.LogInformation("EmailService.SendEmailAsync returned");
                TempData["Success"] = "Report Sent Successfully.";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SendReport] Exception: {ex}");
                _logger?.LogError(ex, "SendReport failed");
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}
