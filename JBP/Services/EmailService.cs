using System.Net;
using System.Net.Mail;

namespace JBP.Services
{
    // Central SMTP wrapper for notification emails.
    // Settings come from appsettings: EmailSettings section.
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(
            IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmail(
            string toEmail,
            string subject,
            string body)
        {
            // Keep SMTP details in configuration so DevOps can change them without code edits.
            var settings =
                _configuration.GetSection(
                    "EmailSettings");

            var fromEmail =
                settings["Email"];

            var password =
                (settings["Password"] ?? string.Empty)
                    .Replace(" ", string.Empty);

            var smtpClient = new SmtpClient(
                settings["Host"])
            {
                Port =
                    int.Parse(settings["Port"]!),

                Credentials =
                    new NetworkCredential(
                        fromEmail,
                        password),

                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From =
                    new MailAddress(
                        fromEmail!),

                Subject = subject,

                Body = body,

                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}
