namespace JBP.Models
{
    public class Job
    {
        public int Id { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Skills { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string Salary { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string CandidateType { get; set; } = string.Empty;

        public int MinExperience { get; set; }
    }
}