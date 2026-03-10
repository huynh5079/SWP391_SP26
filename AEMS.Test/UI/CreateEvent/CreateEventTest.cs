using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AEMS.Test.UI.UnitTest.Login;
using CloudinaryDotNet.Actions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace AEMS.Test.UI.CreateEvent
{
	public class CreateEventTest
	{
		[Fact]
		public void CreateEvent()
		{
			var users = UserServiceTest.LoadLoginUserFromJson();
			Assert.NotNull(users);
			Assert.NotEmpty(users);
			var firstUser = users[0];
			const string loginUrl = "https://localhost:7149/Auth/Login";
			using IWebDriver driver = new ChromeDriver();

			driver.Navigate().GoToUrl(loginUrl);

			driver.FindElement(By.Id("Email")).SendKeys(firstUser.Email);
			driver.FindElement(By.Id("Password")).SendKeys(firstUser.Password);
			driver.FindElement(By.CssSelector("button[type='submit']")).Click();

			Thread.Sleep(2000);

			var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

			wait.Until(d => d.FindElement(By.LinkText("Create Event"))).Click();
		}
	}
}
