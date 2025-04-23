namespace AdministradorTienda.EmailSender;
using System.Net.Mail;


public interface ICustomEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string message);

    Task SendEmailAsync2(string toEmail, string subject, string message, Attachment attachment = null);
}

