using AEMS_Solution.Controllers.Common;
using AEMS_Solution.Models.Authentication;
using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.DTOs.Authentication.Register;
using BusinessLogic.Service.Interface;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AEMS_Solution.Controllers.Authentication
{
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;
        private readonly ISystemErrorLogService _systemErrorLogService;

        public AuthController(IAuthService authService, ISystemErrorLogService systemErrorLogService)
        {
            _authService = authService;
            _systemErrorLogService = systemErrorLogService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                // Map ViewModel to DTO
                var dto = new LoginRequestDto { Email = model.Email, Password = model.Password };
                var response = await _authService.LoginAsync(dto);

                // Create Claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, response.User.Id),
                    new Claim(ClaimTypes.Name, response.User.FullName),
                    new Claim(ClaimTypes.Email, response.User.Email),
                    new Claim(ClaimTypes.Role, response.User.Role),
                    new Claim("AvatarUrl", response.User.AvatarUrl ?? "")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddDays(7)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                SetNotification($"Chào mừng trở lại, {response.User.FullName}!", "success");

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                // Log error to database for debugging
                await _systemErrorLogService.LogErrorAsync(
                    ex, 
                    CurrentUserId, 
                    $"{nameof(AuthController)}.{nameof(Login)}"
                );
                
                SetNotification(ex.Message, "error");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult RegisterStudent()
        {
            return View(new RegisterStudentViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> RegisterStudent(RegisterStudentViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                // Map ViewModel to DTO
                var dto = new RegisterStudentRequest
                {
                    Email = model.Email,
                    Password = model.Password,
                    FullName = model.FullName,
                    StudentCode = model.StudentCode,
                    Phone = model.Phone,
                    Major = model.Major,
                    Gender = Enum.TryParse<DataAccess.Enum.Gender>(model.Gender, out var gender) ? gender : null
                };

                await _authService.RegisterStudentAsync(dto);
                SetNotification("Đăng ký thành công! Vui lòng đăng nhập.", "success");
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                // Log error to database for debugging
                await _systemErrorLogService.LogErrorAsync(
                    ex, 
                    CurrentUserId, 
                    $"{nameof(AuthController)}.{nameof(RegisterStudent)}"
                );
                
                SetNotification(ex.Message, "error");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            SetNotification("Đăng xuất thành công.", "success");
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            SetNotification("Bạn không có quyền truy cập trang này.", "error");
            return RedirectToAction("Index", "Home");
        }
    }
}
