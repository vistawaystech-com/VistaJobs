using System.Net.Http.Headers;
using System.Text.Json;
using JBP.Models;
using JBP.Services;
using Microsoft.Extensions.Options;

namespace JBP.Services.VerificationProviders
{
    public class SurepassEpfoProvider : IEpfoVerificationProvider
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private readonly EpfoOptions _options;

        public SurepassEpfoProvider(
            HttpClient httpClient,
            IOptions<EpfoOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<UanVerificationResult> VerifyUanAsync(
            string uan,
            CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled)
            {
                return new UanVerificationResult
                {
                    Uan = uan,
                    Verified = true,
                    Message = "UAN verified in local EPFO mode",
                    EmploymentHistory = new List<EmploymentHistory>()
                };
            }

            if (string.IsNullOrWhiteSpace(_options.ApiKey) ||
                string.IsNullOrWhiteSpace(_options.BaseUrl))
            {
                return new UanVerificationResult
                {
                    Uan = uan,
                    Verified = false,
                    Message = "EPFO config missing"
                };
            }

            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            var path = (_options.UanPathTemplate ?? "/uan/{uan}")
                .Replace("{uan}", Uri.EscapeDataString(uan));

            using var response =
                await _httpClient.GetAsync(path, cancellationToken);

            var json =
                await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new UanVerificationResult
                {
                    Uan = uan,
                    Verified = false,
                    Message = string.IsNullOrWhiteSpace(json)
                        ? "EPFO verification failed"
                        : json
                };
            }

            var result =
                JsonSerializer.Deserialize<UanVerificationResult>(
                    json,
                    JsonOptions);

            return result ?? new UanVerificationResult
            {
                Uan = uan,
                Verified = false,
                Message = "EPFO response was empty"
            };
        }
    }
}
