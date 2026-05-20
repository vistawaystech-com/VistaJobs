namespace JBP.Models
{
    public class JobApplication
    {
        public int Id { get; set; }

        public int JobId { get; set; }

        public string JobTitle { get; set; } = string.Empty;

        public string CandidateEmail { get; set; } = string.Empty;

        public string CandidateName { get; set; } = string.Empty;

        public DateTime AppliedAt { get; set; }
    }
}