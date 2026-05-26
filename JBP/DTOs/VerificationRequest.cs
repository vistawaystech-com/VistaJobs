namespace JBP.DTOs
{
    // Shared request body for Aadhaar, PAN, and UAN verification endpoints.
    public class VerificationRequest
    {
        public string? Number { get; set; }
    }
}
