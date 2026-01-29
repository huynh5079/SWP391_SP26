namespace BusinessLogic.DTOs.Authentication.Password
{
    public class ResetPasswordRequestDto
    {
        public string Token { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }
}
