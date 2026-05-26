namespace JBP.Models
{
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

        public string Skills { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string Salary { get; set; } = string.Empty;

        public string CandidateType { get; set; } = string.Empty;

        public string ResumePath { get; set; } = string.Empty;
    }
}