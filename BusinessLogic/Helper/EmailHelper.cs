using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BusinessLogic.Helper
{
    public static class EmailHelper
    {
        // Bỏ dấu Unicode (ví dụ: "Lê Thị" -> "Le Thi")
        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var ch in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    // Chuyển đ -> d, Đ -> D
                    if (ch == 'đ') sb.Append('d');
                    else if (ch == 'Đ') sb.Append('D');
                    else sb.Append(ch);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        // Tạo local-part an toàn cho email: chỉ [a-z0-9], bỏ ký tự khác
        public static string NormalizeEmailLocalPart(string? username)
        {
            if (string.IsNullOrWhiteSpace(username)) return "kid";

            var noDiacritics = RemoveDiacritics(username).ToLowerInvariant();

            // Bỏ khoảng trắng
            noDiacritics = Regex.Replace(noDiacritics, @"\s+", "");

            // Chỉ giữ chữ và số (bạn có thể cho phép dấu chấm nếu muốn)
            var safe = Regex.Replace(noDiacritics, @"[^a-z0-9]+", "");

            if (string.IsNullOrEmpty(safe))
                safe = "kid";

            // Giới hạn độ dài local-part (khuyến nghị < 64 ký tự)
            if (safe.Length > 30) safe = safe.Substring(0, 30);

            return safe;
        }

        public static string GenerateNoEmail(string? username)
        {
            var local = NormalizeEmailLocalPart(username);
            var shortId = Guid.NewGuid().ToString("N")[..8];
            return $"{local}.{shortId}@noemail.tpedu.com";
        }

        public static async Task SendEmailAsync(string toEmail, string subject, string body)
        {
             // TODO: Real implementation with SmtpClient or SendGrid/Mailgun
             // For MVP/Development, we will log to console or debug output to simulate sending.
             // If you have SMTP credentials, add them to appsettings.json and read here.
             
             await Task.Run(() => 
             {
                 Console.WriteLine($"[Email Sent] To: {toEmail}, Subject: {subject}");
                 Console.WriteLine($"Body: {body}");
             });

             /* Real Implementation Example:
             using var client = new System.Net.Mail.SmtpClient("smtp.gmail.com", 587);
             client.EnableSsl = true;
             client.Credentials = new System.Net.NetworkCredential("your-email@gmail.com", "your-app-password");
             
             var mailMessage = new System.Net.Mail.MailMessage
             {
                 From = new System.Net.Mail.MailAddress("noreply@aems.com"),
                 Subject = subject,
                 Body = body,
                 IsBodyHtml = true
             };
             mailMessage.To.Add(toEmail);
             
             await client.SendMailAsync(mailMessage);
             */
        }
    }
}
