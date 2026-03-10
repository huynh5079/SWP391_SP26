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
        public void Login_MultipleUsers_LogoutBetweenLogins()
        {
            var users = LoadLoginUserFromJson();
            Assert.NotNull(users);
            Assert.NotEmpty(users);

            const string loginUrl = "https://localhost:7149/Auth/Login";

            using IWebDriver driver = new ChromeDriver();
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);

            foreach (var user in users)
            {
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

                // wait for login to complete (redirect or user dropdown appears)
                var postWait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                postWait.Until(d => !d.Url.Contains("/Auth/Login") || d.FindElements(By.Id("userProfileDropdown")).Count > 0);

                // Assert logged in
                Assert.False(driver.Url.Contains("/Auth/Login", StringComparison.OrdinalIgnoreCase));

                // Perform logout via UI: open user dropdown then submit logout form
                try
                {
                    var dropdown = driver.FindElement(By.Id("userProfileDropdown"));
                    dropdown.Click();
                    // wait for logout button inside form
                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
                    wait.Until(d => d.FindElements(By.CssSelector("form[action='/Auth/Logout'] button[type='submit']")).Count > 0);
                    var logoutBtn = driver.FindElement(By.CssSelector("form[action='/Auth/Logout'] button[type='submit']"));
                    logoutBtn.Click();
                    // wait for redirect back to login
                    var waitLogin = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                    waitLogin.Until(d => d.Url.Contains("/Auth/Login") || d.FindElements(By.CssSelector("a[asp-action='Login'], a[href*='/Auth/Login']")).Count > 0);
                }
                catch
                {
                    // fallback: submit logout form by JS if UI click fails
                    try
                    {
                        ((IJavaScriptExecutor)driver).ExecuteScript("var f = document.querySelector('form[action=\"/Auth/Logout\"]'); if(f) f.submit();");
                        var waitLogin2 = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                        waitLogin2.Until(d => d.Url.Contains("/Auth/Login"));
                    }
                    catch
                    {
                        // ignore; test will fail on next iteration if not logged out
                    }
                }
            }

            driver.Quit();
        }

        private static LoginRequestDto[] LoadLoginUserFromJson()
        {
            // try multiple possible locations (support Login and Login/LoginOrganizer folders)
            var candidatePaths = new[]
            {
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "Login", "LoginOrganizer", "LoginUser.json")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "Login", "LoginUser.json")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "LoginUser.json"))
            };

            string? path = null;
            foreach (var p in candidatePaths)
            {
                if (File.Exists(p))
                {
                    path = p;
                    break;
                }
            }

            if (path == null)
            {
                throw new FileNotFoundException("LoginUser.json not found. Tried paths: " + string.Join("; ", candidatePaths));
            }

            var text = File.ReadAllText(path);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                // parse as array
                var arr = JsonSerializer.Deserialize<LoginRequestDto[]>(text, opts);
                if (arr != null && arr.Length > 0) return arr;
            }
            catch { /* ignore and fallback */ }

            try
            {
                // parse single object
                var obj = JsonSerializer.Deserialize<LoginRequestDto>(text, opts);
                if (obj != null) return new[] { obj };
            }
            catch { /* ignore */ }

            // Fallback: try to extract Email and Password with regex from malformed JSON
            try
            {
                var emailMatch = Regex.Match(text, "\"?Email\"?\\s*[:=]\\s*\"(?<email>[^\"]+)\"", RegexOptions.IgnoreCase);
                var passMatch = Regex.Match(text, "\"?Password\"?\\s*[:=]\\s*\"(?<pass>[^\"]+)\"", RegexOptions.IgnoreCase);
                var dto = new LoginRequestDto();
                if (emailMatch.Success) dto.Email = emailMatch.Groups["email"].Value.Trim();
                if (passMatch.Success) dto.Password = passMatch.Groups["pass"].Value.Trim();

                if (!string.IsNullOrEmpty(dto.Email) && !string.IsNullOrEmpty(dto.Password)) return new[] { dto };
            }
            catch { }

            throw new InvalidDataException("Unable to parse login users from JSON. Tried: " + path);
        }
    }
}
