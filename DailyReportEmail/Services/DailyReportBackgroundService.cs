using DailyReportEmail.Interfaces;
using DailyReportEmail.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

public class DailyReportBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DailyReportBackgroundService> _logger;

    // CORRECTED: Shaam ke 6 baje (18:00:00)
    private readonly TimeSpan _scheduledTime = new TimeSpan(04, 00, 00);
    private DateTime? _lastRunDate = null;

    public DailyReportBackgroundService(IServiceProvider serviceProvider, ILogger<DailyReportBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HMPK Daily Report Background Service Started.");

        // Loop runs every 60 seconds to check if it's time to trigger
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;

            // Check if current time has passed the scheduled time AND it hasn't run today
            if (now.TimeOfDay >= _scheduledTime && (_lastRunDate == null || _lastRunDate.Value.Date < now.Date))
            {
                try
                {
                    _logger.LogInformation("Executing Daily Report Generation Job...");

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbService = scope.ServiceProvider.GetRequiredService<IDashboardDataService>();
                        var pdfService = scope.ServiceProvider.GetRequiredService<IPdfService>();
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                        // Fetch real-time DB data
                        var reportData = await dbService.GetDailyReportDataAsync();

                        // Generate complete PDF structure
                        byte[] pdfBytes = pdfService.GenerateDailyReport(reportData);

                        // Setup email parameters
                        string dateStr = now.ToString("dd-MM-yyyy");
                        string subject = $"HMPK Daily Backup Report - {dateStr}";
                        string body = $"<h3>Jai Hind Team,</h3><p>Please find attached the system-generated automated Daily Backup Report for HMPK Operations dated {dateStr}.</p><br/><p>Regards,<br/><b>CSSH IT Team</b></p>";
                        string fileName = $"HMPK_DailyReport_{now:ddMMyyyy}.pdf";

                        // Send Email
                        await emailService.SendEmailAsync(subject, body, pdfBytes, fileName);

                        _logger.LogInformation("Daily Report successfully emailed to admin.");

                        // Mark today's execution as successful
                        _lastRunDate = now;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while executing the daily report generation job.");

                }
            }

            // Sleep for 1 minute before checking the clock again
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}


