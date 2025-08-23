using marketplace_practice.Services.interfaces;
using marketplace_practice.Services.service_models;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Web;

namespace marketplace_practice.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailConfiguration _emailConfig;

        public EmailService(
            IOptions<EmailConfiguration> emailConfig)
        {
            _emailConfig = emailConfig.Value;
        }

        public async Task SendEmailConfirmationAsync(string email, string firstName, long userId, string token)
        {
            var encodedToken = HttpUtility.UrlEncode(token);
            var callbackUrl = $"{_emailConfig.BaseUrl}/email-verification/confirm-email?userId={userId.ToString()}&token={encodedToken}";

            var message = new EmailMessage
            {
                To = new List<string> { email },
                Subject = "Подтвердите ваш email",
                Body = $@"
                <h1>Добро пожаловать, {firstName}!</h1>
                <p>Пожалуйста, подтвердите ваш email, перейдя по <a href='{callbackUrl}'>ссылке</a>.</p>
                <p>Ссылка действительна 24 часа.</p>
                <p>Если вы не регистрировались на нашем сайте, проигнорируйте это письмо.</p>"
            };

            await SendEmailAsync(message);
        }

        public async Task SendPasswordResetEmailAsync(string email, string firstName, string token)
        {
            var encodedToken = HttpUtility.UrlEncode(token);
            var callbackUrl = $"{_emailConfig.BaseUrl}/auth/reset-password-view?email={email}&token={encodedToken}";
            // точный callbackUrl ещё не известен

            var message = new EmailMessage
            {
                To = new List<string> { email },
                Subject = "Подтвердите ваш email",
                Body = $@"
                <h1>Восстановление пароля</h1>
                <p>Уважаемый {firstName}, Вы сделали запрос на сброс и восстановление пароля. Чтобы создать новый пароль перейдите по ссылке:</p>
                <a href='{callbackUrl}'>Сбросить пароль</a>
                <p>Ссылка действительна в течение 24 часов.</p>
                <p>Если вы не запрашивали сброс пароля, проигнорируйте это письмо.</p>"
            };

            await SendEmailAsync(message);
        }

        public async Task SendEmailChangeConfirmationAsync(string newEmail, string firstName, long userId, string token)
        {
            var encodedToken = HttpUtility.UrlEncode(token);
            var callbackUrl = $"{_emailConfig.BaseUrl}/auth/confirm-email-change?userId={userId.ToString()}&newEmail={newEmail}&token={encodedToken}";

            var message = new EmailMessage
            {
                To = new List<string> { newEmail },
                Subject = "Подтверждение изменения email",
                Body = $@"
                    <h1>Подтвердите изменение email</h1>
                    <p>Уважаемый {firstName}, Вы сделали запрос на сброс и обновление почтового ящика.</p>
                    <p>Для подтверждения нового email перейдите по ссылке:</p>
                    <a href='{callbackUrl}'>Подтвердить email</a>
                    <p>Если вы не запрашивали изменение email, немедленно сообщите в поддержку.</p>"
            };

            await SendEmailAsync(message);
        }

        public async Task SendPdfReceiptAsync(string email, string firstName, byte[] pdfBytes, string fileName, string subject = "Ваш чек")
        {
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                throw new ArgumentException("PDF bytes cannot be null or empty", nameof(pdfBytes));
            }

            var message = new EmailMessage
            {
                To = new List<string> { email },
                Subject = subject,
                Body = $@"
                    <h1>Уважаемый(ая) {firstName}!</h1>
                    <p>Ваш чек готов и прикреплен к этому письму.</p>
                    <p>Спасибо за покупку!</p>
                    <p>С уважением,<br>Команда {_emailConfig.FromName}</p>"
            };

            await SendEmailWithAttachmentAsync(message, pdfBytes, fileName, "application/pdf");
        }

        public async Task SendEmailAsync(EmailMessage message)
        {
            var emailMessage = CreateEmailMessage(message);
            await SendAsync(emailMessage);
        }

        private MimeMessage CreateEmailMessage(EmailMessage message)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(_emailConfig.FromName, _emailConfig.FromAddress));
            emailMessage.To.AddRange(message.To.Select(x => new MailboxAddress(x, x)));
            emailMessage.Subject = message.Subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = message.Body
            };

            emailMessage.Body = bodyBuilder.ToMessageBody();
            return emailMessage;
        }

        private async Task SendAsync(MimeMessage mailMessage)
        {
            using var client = new MailKit.Net.Smtp.SmtpClient();
            try
            {
                await client.ConnectAsync(
                    _emailConfig.SmtpServer, _emailConfig.Port,
                    MailKit.Security.SecureSocketOptions.Auto);
                await client.AuthenticateAsync(_emailConfig.UserName, _emailConfig.Password);
                await client.SendAsync(mailMessage);
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }

        public async Task SendEmailWithAttachmentAsync(EmailMessage message, byte[] attachmentData, string fileName, string contentType)
        {
            var emailMessage = CreateEmailMessageWithAttachment(message, attachmentData, fileName, contentType);
            await SendAsync(emailMessage);
        }

        private MimeMessage CreateEmailMessageWithAttachment(EmailMessage message, byte[] attachmentData, string fileName, string contentType)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(_emailConfig.FromName, _emailConfig.FromAddress));
            emailMessage.To.AddRange(message.To.Select(x => new MailboxAddress(x, x)));
            emailMessage.Subject = message.Subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = message.Body
            };

            // Добавляем вложение
            bodyBuilder.Attachments.Add(fileName, attachmentData, ContentType.Parse(contentType));

            emailMessage.Body = bodyBuilder.ToMessageBody();
            return emailMessage;
        }
    }
}
