using DailyReportEmail.Interfaces;
using DailyReportEmail.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace DailyReportEmail.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _settings = configuration.GetSection("EmailSettings").Get<EmailSettings>() ?? new EmailSettings();
            _logger = logger;
        }

        public async Task SendEmailAsync(
            string subject,
            string body,
            byte[] pdfBytes,
            string fileName,
            string? recipientEmail = null)
        {
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                var ex = new InvalidOperationException("PDF content is empty. Cannot attach report.");
                _logger.LogError(ex, "Email sending aborted because no PDF content was provided.");
                throw ex;
            }

            if (string.IsNullOrWhiteSpace(_settings.SmtpServer) || _settings.Port <= 0)
            {
                var ex = new InvalidOperationException("SMTP configuration is incomplete. Check the SmtpServer and Port values.");
                _logger.LogError(ex, "SMTP configuration validation failed.");
                throw ex;
            }

            if (string.IsNullOrWhiteSpace(_settings.SenderEmail) || string.IsNullOrWhiteSpace(_settings.AppPassword))
            {
                var ex = new InvalidOperationException("SMTP sender credentials are incomplete. Check SenderEmail and AppPassword.");
                _logger.LogError(ex, "SMTP credentials validation failed.");
                throw ex;
            }

            var recipients = BuildRecipients(recipientEmail);
            if (recipients.Count == 0)
            {
                var ex = new InvalidOperationException("No email recipients were provided. Configure Recipients or pass a recipientEmail.");
                _logger.LogError(ex, "SMTP recipients validation failed.");
                throw ex;
            }

            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_settings.SenderEmail));

            foreach (var mail in recipients)
            {
                email.To.Add(MailboxAddress.Parse(mail));
            }

            email.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = body };
            using var attachmentStream = new MemoryStream(pdfBytes, writable: false);
            builder.Attachments.Add(fileName, attachmentStream, new ContentType("application", "pdf"));
            email.Body = builder.ToMessageBody();

            _logger.LogInformation(
                "Preparing to send email. Subject='{Subject}', Attachment='{FileName}', Size={Size} bytes, Recipients={RecipientCount}",
                subject,
                fileName,
                pdfBytes.Length,
                recipients.Count);

            using var smtp = new SmtpClient { Timeout = 30000 };

            try
            {
                var secureSocketOption = _settings.Port == 465
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls;

                await smtp.ConnectAsync(_settings.SmtpServer, _settings.Port, secureSocketOption);

                if (!smtp.IsAuthenticated)
                {
                    await smtp.AuthenticateAsync(_settings.SenderEmail, _settings.AppPassword);
                }

                await smtp.SendAsync(email);
                _logger.LogInformation("Email sent successfully. Subject='{Subject}', Attachment='{FileName}'", subject, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "SMTP email send failed. Subject='{Subject}', Attachment='{FileName}', SmtpServer='{SmtpServer}', Port={Port}",
                    subject,
                    fileName,
                    _settings.SmtpServer,
                    _settings.Port);
                throw new InvalidOperationException($"Email delivery failed: {ex.Message}", ex);
            }
            finally
            {
                if (smtp.IsConnected)
                {
                    await smtp.DisconnectAsync(true);
                }
            }
        }

        private List<string> BuildRecipients(string? recipientEmail)
        {
            var recipients = new List<string>();

            if (!string.IsNullOrWhiteSpace(recipientEmail))
            {
                recipients.Add(recipientEmail.Trim());
            }

            if (!string.IsNullOrWhiteSpace(_settings.Recipients))
            {
                foreach (var mail in _settings.Recipients.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (!recipients.Contains(mail, StringComparer.OrdinalIgnoreCase))
                    {
                        recipients.Add(mail);
                    }
                }
            }

            return recipients;
        }
    }
}

