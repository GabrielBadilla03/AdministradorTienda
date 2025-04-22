using Microsoft.AspNetCore.Identity.UI.Services;

namespace CasoPractico2.NoEmailSender
{
    public class NoEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // No hace nada, evita error por dependencia no resuelta
            return Task.CompletedTask;
        }
    }
}
