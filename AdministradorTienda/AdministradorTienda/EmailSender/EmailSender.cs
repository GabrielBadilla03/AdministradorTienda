using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace AdministradorTienda.EmailSender
{
    public class EmailSender : ICustomEmailSender
    {
        private readonly EmailSettings _emailSettings;

        public EmailSender(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var mail = new MailMessage()
            {
                From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            mail.To.Add(new MailAddress(toEmail));

            using (var smtp = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort))
            {
                smtp.Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.Password);
                smtp.EnableSsl = true;
                await smtp.SendMailAsync(mail);
            }
        }

        public async Task SendEmailAsync2(string toEmail, string subject, string message, Attachment attachment = null)
        {
            using (var mail = new MailMessage())
            {
                mail.From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName);
                mail.To.Add(new MailAddress(toEmail));
                mail.Subject = subject;
                mail.Body = message;
                mail.IsBodyHtml = true;

                if (attachment != null)
                {
                    mail.Attachments.Add(attachment);
                }

                using (var smtp = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort))
                {
                    smtp.Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.Password);
                    smtp.EnableSsl = true;
                    await smtp.SendMailAsync(mail);
                }
            }
        }
    }
}
