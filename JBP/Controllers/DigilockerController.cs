using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JobPortal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DigilockerController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public DigilockerController(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        [HttpPost("generate-link")]
        public async Task<IActionResult> GenerateLink()
        {
            var frontendUrl = GetFrontendRedirectUrl();
            var surepassToken = GetSurepassToken();

            if (string.IsNullOrWhiteSpace(surepassToken))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Surepass API key is missing. Set Surepass:ApiKey in appsettings.json."
                });
            }

            var requestBody = new
            {
                data = new
                {
                    expiry_minutes = 10,
                    send_sms = false,
                    send_email = false,
                    verify_phone = false,
                    verify_email = false,
                    redirect_url = frontendUrl,
                    skip_main_screen = false,
                    signup_flow = true
                }
            };

            var content =
                new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add(
                "Authorization",
                $"Bearer {surepassToken}");
            _httpClient.DefaultRequestHeaders.Add(
                "Accept",
                "application/json");
            _httpClient.DefaultRequestHeaders.Add(
                "User-Agent",
                "VistaJobs/1.0");

            var response =
                await _httpClient.PostAsync(
                    "https://sandbox.surepass.app/api/v1/digilocker/initialize",
                    content);

            var result =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var message = ExtractSurepassMessage(result);

                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    message =
                        "Surepass rejected DigiLocker access with 403. " +
                        "Use a valid Surepass API key with DigiLocker enabled for this account.";
                }

                return StatusCode((int)response.StatusCode, new
                {
                    success = false,
                    message = message ??
                        "Surepass DigiLocker link creation failed"
                });
            }

            try
            {
                using var document =
                    JsonDocument.Parse(result);

                var root =
                    document.RootElement;

                var redirectUrl =
                    FindString(root, "url") ??
                    FindString(root, "redirect_url") ??
                    FindString(root, "redirectUrl") ??
                    FindString(root, "authorization_url") ??
                    FindString(root, "authorizationUrl");

                if (string.IsNullOrWhiteSpace(redirectUrl))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Surepass did not return a DigiLocker redirect URL"
                    });
                }

                return Ok(new
                {
                    success = true,
                    redirectUrl,
                    data = JsonSerializer.Deserialize<object>(result)
                });
            }
            catch (JsonException)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Surepass returned an invalid response while creating DigiLocker link"
                });
            }
        }

        private string GetFrontendRedirectUrl()
        {
            var origin =
                Request.Headers.Origin.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(origin) &&
                Request.Headers.Referer.Count > 0 &&
                Uri.TryCreate(Request.Headers.Referer.FirstOrDefault(), UriKind.Absolute, out var referer))
            {
                origin = referer.GetLeftPart(UriPartial.Authority);
            }

            if (string.IsNullOrWhiteSpace(origin))
            {
                origin = "http://127.0.0.1:5500";
            }

            return $"{origin.TrimEnd('/')}/index.html?status=success";
        }

        private string GetSurepassToken()
        {
            var surepassApiKey =
                _configuration["Surepass:ApiKey"];

            if (!string.IsNullOrWhiteSpace(surepassApiKey))
            {
                return surepassApiKey;
            }

            return _configuration["EPFO:ApiKey"] ??
                string.Empty;
        }

        private static string? ExtractSurepassMessage(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            try
            {
                using var document =
                    JsonDocument.Parse(value);

                return FindString(document.RootElement, "message") ??
                    FindString(document.RootElement, "error") ??
                    FindString(document.RootElement, "detail");
            }
            catch (JsonException)
            {
                var trimmed =
                    Regex.Replace(value, "<.*?>", " ").Trim();

                return string.IsNullOrWhiteSpace(trimmed)
                    ? null
                    : trimmed.Length > 180
                        ? trimmed[..180]
                        : trimmed;
            }
        }

        [HttpGet("verified-details/{clientId}")]
        public async Task<IActionResult> GetVerifiedDetails(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId) ||
                !clientId.StartsWith("digilocker_"))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid DigiLocker client id"
                });
            }

            var surepassToken = GetSurepassToken();

            if (string.IsNullOrWhiteSpace(surepassToken))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Surepass API key is missing. Set Surepass:ApiKey in appsettings.json."
                });
            }

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add(
                "Authorization",
                $"Bearer {surepassToken}");

            var response =
                await _httpClient.GetAsync(
                    $"https://sandbox.surepass.app/api/v1/digilocker/download-aadhaar/{clientId}");

            var result =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, result);
            }

            using var document =
                JsonDocument.Parse(result);

            var root =
                document.RootElement;

            var listJson =
                await GetSurepassJson(
                    $"https://sandbox.surepass.app/api/v1/digilocker/list-documents/{clientId}");

            var aadhaarNumber =
                FindString(root, "aadhaar_number") ??
                FindString(root, "aadhaar") ??
                FindString(root, "masked_aadhaar") ??
                FindString(root, "masked_aadhaar_number") ??
                ExtractMaskedDocumentNumber(listJson, "aadhaar");

            var panNumber =
                FindString(root, "pan_number") ??
                FindString(root, "pan") ??
                FindString(root, "pan_card_number") ??
                ExtractMaskedDocumentNumber(listJson, "pan") ??
                await GetPanFromDigiLockerDocument(clientId);

            return Ok(new
            {
                success = true,
                aadhaarNumber,
                panNumber,
                fullName =
                    FindString(root, "full_name") ??
                    FindString(root, "name"),
                dob =
                    FindString(root, "dob"),
                address =
                    FindString(root, "full_address")
            });
        }

        private async Task<string?> GetPanFromDigiLockerDocument(string clientId)
        {
            var listJson =
                await GetSurepassJson(
                    $"https://sandbox.surepass.app/api/v1/digilocker/list-documents/{clientId}");

            if (string.IsNullOrWhiteSpace(listJson))
            {
                return null;
            }

            var panFromList =
                ExtractPan(listJson);

            if (!string.IsNullOrWhiteSpace(panFromList))
            {
                return panFromList;
            }

            using var listDocument =
                JsonDocument.Parse(listJson);

            var panFileId =
                FindPanFileId(listDocument.RootElement);

            if (string.IsNullOrWhiteSpace(panFileId))
            {
                return null;
            }

            var downloadJson =
                await GetSurepassJson(
                    $"https://sandbox.surepass.app/api/v1/digilocker/download-document/{clientId}/{panFileId}");

            if (string.IsNullOrWhiteSpace(downloadJson))
            {
                return null;
            }

            var panFromDownloadResponse =
                ExtractPan(downloadJson);

            if (!string.IsNullOrWhiteSpace(panFromDownloadResponse))
            {
                return panFromDownloadResponse;
            }

            using var downloadDocument =
                JsonDocument.Parse(downloadJson);

            var downloadUrl =
                FindString(downloadDocument.RootElement, "download_url") ??
                FindString(downloadDocument.RootElement, "file_url") ??
                FindString(downloadDocument.RootElement, "document_url") ??
                FindString(downloadDocument.RootElement, "url");

            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                return null;
            }

            try
            {
                var bytes =
                    await _httpClient.GetByteArrayAsync(downloadUrl);

                var text =
                    Encoding.UTF8.GetString(bytes) + "\n" +
                    Encoding.Latin1.GetString(bytes);

                return ExtractPan(text);
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> GetSurepassJson(string url)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add(
                "Authorization",
                $"Bearer {GetSurepassToken()}");

            var response =
                await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadAsStringAsync();
        }

        private static string? ExtractPan(string value)
        {
            var match =
                Regex.Match(
                    value,
                    @"\b[A-Z]{5}[0-9]{4}[A-Z]\b",
                    RegexOptions.IgnoreCase);

            return match.Success
                ? match.Value.ToUpperInvariant()
                : ExtractMaskedPan(value);
        }

        private static string? ExtractMaskedPan(string value)
        {
            var match =
                Regex.Match(
                    value,
                    @"\b[Xx*]{2,5}[A-Z][0-9]{4}[A-Z]\b",
                    RegexOptions.IgnoreCase);

            return match.Success
                ? match.Value.ToUpperInvariant().Replace("*", "X")
                : null;
        }

        private static string? ExtractMaskedDocumentNumber(
            string? value,
            string documentType)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var pattern =
                documentType.Equals("pan", StringComparison.OrdinalIgnoreCase)
                    ? @"PAN[^()]*\(([Xx*]{2,5}[A-Z][0-9]{4}[A-Z])\)"
                    : @"Aadhaar[^()]*\(([Xx*]{2,8}[0-9]{4})\)";

            var match =
                Regex.Match(
                    value,
                    pattern,
                    RegexOptions.IgnoreCase);

            return match.Success
                ? match.Groups[1].Value.ToUpperInvariant().Replace("*", "X")
                : null;
        }

        private static string? FindPanFileId(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                var isPanDocument = false;
                string? fileId = null;

                foreach (var property in element.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.String)
                    {
                        var value =
                            property.Value.GetString() ?? string.Empty;

                        if (string.Equals(property.Name, "file_id", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(property.Name, "fileId", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(property.Name, "uri", StringComparison.OrdinalIgnoreCase))
                        {
                            fileId = value;
                        }

                        if (value.Contains("PAN", StringComparison.OrdinalIgnoreCase) ||
                            value.Contains("PANCR", StringComparison.OrdinalIgnoreCase))
                        {
                            isPanDocument = true;
                        }
                    }

                    var nested =
                        FindPanFileId(property.Value);

                    if (!string.IsNullOrWhiteSpace(nested))
                    {
                        return nested;
                    }
                }

                if (isPanDocument &&
                    !string.IsNullOrWhiteSpace(fileId))
                {
                    return fileId;
                }
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var nested =
                        FindPanFileId(item);

                    if (!string.IsNullOrWhiteSpace(nested))
                    {
                        return nested;
                    }
                }
            }

            return null;
        }

        private static string? FindString(JsonElement element, string propertyName)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase) &&
                        property.Value.ValueKind == JsonValueKind.String)
                    {
                        return property.Value.GetString();
                    }

                    var nested =
                        FindString(property.Value, propertyName);

                    if (!string.IsNullOrWhiteSpace(nested))
                    {
                        return nested;
                    }
                }
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var nested =
                        FindString(item, propertyName);

                    if (!string.IsNullOrWhiteSpace(nested))
                    {
                        return nested;
                    }
                }
            }

            return null;
        }
    }
}
