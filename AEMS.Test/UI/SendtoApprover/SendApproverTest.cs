using System;
using System.Linq;
using System.Threading;
using AEMS.Test.UI.UnitTest.Login;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace AEMS.Test.UI.SendtoApprover
{
	public class SendApproverTest
	{
		[Fact]
		public void sendtoapprover()
		{
			var users = UserServiceTest.LoadLoginUserFromJson();
			Assert.NotNull(users);
			Assert.NotEmpty(users);

			const string loginUrl = "https://localhost:7149/Auth/Login";

			var firstUser = users[0];
			var thirdUser = users.Length > 2 ? users[2] : users[0];

			using IWebDriver driver = new ChromeDriver();

			driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
			driver.Navigate().GoToUrl(loginUrl);

			// LOGIN USER 1
			driver.FindElement(By.Id("Email")).SendKeys(firstUser.Email);
			driver.FindElement(By.Id("Password")).SendKeys(firstUser.Password);
			driver.FindElement(By.CssSelector("button[type='submit']")).Click();

			Thread.Sleep(2000);

			var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

			wait.Until(d => d.FindElement(By.LinkText("Manage"))).Click();
			wait.Until(d => d.FindElement(By.LinkText("My Events"))).Click();

			bool found = false;

			while (!found)
			{
				var sends = driver.FindElements(By.XPath("//button[contains(text(),'Send')]"));

				foreach (var send in sends)
				{
					try
					{
						((IJavaScriptExecutor)driver)
							.ExecuteScript("arguments[0].scrollIntoView(true);", send);

						((IJavaScriptExecutor)driver)
							.ExecuteScript("arguments[0].click();", send);

						Thread.Sleep(1500);

						try
						{
							var closeBtn = driver.FindElement(
								By.CssSelector(".toast .close, .toast button, .toast .btn-close")
							);

							((IJavaScriptExecutor)driver)
								.ExecuteScript("arguments[0].click();", closeBtn);

							Console.WriteLine("Đã đóng alert");

							Thread.Sleep(2000);

							found = true;
							break;
						}
						catch
						{
							// không có toast thì bỏ qua
						}
					}
					catch
					{
						// lỗi click send thì bỏ qua
					}
				}

				if (found) break;

				var next = driver.FindElements(By.XPath("//a[contains(text(),'Next')]"));

				if (next.Count == 0)
				{
					Console.WriteLine("Không tìm thấy Send");
					break;
				}

				next[0].Click();
			}

			// LOGOUT USER 1
			try
			{
				var waitLogout = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

				// Remove floating alerts/toasts
				try
				{
					((IJavaScriptExecutor)driver).ExecuteScript(
						"document.querySelectorAll('.toast, .alert, [role=\"alert\"]').forEach(e=>e.remove());"
					);
				}
				catch { }

				// mở dropdown profile
				var dropdown = waitLogout.Until(d => d.FindElement(By.Id("userProfileDropdown")));

				((IJavaScriptExecutor)driver)
					.ExecuteScript("arguments[0].click();", dropdown);

				// chờ form logout
				var logoutForm = waitLogout.Until(
					d => d.FindElement(By.CssSelector("form[action*='Logout']"))
				);

				// submit logout
				((IJavaScriptExecutor)driver)
					.ExecuteScript("arguments[0].submit();", logoutForm);

				waitLogout.Until(d => d.Url.Contains("/Auth/Login"));

				Console.WriteLine("Logout success");
			}
			catch (Exception ex)
			{
				Console.WriteLine("Logout failed: " + ex.Message);
			}

			Console.WriteLine("Current URL after logout: " + driver.Url);

			// LOGIN USER 3
			try
			{
				driver.Navigate().GoToUrl(loginUrl);

				wait.Until(d => d.FindElement(By.Id("Email"))).SendKeys(thirdUser.Email);
				driver.FindElement(By.Id("Password")).SendKeys(thirdUser.Password);

				driver.FindElement(By.CssSelector("button[type='submit']")).Click();

				Thread.Sleep(2000);

				Console.WriteLine("Login user 3 success");
			}
			catch (Exception ex)
			{
				Console.WriteLine("Login user 3 failed: " + ex.Message);
			}
			var firstReject = driver.FindElement(By.XPath("(//a[contains(@href,'operation=reject')])[1]"));

			firstReject.Click();
			Thread.Sleep(2000);
			var comments = ApproverComment.LoadComments();

			var wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

			// tìm textarea comment
			var commentBox = wait2.Until(d => d.FindElement(By.Name("comment")));

			// nhập comment đầu tiên từ JSON
			commentBox.SendKeys(comments[0].Comment);

			// submit form
			var form = driver.FindElement(By.CssSelector("form[action*='Reject']"));

			var submitBtn = form.FindElement(By.CssSelector("button[type='submit']"));

			submitBtn.Click();
			Thread.Sleep(2000);
		}
	}

}