namespace JBP.Models
{
    // Jobseeker profile row.
    // Email links this profile to the login user; employers search this table for matches.
    public class Candidate
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public DateTime? Dob { get; set; }

        public bool AadhaarVerified { get; set; }

        public bool PanVerified { get; set; }

        public bool UanVerified { get; set; }

        public string? PanNumber { get; set; }

        public string? AadhaarNumber { get; set; }

        public string? UanNumber { get; set; }

        public string? EmploymentHistory { get; set; }

        public int Experience { get; set; }

        // Comma-separated skills entered from the chip UI, for example: "react,sql,aws".
        public string Skills { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string Salary { get; set; } = string.Empty;

        // Expected values: fresher or experienced.
        public string CandidateType { get; set; } = string.Empty;

        // Public file path returned to the frontend, for example: /Uploads/<file>.
        public string ResumePath { get; set; } = string.Empty;
    }
}
