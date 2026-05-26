using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace JBP.Services
{
    // Abstraction keeps controller code independent from the DigiLocker integration details.
    public interface IDigiLockerGateway
    {
        DigiLockerStartResult StartVerification(
            string documentType,
            string number,
            string candidateEmail);
    }

    public class DigiLockerStartResult
    {
        public bool IsEnabled { get; set; }

        public bool IsConfigured { get; set; }

        public string? RedirectUrl { get; set; }

        public string? Message { get; set; }
    }

    public class DigiLockerGateway : IDigiLockerGateway
    {
        private readonly DigiLockerOptions _options;

        public DigiLockerGateway(
            IOptions<DigiLockerOptions> options)
        {
            _options = options.Value;
        }

        public DigiLockerStartResult StartVerification(
            string documentType,
            string number,
            string candidateEmail)
        {
            // Local/test mode: controller will mark the document verified without redirect.
            if (!_options.Enabled)
            {
                return new DigiLockerStartResult
                {
                    IsEnabled = false,
                    IsConfigured = false
                };
            }

            // Enabled but incomplete config should fail clearly for DevOps/testers.
            if (string.IsNullOrWhiteSpace(_options.ClientId) ||
                string.IsNullOrWhiteSpace(_options.AuthorizationUrl) ||
                string.IsNullOrWhiteSpace(_options.RedirectUri))
            {
                return new DigiLockerStartResult
                {
                    IsEnabled = true,
                    IsConfigured = false,
                    Message = "DigiLocker config missing"
                };
            }

            // State carries the requested document context back to the callback endpoint.
            var statePayload = JsonSerializer.Serialize(new
            {
                documentType,
                number,
                candidateEmail,
                createdAt = DateTimeOffset.UtcNow
            });

            var state = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(statePayload));

            var query = new Dictionary<string, string?>
            {
                // Standard OAuth-style redirect parameters.
                ["response_type"] = "code",
                ["client_id"] = _options.ClientId,
                ["redirect_uri"] = _options.RedirectUri,
                ["scope"] = _options.Scope,
                ["state"] = state,
                ["document_type"] = documentType
            };

            return new DigiLockerStartResult
            {
                IsEnabled = true,
                IsConfigured = true,
                RedirectUrl = QueryHelpers.AddQueryString(
                    _options.AuthorizationUrl,
                    query),
                Message = $"Redirecting to DigiLocker for {documentType}"
            };
        }
    }
}
