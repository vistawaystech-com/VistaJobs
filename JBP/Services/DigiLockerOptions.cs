namespace JBP.Services
{
    // Maps the appsettings:DigiLocker section.
    // Keep Enabled=false for local demo verification.
    public class DigiLockerOptions
    {
        public bool Enabled { get; set; }

        public string ClientId { get; set; } = string.Empty;

        public string ClientSecret { get; set; } = string.Empty;

        public string AuthorizationUrl { get; set; } = string.Empty;

        public string RedirectUri { get; set; } = string.Empty;

        public string Scope { get; set; } = "openid";

        public string FrontendRedirectUrl { get; set; } = string.Empty;
    }
}
