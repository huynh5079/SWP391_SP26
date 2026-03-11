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
			var secondStudent = TestDataLoader.LoadStudentRegisterRequest("users.json", 1);
			const string registerUrl = "https://localhost:7149/Auth/RegisterStudent";

			using IWebDriver driver = new ChromeDriver();
			driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);

			// Đăng ký sinh viên đầu tiên
			RegisterStudent(driver, registerUrl, firstStudent);
			// Kiểm tra chuyển hướng đến trang login
			Assert.Contains("/Auth/Login", driver.Url, StringComparison.OrdinalIgnoreCase);

			// Quay lại trang đăng ký
			driver.Navigate().GoToUrl(registerUrl);

			// Đăng ký sinh viên thứ hai
			RegisterStudent(driver, registerUrl, secondStudent);
			// Kiểm tra thông báo lỗi trùng email (cần tìm element chứa thông báo lỗi, ví dụ: div.text-danger)
			var errorElement = driver.FindElement(By.CssSelector(".text-danger"));
			Assert.Contains("Email đã được sử dụng", errorElement.Text); // tùy theo thông báo thực tế
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
