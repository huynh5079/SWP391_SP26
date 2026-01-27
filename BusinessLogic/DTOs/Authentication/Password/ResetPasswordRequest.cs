namespace BusinessLogic.DTOs.Authentication.Password
{
    public class ResetPasswordRequest
    {
        public string Token { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }
}
