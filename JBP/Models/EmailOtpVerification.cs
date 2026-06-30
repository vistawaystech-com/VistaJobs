namespace JBP.Models
{
    public class EmailOtpVerification
    {
        public int Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public string CodeHash { get; set; } = string.Empty;

        public string Purpose { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
