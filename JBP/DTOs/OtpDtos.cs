namespace JBP.DTOs
{
    public class OtpRequestDto
    {
        public string Email { get; set; } = string.Empty;

        public string Purpose { get; set; } = string.Empty;
    }

    public class LoginOtpVerifyDto
    {
        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Otp { get; set; } = string.Empty;
    }

    public class ForgotPasswordDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class VerifyResetOtpDto
    {
        public string Email { get; set; } = string.Empty;

        public string Otp { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        public string Email { get; set; } = string.Empty;

        public string Otp { get; set; } = string.Empty;

        public string NewPassword { get; set; } = string.Empty;

        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class GoogleAuthDto
    {
        public string Credential { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
    }
}
