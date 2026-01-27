using AEMS_Solution.Controllers.Common;
using AEMS_Solution.Models.Authentication;
using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.DTOs.Authentication.Password;
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

                SetSuccess($"Chào mừng trở lại, {response.User.FullName}!");

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                return RedirectToDashboard(response.User.Role);
            }
            catch (Exception ex)
            {
                // Log error to database for debugging
                await _systemErrorLogService.LogErrorAsync(
                    ex, 
                    CurrentUserId, 
                    $"{nameof(AuthController)}.{nameof(Login)}"
                );
                
                SetError(ex.Message);
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
                var userClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role.RoleName.ToString() ?? ""),
                    new Claim("AvatarUrl", user.AvatarUrl ?? "")
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

                SetSuccess($"Chào mừng, {user.FullName}!");
                
                 if (Url.IsLocalUrl(returnUrl) && returnUrl != "/")
                {
                    return Redirect(returnUrl);
                }
                return RedirectToDashboard(user.Role.RoleName.ToString() ?? "");
            }
            catch (Exception ex)
            {
                SetError($"Lỗi: {ex.Message}");
                 await _systemErrorLogService.LogErrorAsync(
                    ex, 
                    "System", 
                    $"{nameof(AuthController)}.{nameof(GoogleResponse)}"
                );
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
                SetSuccess("Đăng ký thành công! Vui lòng đăng nhập.");
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
                
                SetError(ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            SetSuccess("Đăng xuất thành công.");
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
                var req = new BusinessLogic.DTOs.Authentication.Password.ForgotPasswordRequest { Email = model.Email };
                await _authService.ForgotPasswordAsync(req);
                // Always show success message for security
                SetSuccess("Nếu email tồn tại trong hệ thống, chúng tôi đã gửi hướng dẫn đặt lại mật khẩu của bạn.");
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
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
                var req = new BusinessLogic.DTOs.Authentication.Password.ResetPasswordRequest
                {
                    Token = model.Token,
                    NewPassword = model.NewPassword
                };

                await _authService.ResetPasswordAsync(req);
                SetSuccess("Đặt lại mật khẩu thành công! Vui lòng đăng nhập.");
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
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
                var req = new ChangePasswordRequest
                {
                    OldPassword = model.OldPassword,
                    NewPassword = model.NewPassword,
                    ConfirmNewPassword = model.ConfirmNewPassword
                };

                await _authService.ChangePasswordAsync(CurrentUserId, req);
                SetSuccess("Đổi mật khẩu thành công!");
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                // If it's a known logic error (Old password mismatch), display it on the field
                if (ex.Message.Contains("Mật khẩu cũ không chính xác"))
                {
                    ModelState.AddModelError("OldPassword", "Mật khẩu cũ không chính xác");
                }
                else
                {
                    SetError(ex.Message);
                }
                return View(model);
            }
        }

        private IActionResult RedirectToDashboard(string role)
        {
            return role switch
            {
                "Student" => RedirectToAction("Index", "Student"),
                "Staff" => RedirectToAction("Index", "Organizer"), // Staff maps to Organizer Dashboard
                "Admin" => RedirectToAction("Index", "Admin"),
                _ => RedirectToAction("Index", "Home") // Default fall back
            };
        }
    }
}
