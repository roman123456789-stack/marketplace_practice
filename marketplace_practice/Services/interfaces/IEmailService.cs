using marketplace_practice.Services.service_models;
using MimeKit;

namespace marketplace_practice.Services.interfaces
{
    public interface IEmailService
    {
        public Task SendEmailConfirmationAsync(string email, string firstName, long userId, string token);
        public Task SendPasswordResetEmailAsync(string email, string firstName, string token);
        public Task SendEmailChangeConfirmationAsync(string email, string firstName, long userId, string token);
        public Task SendPdfReceiptAsync(string email, string firstName, byte[] pdfBytes, string fileName, string subject = "Ваш чек");
        public Task SendEmailAsync(EmailMessage message);
        public Task SendEmailWithAttachmentAsync(EmailMessage message, byte[] attachmentData, string fileName, string contentType);
    }
}
