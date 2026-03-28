using AEMS_Solution.Controllers.Common;
using AEMS_Solution.Models.Authentication;
using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.DTOs.Authentication.Password;
using BusinessLogic.DTOs.Authentication.Register;
using BusinessLogic.Service.Auth;
using BusinessLogic.Service.System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using DataAccess.Enum;
using System.Security.Claims;

using Microsoft.Extensions.DependencyInjection;
using DataAccess.Enum;

namespace AEMS_Solution.Controllers.Authentication
{
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        private ISystemErrorLogService _systemErrorLogService => HttpContext.RequestServices.GetRequiredService<ISystemErrorLogService>();

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
                var claims = new List<System.Security.Claims.Claim>
                {
                    new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, response.User.Id),
                    new System.Security.Claims.Claim(ClaimTypes.Name, response.User.FullName),
                    new System.Security.Claims.Claim(ClaimTypes.Email, response.User.Email),
                    new System.Security.Claims.Claim(ClaimTypes.Role, response.User.Role),
                    new System.Security.Claims.Claim("AvatarUrl", response.User.AvatarUrl ?? "")
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

                await LogUserActivity(UserActionType.Login, response.User.Id, TargetType.User, $"User {response.User.Email} logged in.");
                SetSuccess($"Chào mừng trở lại, {response.User.FullName}!");

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                return RedirectToDashboard(response.User.Role);
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult LoginGoogle(string returnUrl = "/")
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleResponse)),
                Items = { { "returnUrl", returnUrl } }
            };
            return Challenge(properties, Microsoft.AspNetCore.Authentication.Google.GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(Microsoft.AspNetCore.Authentication.Google.GoogleDefaults.AuthenticationScheme);

            if (!result.Succeeded)
            {
                SetError("Đăng nhập Google thất bại.");
                return RedirectToAction(nameof(Login));
            }

            try
            {
                var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
                var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                var googleId = claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                var avatarUrl = claims?.FirstOrDefault(c => c.Type == "picture")?.Value ?? "";

                if (string.IsNullOrEmpty(email))
                {
                    SetError("Không thể lấy email từ Google.");
                    return RedirectToAction(nameof(Login));
                }

                // Get returnUrl from properties if preserved, otherwise default
                var returnUrl = result.Properties?.Items.ContainsKey("returnUrl") == true 
                    ? result.Properties.Items["returnUrl"] 
                    : "/";

                var user = await _authService.LoginGoogleAsync(email, googleId, name ?? "Unknown", avatarUrl);

                // Create Cookie Claims
                var userClaims = new List<System.Security.Claims.Claim>
                {
                    new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, user.Id),
                    new System.Security.Claims.Claim(ClaimTypes.Name, user.FullName),
                    new System.Security.Claims.Claim(ClaimTypes.Email, user.Email),
                    new System.Security.Claims.Claim(ClaimTypes.Role, user.Role.RoleName.ToString() ?? ""),
                    new System.Security.Claims.Claim("AvatarUrl", user.AvatarUrl ?? "")
                };

                var claimsIdentity = new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true, // Google login is typically persistent
                    ExpiresUtc = DateTime.UtcNow.AddDays(7)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                await LogUserActivity(UserActionType.Login, user.Id, TargetType.User, $"User {user.Email} logged in via Google.");
                SetSuccess($"Chào mừng, {user.FullName}!");
                
                 if (Url.IsLocalUrl(returnUrl) && returnUrl != "/")
                {
                    return Redirect(returnUrl);
                }
                return RedirectToDashboard(user.Role.RoleName.ToString() ?? "");
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, ex.Message);
                return RedirectToAction(nameof(Login));
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
                var dto = new RegisterStudentRequestDto
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
                await ExecuteSuccessAsync("Đăng ký thành công! Vui lòng đăng nhập.", UserActionType.AccountRegister, model.Email, TargetType.User);
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult RegisterOrganizer()
        {
            return View(new RegisterOrganizerViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> RegisterOrganizer(RegisterOrganizerViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var dto = new BusinessLogic.DTOs.Authentication.Register.RegisterStaffRequestDto
                {
                    Email = model.Email,
                    Password = model.Password,
                    FullName = model.FullName,
                    StaffCode = model.StaffCode,
                    Phone = model.Phone,
                    Position = model.Position,
                    RoleName = "Organizer" // Explicitly explicitly set for RegisterOrganizer
                };

                await _authService.RegisterStaffAsync(dto);
                await ExecuteSuccessAsync("Đăng ký thành công! Vui lòng đăng nhập.", UserActionType.AccountRegister, model.Email, TargetType.User);
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await ExecuteSuccessAsync("Đăng xuất thành công.", UserActionType.Logout, CurrentUserId ?? "Unknown", TargetType.User);
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            SetError("Bạn không có quyền truy cập trang này.");
            return RedirectToAction("Index", "Home");
        }
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var req = new BusinessLogic.DTOs.Authentication.Password.ForgotPasswordRequestDto { Email = model.Email };
                await _authService.ForgotPasswordAsync(req);
                // Always show success message for security
                await ExecuteSuccessAsync("Nếu email tồn tại trong hệ thống, chúng tôi đã gửi hướng dẫn đặt lại mật khẩu của bạn.", UserActionType.Sync, model.Email, TargetType.User);
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                SetError("Liên kết đặt lại mật khẩu không hợp lệ.");
                return RedirectToAction(nameof(Login));
            }
            return View(new ResetPasswordViewModel { Token = token, Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
             if (!ModelState.IsValid) return View(model);

            try
            {
                var req = new BusinessLogic.DTOs.Authentication.Password.ResetPasswordRequestDto
                {
                    Token = model.Token,
                    NewPassword = model.NewPassword
                };

                await _authService.ResetPasswordAsync(req);
                await ExecuteSuccessAsync("Đặt lại mật khẩu thành công! Vui lòng đăng nhập.", UserActionType.ChangePassword, model.Email, TargetType.User);
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult SetPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var req = new SetPasswordRequestDto
                {
                    NewPassword = model.NewPassword,
                    ConfirmNewPassword = model.ConfirmNewPassword
                };

                await _authService.SetPasswordAsync(CurrentUserId, req);
                await ExecuteSuccessAsync("Thiết lập mật khẩu thành công!", UserActionType.ChangePassword, CurrentUserId, TargetType.User);
                return RedirectToAction("Index", "Profile");
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var req = new ChangePasswordRequestDto
                {
                    OldPassword = model.OldPassword,
                    NewPassword = model.NewPassword,
                    ConfirmNewPassword = model.ConfirmNewPassword
                };

                await _authService.ChangePasswordAsync(CurrentUserId, req);
                await ExecuteSuccessAsync("Đổi mật khẩu thành công!", UserActionType.ChangePassword, CurrentUserId, TargetType.User);
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, ex.Message);
                return View(model);
            }
        }

        private IActionResult RedirectToDashboard(string role)
        {
            return RedirectToAction("Index", "Home");
        }
    }
}
