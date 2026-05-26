namespace JBP.Models
{
    public class EmploymentHistory
    {
        public int Id { get; set; }

        public int CandidateId { get; set; }

        public Candidate? Candidate { get; set; }

        public string Uan { get; set; } = string.Empty;

        public string Company { get; set; } = string.Empty;

        public string Doj { get; set; } = string.Empty;

        public string Doe { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
