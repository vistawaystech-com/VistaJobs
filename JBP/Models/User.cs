namespace JBP.Models
{
    // Login account row. Role controls which dashboard the frontend opens after login.
    public class User
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        // BCrypt hash from AuthController.Register; never store plain text passwords.
        public string Password { get; set; } = string.Empty;

        // Expected values: admin, employer, jobseeker.
        public string Role { get; set; } = string.Empty;
    }
}
