using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace BusinessLogic.Service.System
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string token);
        Task SendEventRegistrationEmailAsync(string toEmail, string studentName, string eventTitle, DateTime startTime, string location, string qrCodeBase64);
        Task SendAsync(string toEmail, string subject, string htmlBody);
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

        public async Task SendEventRegistrationEmailAsync(string toEmail, string studentName, string eventTitle, DateTime startTime, string location, string qrCodeBase64)
        {
            var subject = $"[AEMS] Xác nhận đăng ký sự kiện: {eventTitle}";
            var body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; max-width: 600px; margin: auto; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <h2 style='color: #198754; text-align: center;'>ĐĂNG KÝ THÀNH CÔNG</h2>
                    <p>Xin chào <strong>{studentName}</strong>,</p>
                    <p>Bạn đã đăng ký thành công tham gia sự kiện <strong>{eventTitle}</strong>.</p>
                    <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                        <p style='margin: 5px 0;'><strong>⏰ Thời gian:</strong> {startTime:dd/MM/yyyy HH:mm}</p>
                        <p style='margin: 5px 0;'><strong>📍 Địa điểm:</strong> {location}</p>
                    </div>
                    <div style='text-align: center; margin: 20px 0;'>
                        <p style='margin-bottom: 10px; font-weight: bold;'>Mã QR Check-in của bạn (Vui lòng không chia sẻ):</p>
                        <img src='cid:qrCodeImage' alt='QR Code' style='width: 200px; height: 200px; border: 1px solid #ccc; border-radius: 5px; padding: 5px;' />
                    </div>
                    <p>Vui lòng đưa mã QR này cho ban tổ chức tại quầy check-in khi bạn đến sự kiện.</p>
                    <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;' />
                    <p style='font-size: 12px; color: gray; text-align: center;'>Đây là email tự động từ hệ thống AEMS, vui lòng không trả lời.</p>
                </div>";

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

            // Add the QR code image as an inline linked resource
            if (!string.IsNullOrEmpty(qrCodeBase64))
            {
                byte[] imageBytes = Convert.FromBase64String(qrCodeBase64);
                var ms = new MemoryStream(imageBytes);
                
                var view = global::System.Net.Mail.AlternateView.CreateAlternateViewFromString(body, null, "text/html");
                var linkedResource = new global::System.Net.Mail.LinkedResource(ms, "image/png")
                {
                    ContentId = "qrCodeImage",
                    TransferEncoding = global::System.Net.Mime.TransferEncoding.Base64
                };
                view.LinkedResources.Add(linkedResource);
                
                // Clear the default body to use the AlternateView instead
                mailMessage.Body = string.Empty;
                mailMessage.AlternateViews.Add(view);
            }

            await client.SendMailAsync(mailMessage);
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            var smtpHost = _configuration["Email:SmtpHost"];
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var smtpUser = _configuration["Email:SmtpUser"];
            var smtpPass = _configuration["Email:SmtpPass"];
            var fromEmail = _configuration["Email:FromEmail"] ?? smtpUser;
            var fromName = _configuration["Email:FromName"] ?? "AEMS System";

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };

            var mail = new MailMessage
            {
                From = new MailAddress(fromEmail!, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            mail.To.Add(toEmail);

            await client.SendMailAsync(mail);
        }
    }
}
