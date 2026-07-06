namespace JBP.Models
{
    // Verified employer account details captured during employer registration.
    public class EmployerProfile
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public string OfficialEmail { get; set; } = string.Empty;

        public string GstNumber { get; set; } = string.Empty;

        public string CinNumber { get; set; } = string.Empty;

        public string Website { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
    }
}
