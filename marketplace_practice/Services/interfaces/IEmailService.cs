using marketplace_practice.Services.service_models;
using MimeKit;

namespace marketplace_practice.Services.interfaces
{
    public interface IEmailService
    {
        public Task SendEmailConfirmationAsync(string email, long userId, string token);
        public Task SendEmailAsync(EmailMessage message);
    }
}
