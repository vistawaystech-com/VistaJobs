namespace JBP.Models
{
    // Employer-posted job requirement.
    // Frontend uses Skills/CandidateType/MinExperience to match candidate profiles.
    public class Job
    {
        public int Id { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        // Comma-separated required skills from employer chip input.
        public string Skills { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string Salary { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        // Expected values: fresher, experienced, or both.
        public string CandidateType { get; set; } = string.Empty;

        public int MinExperience { get; set; }
    }
}
