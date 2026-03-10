using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using BusinessLogic.DTOs.Authentication.Login;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace AEMS.Test.UI.UnitTest.Login
{
    public class UserServiceTest
    {
        [Fact]
        public void Login_WithJsonUser_AttemptsLogin()
        {
            var user = LoadLoginUserFromJson();
            Assert.False(string.IsNullOrEmpty(user?.Email));
            Assert.False(string.IsNullOrEmpty(user?.Password));

            const string loginUrl = "https://localhost:7149/Auth/Login";

            using IWebDriver driver = new ChromeDriver();
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);

            driver.Navigate().GoToUrl(loginUrl);

            var email = driver.FindElement(By.Id("Email"));
            email.Clear();
            email.SendKeys(user.Email);

            var password = driver.FindElement(By.Id("Password"));
            password.Clear();
            password.SendKeys(user.Password);

            // submit
            var submit = driver.FindElement(By.CssSelector("button[type='submit']"));
            submit.Click();
            Thread.Sleep(5000);
			// wait for redirect or error
			var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            wait.Until(d => !d.Url.Contains("/Auth/Login") || d.PageSource.Contains("Sai mật khẩu") || d.PageSource.Contains("Không tìm thấy"));

            // Assert: should not remain on login page (successful login redirects)
            Assert.DoesNotContain("/Auth/Login", driver.Url, StringComparison.OrdinalIgnoreCase);
        }

        private static LoginRequestDto? LoadLoginUserFromJson()
        {
            var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "Login", "LoginUser.json"));
            if (!File.Exists(path)) throw new FileNotFoundException("LoginUser.json not found", path);

            var text = File.ReadAllText(path);
            try
            {
                // Try parse as array or single object
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                if (text.TrimStart().StartsWith("["))
                {
                    var arr = JsonSerializer.Deserialize<LoginRequestDto[]>(text, opts);
                    if (arr != null && arr.Length > 0) return arr[0];
                }
                else
                {
                    var obj = JsonSerializer.Deserialize<LoginRequestDto>(text, opts);
                    if (obj != null) return obj;
                }
            }
            catch { /* ignore and fallback */ }

            // Fallback: try to extract Email and Password with regex from malformed JSON
            try
            {
                var emailMatch = Regex.Match(text, "\\\"?Email\\\"?\\s*[:=]\\s*\\\"(?<email>[^\\\"]+)\\\"", RegexOptions.IgnoreCase);
                var passMatch = Regex.Match(text, "\\\"?Password\\\"?\"\\s*[:=]\\s*\\\"(?<pass>[^\\\"]+)\\\"", RegexOptions.IgnoreCase);
                var dto = new LoginRequestDto();
                if (emailMatch.Success) dto.Email = emailMatch.Groups["email"].Value.Trim();
                if (passMatch.Success) dto.Password = passMatch.Groups["pass"].Value.Trim();

                if (!string.IsNullOrEmpty(dto.Email) && !string.IsNullOrEmpty(dto.Password)) return dto;
            }
            catch { }

            return null;
        }
    }
}
