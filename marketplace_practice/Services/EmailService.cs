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
        private readonly IWebHostEnvironment _env;

        public EmailService(
            IOptions<EmailConfiguration> emailConfig,
            IWebHostEnvironment env)
        {
            _emailConfig = emailConfig.Value;
            _env = env;
        }

        public async Task SendEmailConfirmationAsync(string email, long userId, string token)
        {
            var encodedToken = HttpUtility.UrlEncode(token);
            var callbackUrl = $"{_emailConfig.BaseUrl}/email-verification/confirm-email?userId={userId.ToString()}&token={encodedToken}";

            var message = new EmailMessage
            {
                To = new List<string> { email },
                Subject = "Подтвердите ваш email",
                Body = $@"
                <h1>Добро пожаловать!</h1>
                <p>Пожалуйста, подтвердите ваш email, перейдя по <a href='{callbackUrl}'>ссылке</a>.</p>
                <p>Ссылка действительна 24 часа.</p>"
            };

            await SendEmailAsync(message);
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
    }
}
