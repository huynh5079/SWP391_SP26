using AEMS.Test.Helper;
using BusinessLogic.DTOs.Authentication.Register;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using Xunit;

namespace AEMS.Test.TestData.Register
{
	public class RegisterUITest
	{
		[Fact]
		public void Register_WithFirstJsonRecord_Succeeds_AndSecondJsonRecord_ShowsDuplicateEmailError()
		{
			var firstStudent = TestDataLoader.LoadStudentRegisterRequest("users.json", 0);
			var secondStudent = TestDataLoader.LoadStudentRegisterRequest("users.json", 1);
			const string registerUrl = "https://localhost:7149/Auth/RegisterStudent";

			using IWebDriver driver = new ChromeDriver();
			driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);

			WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

			RegisterStudent(driver, registerUrl, firstStudent);

			wait.Until(d => d.Url.Contains("/Auth/Login"));

			Assert.Contains("/Auth/Login", driver.Url, StringComparison.OrdinalIgnoreCase);

			RegisterStudent(driver, registerUrl, secondStudent);
			Assert.Contains("/Auth/RegisterStudent", driver.Url, StringComparison.OrdinalIgnoreCase);
			Assert.Contains("Email đã tồn tại trong hệ thống.", driver.PageSource);
		}

		private static void RegisterStudent(IWebDriver driver, string registerUrl, RegisterStudentRequestDto student)
		{
			driver.Navigate().GoToUrl(registerUrl);

			var email = driver.FindElement(By.Id("Email"));
			email.Clear();
			email.SendKeys(student.Email);
			TryWaitForValue(driver, email, student.Email, TimeSpan.FromSeconds(2));

			var password = driver.FindElement(By.Id("Password"));
			password.Clear();
			password.SendKeys(student.Password);
			TryWaitForValue(driver, password, student.Password, TimeSpan.FromSeconds(2));

			var confirm = driver.FindElement(By.Id("ConfirmPassword"));
			confirm.Clear();
			confirm.SendKeys(student.Password);
			TryWaitForValue(driver, confirm, student.Password, TimeSpan.FromSeconds(2));

			var fullName = driver.FindElement(By.Id("FullName"));
			fullName.Clear();
			fullName.SendKeys(student.FullName);
			TryWaitForValue(driver, fullName, student.FullName, TimeSpan.FromSeconds(2));
			fullName.SendKeys(Keys.Tab);   // extra tab to trigger client validation

			var studentCode = driver.FindElement(By.Id("StudentCode"));
			studentCode.Clear();
			studentCode.SendKeys(student.StudentCode);
			TryWaitForValue(driver, studentCode, student.StudentCode, TimeSpan.FromSeconds(2));
			studentCode.SendKeys(Keys.Tab);
			var phone = driver.FindElement(By.Id("Phone"));
			phone.Clear();
			phone.SendKeys(student.Phone ?? "");
			TryWaitForValue(driver, phone, student.Phone ?? string.Empty, TimeSpan.FromSeconds(2));
			phone.SendKeys(Keys.Tab);

			// Select Major (try by value first, fall back to visible text)
			if (!string.IsNullOrEmpty(student.Major))
			{
				var majorElement = driver.FindElement(By.Id("Major"));
				var select = new SelectElement(majorElement);
				try
				{
					select.SelectByValue(student.Major);
				}
				catch
				{
					try { select.SelectByText(student.Major); } catch { }
				}
				// trigger change event so client-side scripts notice the update
				((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].dispatchEvent(new Event('change', { bubbles: true }));", majorElement);
				// wait until selected option matches
                try
                {
                    var waitSelect = new WebDriverWait(driver, TimeSpan.FromSeconds(2));
                    waitSelect.Until(d => new SelectElement(majorElement).SelectedOption != null && !string.IsNullOrEmpty(new SelectElement(majorElement).SelectedOption.GetAttribute("value")));
                }
				catch { }
			}

            // Select Gender radio button (Gender enum values are Male / Female)
			if (student.Gender is not null)
			{
				var genderStr = student.Gender.ToString();
				var genderId = string.Equals(genderStr, "Male", StringComparison.OrdinalIgnoreCase) ? "genderMale" : "genderFemale";
				var genderEl = driver.FindElement(By.Id(genderId));
				var waitClickable = new WebDriverWait(driver, TimeSpan.FromSeconds(2));
				waitClickable.Until(d => genderEl.Displayed && genderEl.Enabled);
				genderEl.Click();
				((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].dispatchEvent(new Event('change', { bubbles: true }));", genderEl);
			}

			var submit = driver.FindElement(By.CssSelector("button[type='submit']"));
			// wait until submit is enabled
			try { new WebDriverWait(driver, TimeSpan.FromSeconds(3)).Until(d => submit.Enabled && submit.Displayed); } catch { }
				submit.Click();
            Console.WriteLine(driver.Url);
            Console.WriteLine(driver.PageSource);
            var postSubmitWait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            postSubmitWait.Until(d => d.Url.Contains("Login") || d.PageSource.Contains("Email đã tồn tại"));
			
		}

		private static void TryWaitForValue(IWebDriver driver, IWebElement el, string expected, TimeSpan timeout)
		{
			try
			{
				var wait = new WebDriverWait(driver, timeout);
				wait.Until(d => el.GetAttribute("value") == expected);
				((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].dispatchEvent(new Event('input', { bubbles: true })); arguments[0].dispatchEvent(new Event('change', { bubbles: true })); arguments[0].blur();", el);
			}
			catch
			{
				// fallback: set value via JS and dispatch events (works when SendKeys is not registered by client scripts)
				try
				{
					((IJavaScriptExecutor)driver).ExecuteScript(
						"arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('input', { bubbles: true })); arguments[0].dispatchEvent(new Event('change', { bubbles: true })); arguments[0].blur();",
						el, expected);
				}
				catch { }
			}
		}
	}
}
