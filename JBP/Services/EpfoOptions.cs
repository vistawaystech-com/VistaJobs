namespace JBP.Services
{
    // Maps appsettings:EPFO. Keep Enabled=false for local plug-and-play verification.
    public class EpfoOptions
    {
        public bool Enabled { get; set; }

        public string ApiKey { get; set; } = string.Empty;

        public string BaseUrl { get; set; } = string.Empty;

        public string UanPathTemplate { get; set; } = "/uan/{uan}";
    }
}
