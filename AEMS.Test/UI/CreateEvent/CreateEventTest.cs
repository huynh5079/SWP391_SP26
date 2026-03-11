using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace AEMS.Test.UI.CreateEvent
{
	public class CreateEventTest
	{
		private const string BaseUrl = "https://localhost:7149";
		private const string LoginUrl = BaseUrl + "/Auth/Login";
		private const string MyEventsUrl = BaseUrl + "/Organizer/MyEvents";

		[Fact]
		public void CreateEvent_ShouldProcessAllTestCases()
		{
			// Load user from JSON (lấy user đầu tiên)
			var users = LoadLoginUsers();
			Assert.NotNull(users);
			Assert.NotEmpty(users);
			var user = users[0];
			Console.WriteLine($"🔑 Sử dụng tài khoản: {user.Email}");

			// Chrome options
			var options = new ChromeOptions();
			//options.AddArgument("--incognito");
			options.AddArgument("--ignore-certificate-errors");
			options.AddArgument("--allow-insecure-localhost");
			options.AddArgument("--disable-web-security");
			options.AddArgument("--start-maximized");
			options.AddArgument("--disable-features=VizDisplayCompositor");
			options.AddArgument("--disable-gpu");

			using var driver = new ChromeDriver(options);
			driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
			//driver.Manage().Cookies.DeleteAllCookies();

			// Đăng nhập
			EnsureLoggedIn(driver, user);

			// Điều hướng đến My Events
			driver.Navigate().GoToUrl(MyEventsUrl);
			WaitForPageLoad(driver);

			// Đọc test cases
			var testCases = LoadEventTestData();
			Assert.NotEmpty(testCases);

			int total = testCases.Count;
			int success = 0, failed = 0;

			for (int i = 0; i < total; i++)
			{
				var ev = testCases[i];
				Console.WriteLine($"\n========== TEST CASE {i + 1}/{total}: {ev.Title} ==========");

				try
				{
					// Đảm bảo đang ở trang My Events và đã login
					EnsureOnMyEvents(driver, user);

					// Click "Create Event"
					ClickCreateEventButton(driver);

					// Chờ form load
					WaitForElement(driver, By.Id("Title"), 10);

					// Điền form
					FillEventForm(driver, ev);

					// Submit và chờ kết quả
					bool isSuccess = SubmitAndWaitForResult(driver, ev.Title);

					if (isSuccess)
					{
						Console.WriteLine($"✅ TEST CASE {i + 1}: THÀNH CÔNG");
						success++;
					}
					else
					{
						Console.WriteLine($"❌ TEST CASE {i + 1}: THẤT BẠI");
						TakeScreenshot(driver, $"FAIL_CASE_{i + 1}");
						failed++;
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"❌ LỖI TEST CASE {i + 1}: {ex.Message}");
					TakeScreenshot(driver, $"ERROR_CASE_{i + 1}");
					failed++;
				}

				// Sau mỗi case, quay về MyEvents (nếu chưa)
				GoToMyEventsDirect(driver, user);
				Thread.Sleep(1000);
			}

			Console.WriteLine($"\n========== KẾT QUẢ: {success}/{total} THÀNH CÔNG, {failed} THẤT BẠI ==========");
		}

		// ==================== HÀM HỖ TRỢ CHÍNH ====================

		/// <summary>
		/// Đảm bảo đã đăng nhập, nếu chưa thì login.
		/// </summary>
		private void EnsureLoggedIn(IWebDriver driver, UserTestData user)
		{
			if (driver.Url.Contains("/Auth/Login") || driver.PageSource.Contains("Đăng nhập"))
			{
				Console.WriteLine("🔐 Đang đăng nhập...");
				driver.Navigate().GoToUrl(LoginUrl);
				WaitForElement(driver, By.Id("Email"));

				driver.FindElement(By.Id("Email")).SendKeys(user.Email);
				driver.FindElement(By.Id("Password")).SendKeys(user.Password);
				driver.FindElement(By.CssSelector("button[type='submit']")).Click();

				// Chờ qua trang login
				new WebDriverWait(driver, TimeSpan.FromSeconds(10))
					.Until(d => !d.Url.Contains("/Auth/Login"));
				Console.WriteLine("✅ Đăng nhập thành công");
				Thread.Sleep(1000);
			}
		}

		/// <summary>
		/// Đảm bảo đang ở trang MyEvents, nếu không thì điều hướng.
		/// </summary>
		private void EnsureOnMyEvents(IWebDriver driver, UserTestData user)
		{
			EnsureLoggedIn(driver, user);
			if (!driver.Url.Contains("/Organizer/MyEvents"))
			{
				driver.Navigate().GoToUrl(MyEventsUrl);
				WaitForPageLoad(driver);
			}
		}

		/// <summary>
		/// Click nút Create Event trên trang MyEvents.
		/// </summary>
		private void ClickCreateEventButton(IWebDriver driver)
		{
			var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
			var btn = wait.Until(d => d.FindElement(By.LinkText("Create Event")));
			((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", btn);
			Thread.Sleep(500);
			btn.Click();
			Thread.Sleep(1000);
		}

		/// <summary>
		/// Điền toàn bộ form tạo event.
		/// </summary>
		private void FillEventForm(IWebDriver driver, EventTestData ev)
		{
			Console.WriteLine("📝 Điền thông tin event...");

			// Title (bắt buộc)
			SetText(driver, "Title", ev.Title);

			// Description
			if (!string.IsNullOrWhiteSpace(ev.Description))
				SetText(driver, "Description", ev.Description);

			// Semester
			if (!string.IsNullOrWhiteSpace(ev.SemesterName))
				SelectDropdownByText(driver, "SemesterId", ev.SemesterName);

			// Department
			if (!string.IsNullOrWhiteSpace(ev.DepartmentName))
				SelectDropdownByText(driver, "DepartmentId", ev.DepartmentName);

			// Topic
			if (!string.IsNullOrWhiteSpace(ev.TopicName))
				SelectDropdownByText(driver, "TopicId", ev.TopicName);

			// Location
			if (!string.IsNullOrWhiteSpace(ev.Location))
				SelectDropdownByText(driver, "LocationId", ev.Location);

			// StartTime / EndTime
			if (!string.IsNullOrWhiteSpace(ev.StartTime))
				SetDateTime(driver, "StartTime", ev.StartTime);
			if (!string.IsNullOrWhiteSpace(ev.EndTime))
				SetDateTime(driver, "EndTime", ev.EndTime);

			// Capacity & Thumbnail
			if (!string.IsNullOrWhiteSpace(ev.MaxCapacity))
				SetText(driver, "MaxCapacity", ev.MaxCapacity);
			if (!string.IsNullOrWhiteSpace(ev.ThumbnailUrl))
				SetText(driver, "ThumbnailUrl", ev.ThumbnailUrl);

			// Type, Status, Mode
			if (!string.IsNullOrWhiteSpace(ev.Type))
				SelectDropdownByValue(driver, "Type", ev.Type);
			if (!string.IsNullOrWhiteSpace(ev.Status))
				SelectDropdownByValue(driver, "Status", ev.Status);
			if (!string.IsNullOrWhiteSpace(ev.Mode))
				SelectDropdownByValue(driver, "Mode", ev.Mode);

			// MeetingUrl (nếu mode online/hybrid)
			if (ev.Mode?.ToLower() == "online" || ev.Mode?.ToLower() == "hybrid")
			{
				if (!string.IsNullOrWhiteSpace(ev.MeetingUrl))
					SetText(driver, "MeetingUrl", ev.MeetingUrl);
			}

			// Deposit
			if (ev.IsDepositRequired == true)
			{
				ClickCheckbox(driver, "IsDepositRequired");
				if (ev.DepositAmount.HasValue)
					SetText(driver, "DepositAmount", ev.DepositAmount.Value.ToString());
			}

			// Documents
			if (ev.Documents != null && ev.Documents.Any())
				AddDocuments(driver, ev.Documents);

			// Agendas (chỉ cần 2-3 agenda là được, nhưng code vẫn xử lý tất cả)
			if (ev.Agendas != null && ev.Agendas.Any())
				AddAgendas(driver, ev.Agendas);

			Console.WriteLine("✅ Điền form hoàn tất");
		}

		/// <summary>
		/// Submit form và chờ kết quả, trả về true nếu thành công (chuyển về MyEvents).
		/// </summary>
		private bool SubmitAndWaitForResult(IWebDriver driver, string eventTitle)
		{
			Console.WriteLine("🖱️ Submit form...");
			var submitBtn = driver.FindElement(By.CssSelector("button[type='submit']"));
			((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", submitBtn);
			Thread.Sleep(500);

			// Đợi nút có thể click
			var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
			wait.Until(d => submitBtn.Displayed && submitBtn.Enabled);

			// Click bằng JavaScript
			((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", submitBtn);
			Console.WriteLine("✅ Đã click nút Create Event");

			// Chờ chuyển trang (tối đa 15 giây)
			try
			{
				wait.Until(d => d.Url.Contains("/Organizer/MyEvents") || d.Url.Contains("/Auth/Login"));
			}
			catch (WebDriverTimeoutException)
			{
				// Nếu không chuyển trang, có thể ở lại form do lỗi validation
				return false;
			}

			// Nếu bị redirect về login -> session mất
			if (driver.Url.Contains("/Auth/Login"))
			{
				Console.WriteLine("⚠️ Bị redirect về login (session hết)");
				return false;
			}

			// Kiểm tra xem có event trong danh sách không (tuỳ chọn)
			return driver.Url.Contains("/Organizer/MyEvents");
		}

		/// <summary>
		/// Quay về trang MyEvents (nếu chưa) và đảm bảo login.
		/// </summary>
		private void GoToMyEventsDirect(IWebDriver driver, UserTestData user)
		{
			EnsureLoggedIn(driver, user);
			driver.Navigate().GoToUrl(MyEventsUrl);
			WaitForPageLoad(driver);
		}

		// ==================== CÁC HÀM TIỆN ÍCH ====================

		private void SetText(IWebDriver driver, string elementId, string value)
		{
			try
			{
				var el = driver.FindElement(By.Id(elementId));
				el.Clear();
				el.SendKeys(value);
				Console.WriteLine($"  - {elementId}: {value}");
			}
			catch (NoSuchElementException)
			{
				Console.WriteLine($"  ⚠️ Không tìm thấy {elementId}");
			}
		}

		private void SetDateTime(IWebDriver driver, string elementId, string dateTimeString)
		{
			try
			{
				var converted = ConvertToDateTimeLocal(dateTimeString);
				var el = driver.FindElement(By.Id(elementId));
				((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].value = arguments[1];", el, converted);
				Console.WriteLine($"  - {elementId}: {converted}");
			}
			catch { }
		}

		private void SelectDropdownByText(IWebDriver driver, string elementId, string text)
		{
			try
			{
				var select = new SelectElement(driver.FindElement(By.Id(elementId)));
				select.SelectByText(text);
				Console.WriteLine($"  - {elementId}: chọn '{text}'");
			}
			catch { }
		}

		private void SelectDropdownByValue(IWebDriver driver, string elementId, string value)
		{
			try
			{
				var select = new SelectElement(driver.FindElement(By.Id(elementId)));
				select.SelectByValue(value);
				Console.WriteLine($"  - {elementId}: chọn value '{value}'");
			}
			catch { }
		}

		private void ClickCheckbox(IWebDriver driver, string elementId)
		{
			try
			{
				var el = driver.FindElement(By.Id(elementId));
				if (!el.Selected)
				{
					((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", el);
					Console.WriteLine($"  - Checkbox {elementId} đã bật");
					Thread.Sleep(500);
				}
			}
			catch { }
		}

		private void AddDocuments(IWebDriver driver, List<DocumentTestData> docs)
		{
			Console.WriteLine($"  📎 Thêm {docs.Count} documents...");
			for (int i = 0; i < docs.Count; i++)
			{
				var doc = docs[i];
				var addBtn = driver.FindElement(By.Id("btnAddDocument"));
				((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", addBtn);
				Thread.Sleep(500);
				addBtn.Click();
				Thread.Sleep(500);

				var items = driver.FindElements(By.CssSelector("#documentsContainer .agenda-item"));
				var last = items.Last();

				last.FindElement(By.CssSelector("input[name$='.FileName']")).SendKeys(doc.FileName ?? "");
				last.FindElement(By.CssSelector("input[name$='.Type']")).SendKeys(doc.Type ?? "");
				last.FindElement(By.CssSelector("input[name$='.Url']")).SendKeys(doc.Url ?? "");
				Console.WriteLine($"    - Document {i + 1}: {doc.FileName}");
				Thread.Sleep(300);
			}
		}

		private void AddAgendas(IWebDriver driver, List<AgendaTestData> agendas)
		{
			Console.WriteLine($"  📅 Thêm {agendas.Count} agendas...");
			for (int i = 0; i < agendas.Count; i++)
			{
				var agenda = agendas[i];
				var addBtn = driver.FindElement(By.Id("btnAddAgenda"));
				((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", addBtn);
				Thread.Sleep(500);
				addBtn.Click();
				Thread.Sleep(500);

				var items = driver.FindElements(By.CssSelector("#agendaContainer .agenda-item"));
				var last = items.Last();

				last.FindElement(By.CssSelector("input[name$='.SessionName']")).SendKeys(agenda.SessionName ?? "");
				last.FindElement(By.CssSelector("input[name$='.SpeakerInfo']")).SendKeys(agenda.Speaker ?? "");
				last.FindElement(By.CssSelector("textarea[name$='.Description']")).SendKeys(agenda.Description ?? "");

				if (!string.IsNullOrWhiteSpace(agenda.StartTime))
				{
					var start = last.FindElement(By.CssSelector("input[name$='.StartTime']"));
					var startConverted = ConvertToDateTimeLocal(agenda.StartTime);
					((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].value = arguments[1];", start, startConverted);
				}

				if (!string.IsNullOrWhiteSpace(agenda.EndTime))
				{
					var end = last.FindElement(By.CssSelector("input[name$='.EndTime']"));
					var endConverted = ConvertToDateTimeLocal(agenda.EndTime);
					((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].value = arguments[1];", end, endConverted);
				}

				if (!string.IsNullOrWhiteSpace(agenda.Location))
				{
					var select = new SelectElement(last.FindElement(By.CssSelector("select[name$='.Location']")));
					try { select.SelectByText(agenda.Location); } catch { select.SelectByIndex(0); }
				}

				Console.WriteLine($"    - Agenda {i + 1}: {agenda.SessionName}");
				Thread.Sleep(500);
			}
		}

		private string ConvertToDateTimeLocal(string dateTimeString)
		{
			try
			{
				dateTimeString = dateTimeString.Replace("SA", "AM").Replace("CH", "PM");
				if (DateTime.TryParse(dateTimeString, out DateTime dt))
					return dt.ToString("yyyy-MM-ddTHH:mm");
			}
			catch { }
			return dateTimeString;
		}

		private void WaitForElement(IWebDriver driver, By by, int seconds = 10)
		{
			new WebDriverWait(driver, TimeSpan.FromSeconds(seconds)).Until(d => d.FindElement(by).Displayed);
		}

		private void WaitForPageLoad(IWebDriver driver)
		{
			new WebDriverWait(driver, TimeSpan.FromSeconds(10))
				.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
		}

		private void TakeScreenshot(IWebDriver driver, string fileName)
		{
			try
			{
				var screenshot = ((ITakesScreenshot)driver).GetScreenshot();
				var dir = Path.Combine(Directory.GetCurrentDirectory(), "Screenshots");
				Directory.CreateDirectory(dir);
				var path = Path.Combine(dir, $"{fileName}_{DateTime.Now:yyyyMMddHHmmss}.png");
				screenshot.SaveAsFile(path);
				Console.WriteLine($"📸 Screenshot: {path}");
			}
			catch { }
		}

		// ========== ĐỌC DỮ LIỆU TỪ JSON ==========

		private List<UserTestData> LoadLoginUsers(string relativePath = "TestData/Login/LoginOrganizer/LoginUser.json")
		{
			var path = FindFileUpwards(relativePath);
			var json = File.ReadAllText(path);
			// Loại bỏ comment nếu có
			var lines = json.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
				.Where(l => !l.TrimStart().StartsWith("//"));
			var cleanJson = string.Join("\n", lines);

			var options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true,
				ReadCommentHandling = JsonCommentHandling.Skip
			};

			// Đối diện với file JSON có thể là array, nhưng cũng có thể là object lẻ
			try
			{
				return JsonSerializer.Deserialize<List<UserTestData>>(cleanJson, options) ?? new();
			}
			catch (JsonException)
			{
				// Nếu là object đơn
				var single = JsonSerializer.Deserialize<UserTestData>(cleanJson, options);
				return single != null ? new List<UserTestData> { single } : new List<UserTestData>();
			}
		}

		private List<EventTestData> LoadEventTestData(string relativePath = "TestData/Event/CreateEvent.json")
		{
			var path = FindFileUpwards(relativePath);
			var json = File.ReadAllText(path);
			var lines = json.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
				.Where(l => !l.TrimStart().StartsWith("//"));
			var cleanJson = string.Join("\n", lines);

			var options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true,
				ReadCommentHandling = JsonCommentHandling.Skip
			};
			return JsonSerializer.Deserialize<List<EventTestData>>(cleanJson, options) ?? new();
		}

		private string FindFileUpwards(string relativePath)
		{
			var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
			while (dir != null)
			{
				var candidate = Path.Combine(dir.FullName, relativePath);
				if (File.Exists(candidate)) return candidate;
				dir = dir.Parent;
			}
			throw new FileNotFoundException($"Không tìm thấy file {relativePath}");
		}
	}

	// ========== CLASS DỮ LIỆU ==========

	public class UserTestData
	{
		public string Email { get; set; } = "";
		public string Password { get; set; } = "";
	}

	public class EventTestData
	{
		public string Title { get; set; } = "";
		public string? Description { get; set; }
		public string? SemesterName { get; set; }
		public string? DepartmentName { get; set; }
		public string? TopicName { get; set; }
		public string? Location { get; set; }
		public string? StartTime { get; set; }
		public string? EndTime { get; set; }
		public string? MaxCapacity { get; set; }
		public string? ThumbnailUrl { get; set; }
		public string? Type { get; set; }
		public string? Status { get; set; }
		public string? Mode { get; set; }
		public string? MeetingUrl { get; set; }
		public bool? IsDepositRequired { get; set; }
		public decimal? DepositAmount { get; set; }
		public List<AgendaTestData>? Agendas { get; set; }
		public List<DocumentTestData>? Documents { get; set; }
	}

	public class AgendaTestData
	{
		public string? SessionName { get; set; }
		public string? Speaker { get; set; }
		public string? Description { get; set; }
		public string? StartTime { get; set; }
		public string? EndTime { get; set; }
		public string? Location { get; set; }
	}

	public class DocumentTestData
	{
		public string? FileName { get; set; }
		public string? Type { get; set; }
		public string? Url { get; set; }
	}
}