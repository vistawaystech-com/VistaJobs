using System.IO;
using System.Text;
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

            var payload = new
            {
                id_number = uan
            };

            var content =
                new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

            using var response =
                await _httpClient.PostAsync(
                    _options.UanPathTemplate,
                    content,
                    cancellationToken);

            var json =
                await response.Content.ReadAsStringAsync(cancellationToken);
            Directory.CreateDirectory(@"C:\temp");

            File.WriteAllText(
                @"C:\temp\surepass.json",
                json);


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

            Console.WriteLine("SUREPASS JSON:");
            Console.WriteLine(json);

            var surepassResponse =
    JsonSerializer.Deserialize<SurepassResponse>(
        json,
        JsonOptions);
            if (surepassResponse?.Data == null)
            {
                return new UanVerificationResult
                {
                    Uan = uan,
                    Verified = false,
                    Message = "No data returned"
                };
            }
            Console.WriteLine(
    "Surepass Employment Count = " +
    (surepassResponse.Data.Employment_History?.Count ?? 0)
);
            return new UanVerificationResult
            {
                Uan = surepassResponse.Data.Uan ?? uan,
                Verified = surepassResponse.Success,
                EmploymentHistory =
   surepassResponse.Data.Employment_History?
    .Select(x => new EmploymentHistory
    {
        Company = x.Establishment_Name ?? "",
        Doj = x.Date_Of_Joining ?? "",
        Doe = x.Date_Of_Exit ?? ""
    })
    .ToList()
    ?? new List<EmploymentHistory>()
            };
        }
    }
}
