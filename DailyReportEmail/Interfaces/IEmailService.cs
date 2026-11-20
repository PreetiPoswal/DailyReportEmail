namespace DailyReportEmail.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(
            string subject,
            string body,
            byte[] pdfBytes,
            string fileName,
            string? recipientEmail = null);
    }
}