using Microsoft.Extensions.Configuration;

namespace BusinessLogic.Service.System
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string token);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string token)
        {
            var baseUrl = _configuration["AppSettings:BaseUrl"];
            var resetLink = $"{baseUrl}/Auth/ResetPassword?token={token}&email={toEmail}";

            var subject = "[AEMS] Yêu cầu đặt lại mật khẩu";
            var body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2 style='color: #0d6efd;'>Đặt lại mật khẩu AEMS</h2>
                    <p>Xin chào,</p>
                    <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản liên kết với email <strong>{toEmail}</strong>.</p>
                    <p>Vui lòng nhấn vào nút bên dưới để đặt lại mật khẩu:</p>
                    <a href='{resetLink}' style='background-color: #0d6efd; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 10px 0;'>Đặt lại mật khẩu</a>
                    <p>Nếu bạn không yêu cầu thay đổi này, vui lòng bỏ qua email này.</p>
                    <hr />
                    <p style='font-size: 12px; color: gray;'>Đây là email tự động, vui lòng không trả lời.</p>
                </div>";

            // Get Config
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var port = int.Parse(_configuration["EmailSettings:Port"] ?? "587");
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var senderName = _configuration["EmailSettings:SenderName"];
            var password = _configuration["EmailSettings:Password"];

            using var client = new global::System.Net.Mail.SmtpClient(smtpServer, port);
            client.EnableSsl = true;
            client.Credentials = new global::System.Net.NetworkCredential(senderEmail, password);

            var mailMessage = new global::System.Net.Mail.MailMessage
            {
                From = new global::System.Net.Mail.MailAddress(senderEmail!, senderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
        }
    }
}
