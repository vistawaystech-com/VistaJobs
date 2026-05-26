namespace JBP.Models
{
    public class UanVerificationResult
    {
        public string Uan { get; set; } = string.Empty;

        public bool Verified { get; set; }

        public List<EmploymentHistory> EmploymentHistory { get; set; } = new();

        public string Message { get; set; } = string.Empty;
    }

}
