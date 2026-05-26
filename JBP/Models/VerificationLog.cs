namespace JBP.Models
{
    public class VerificationLog
    {
        public int Id { get; set; }

        public int? CandidateId { get; set; }

        public Candidate? Candidate { get; set; }

        public string DocumentType { get; set; } = string.Empty;

        public string Provider { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string? RawResponse { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
