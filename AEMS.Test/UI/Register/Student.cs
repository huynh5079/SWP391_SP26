using AEMS.Test.Helper;
using BusinessLogic.DTOs.Authentication.Register;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit;

namespace AEMS.Test.UI.Register
{
	public class RegisterUITest
	{
		[Fact]
		public void Register_WithFirstJsonRecord_Succeeds_AndSecondJsonRecord_ShowsDuplicateEmailError()
		{
			var firstStudent = TestDataLoader.LoadStudentRegisterRequest("users.json", 0);
			
			const string registerUrl = "https://localhost:7149/Auth/RegisterStudent";

			using IWebDriver driver = new ChromeDriver();
			driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);

			RegisterStudent(driver, registerUrl, firstStudent);
			Assert.Contains("/Auth/Login", driver.Url, StringComparison.OrdinalIgnoreCase);

			
		}

		private static void RegisterStudent(IWebDriver driver, string registerUrl, RegisterStudentRequestDto student)
		{
			driver.Navigate().GoToUrl(registerUrl);

			driver.FindElement(By.Id("Email")).Clear();
			driver.FindElement(By.Id("Email")).SendKeys(student.Email);
			driver.FindElement(By.Id("Password")).Clear();
			driver.FindElement(By.Id("Password")).SendKeys(student.Password);
			driver.FindElement(By.Id("ConfirmPassword")).Clear();
			driver.FindElement(By.Id("ConfirmPassword")).SendKeys(student.Password);
			driver.FindElement(By.Id("FullName")).Clear();
			driver.FindElement(By.Id("FullName")).SendKeys(student.FullName);
			driver.FindElement(By.Id("StudentCode")).Clear();
			driver.FindElement(By.Id("StudentCode")).SendKeys(student.StudentCode);
			driver.FindElement(By.Id("Phone")).Clear();
			driver.FindElement(By.Id("Phone")).SendKeys(student.Phone ?? string.Empty);

			driver.FindElement(By.CssSelector("button[type='submit']")).Click();
		}
	}
}
