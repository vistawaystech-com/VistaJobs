namespace JBP.DTOs
{
    // Request body for /api/Auth/register.
    // Role decides where the user lands after login.
    public class RegisterDto
    {
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Otp { get; set; } = string.Empty;

        // Expected values: jobseeker, employer, admin.
        public string Role { get; set; } = string.Empty;
    }
}
