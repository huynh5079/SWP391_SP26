using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BusinessLogic.Service.Interface;
using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.DTOs.Authentication.Register;

namespace AEMS_Solution.Controllers
{
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginRequestDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            try
            {
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
                    IsPersistent = true, // Remember me logic can be added here
                    ExpiresUtc = DateTime.UtcNow.AddDays(7)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                NotifySuccess("Đăng nhập thành công!");
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                NotifyError(ex.Message);
                return View(dto);
            }
        }

        [HttpGet]
        public IActionResult RegisterStudent()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RegisterStudent(RegisterStudentRequest dto)
        {
            if (!ModelState.IsValid) return View(dto);

            try
            {
                await _authService.RegisterStudentAsync(dto);
                NotifySuccess("Đăng ký thành công! Vui lòng đăng nhập.");
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                NotifyError(ex.Message);
                return View(dto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            NotifySuccess("Đăng xuất thành công.");
            return RedirectToAction(nameof(Login));
        }
    }
}
