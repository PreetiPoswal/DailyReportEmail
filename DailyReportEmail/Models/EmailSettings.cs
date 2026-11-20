namespace DailyReportEmail.Models
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = "";
        public int Port { get; set; }
        public string SenderEmail { get; set; } = "";
        public string AppPassword { get; set; } = "";
        public string Recipients { get; set; } = "";
    }
}