using System.Text;
using System.Text.Json;
using JBP.Data;
using JBP.Models;
using JBP.Services.VerificationProviders;
using Microsoft.Extensions.Options;

namespace JBP.Services
{
    public class VerificationService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly ApplicationDbContext _context;
        private readonly IDigiLockerGateway _digiLockerGateway;
        private readonly IEpfoVerificationProvider _epfoProvider;
        private readonly DigiLockerOptions _digiLockerOptions;

        public VerificationService(
            ApplicationDbContext context,
            IDigiLockerGateway digiLockerGateway,
            IEpfoVerificationProvider epfoProvider,
            IOptions<DigiLockerOptions> digiLockerOptions)
        {
            _context = context;
            _digiLockerGateway = digiLockerGateway;
            _epfoProvider = epfoProvider;
            _digiLockerOptions = digiLockerOptions.Value;
        }

        public VerificationStartResult VerifyAadhaar(
            Candidate? candidate,
            string number,
            string email)
        {
            if (string.IsNullOrWhiteSpace(number))
            {
                return VerificationStartResult.Failed("Aadhaar is required");
            }

            if (number.Length != 12)
            {
                return VerificationStartResult.Failed("Invalid Aadhaar");
            }

            var digiLocker =
                _digiLockerGateway.StartVerification(
                    "aadhaar",
                    number,
                    email);

            if (digiLocker.IsEnabled)
            {
                if (!digiLocker.IsConfigured)
                {
                    SaveLog(candidate, "aadhaar", "DigiLocker", "config_missing", digiLocker);
                    return VerificationStartResult.Failed(digiLocker.Message ?? "DigiLocker config missing");
                }

                SaveLog(candidate, "aadhaar", "DigiLocker", "redirect_started", digiLocker);
                return VerificationStartResult.Redirect(digiLocker.RedirectUrl, digiLocker.Message);
            }

            if (candidate != null)
            {
                candidate.AadhaarVerified = true;
                candidate.AadhaarNumber = number;
                SaveLog(candidate, "aadhaar", "Local", "verified", new { number });
                _context.SaveChanges();
            }

            return VerificationStartResult.Completed("Aadhaar verified");
        }

        public VerificationStartResult VerifyPan(
            Candidate? candidate,
            string number,
            string email)
        {
            if (string.IsNullOrWhiteSpace(number))
            {
                return VerificationStartResult.Failed("PAN is required");
            }

            if (number.Length != 10)
            {
                return VerificationStartResult.Failed("Invalid PAN");
            }

            var normalizedPan = number.ToUpperInvariant();
            var digiLocker =
                _digiLockerGateway.StartVerification(
                    "pan",
                    normalizedPan,
                    email);

            if (digiLocker.IsEnabled)
            {
                if (!digiLocker.IsConfigured)
                {
                    SaveLog(candidate, "pan", "DigiLocker", "config_missing", digiLocker);
                    return VerificationStartResult.Failed(digiLocker.Message ?? "DigiLocker config missing");
                }

                SaveLog(candidate, "pan", "DigiLocker", "redirect_started", digiLocker);
                return VerificationStartResult.Redirect(digiLocker.RedirectUrl, digiLocker.Message);
            }

            if (candidate != null)
            {
                candidate.PanVerified = true;
                candidate.PanNumber = normalizedPan;
                SaveLog(candidate, "pan", "Local", "verified", new { number = normalizedPan });
                _context.SaveChanges();
            }

            return VerificationStartResult.Completed("PAN verified");
        }

        public async Task<UanVerificationResult> VerifyUanAsync(
            Candidate? candidate,
            string number,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(number))
            {
                return new UanVerificationResult
                {
                    Verified = false,
                    Message = "UAN is required"
                };
            }

            if (number.Length != 12)
            {
                return new UanVerificationResult
                {
                    Verified = false,
                    Message = "Invalid UAN"
                };
            }

            var result =
                await _epfoProvider.VerifyUanAsync(
                    number,
                    cancellationToken);
            Console.WriteLine(
    "Employment Count = " +
    result.EmploymentHistory.Count);
            Console.WriteLine("Verified = " + result.Verified);
            Console.WriteLine("Message = " + result.Message);

            SaveLog(
                candidate,
                "uan",
                "SurepassEPFO",
                result.Verified ? "verified" : "failed",
                result);

            if (result.Verified && candidate != null)
            {
                candidate.UanVerified = true;
                candidate.UanNumber = number;
                candidate.EmploymentHistory =
                    JsonSerializer.Serialize(result.EmploymentHistory);

                ReplaceEmploymentHistory(candidate, number, result.EmploymentHistory);
            }

            _context.SaveChanges();

            return result;
        }

        public DigiLockerCallbackResult PersistDigiLockerCallback(
            string code,
            string state)
        {
            var stateData = DecodeState(state);

            if (stateData == null ||
                string.IsNullOrWhiteSpace(stateData.CandidateEmail) ||
                string.IsNullOrWhiteSpace(stateData.DocumentType))
            {
                return new DigiLockerCallbackResult
                {
                    Success = false,
                    Message = "Invalid DigiLocker state"
                };
            }

            var candidate =
                _context.Candidates
                    .FirstOrDefault(c => c.Email == stateData.CandidateEmail);

            if (candidate == null)
            {
                SaveLog(null, stateData.DocumentType, "DigiLocker", "candidate_not_found", stateData);
                _context.SaveChanges();

                return new DigiLockerCallbackResult
                {
                    Success = false,
                    Message = "Candidate not found"
                };
            }

            switch (stateData.DocumentType.ToLowerInvariant())
            {
                case "aadhaar":
                    candidate.AadhaarVerified = true;
                    candidate.AadhaarNumber = stateData.Number;
                    break;

                case "pan":
                    candidate.PanVerified = true;
                    candidate.PanNumber = stateData.Number?.ToUpperInvariant();
                    break;

                default:
                    return new DigiLockerCallbackResult
                    {
                        Success = false,
                        Message = "Unsupported DigiLocker document type"
                    };
            }

            SaveLog(candidate, stateData.DocumentType, "DigiLocker", "callback_verified", new
            {
                code,
                state = stateData
            });

            _context.SaveChanges();

            return new DigiLockerCallbackResult
            {
                Success = true,
                Message = "DigiLocker verification saved",
                RedirectUrl = _digiLockerOptions.FrontendRedirectUrl
            };
        }

        private void ReplaceEmploymentHistory(
            Candidate candidate,
            string uan,
            List<EmploymentHistory> history)
        {
            Console.WriteLine("History Count = " + history.Count);
            var existing =
                _context.EmploymentHistories
                    .Where(item => item.CandidateId == candidate.Id);

            _context.EmploymentHistories.RemoveRange(existing);

            for (var index = 0; index < history.Count; index++)
            {
                var item = history[index];
                Console.WriteLine("Company = " + item.Company);
                item.Id = 0;
                item.CandidateId = candidate.Id;
                item.Candidate = null;
                item.Uan = uan;
                item.DisplayOrder = index + 1;
                item.CreatedAt = DateTime.UtcNow;

                _context.EmploymentHistories.Add(item);
            }
        }

        private DigiLockerState? DecodeState(string state)
        {
            try
            {
                var json =
                    Encoding.UTF8.GetString(
                        Convert.FromBase64String(state));

                return JsonSerializer.Deserialize<DigiLockerState>(
                    json,
                    JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        private void SaveLog(
            Candidate? candidate,
            string documentType,
            string provider,
            string status,
            object? rawResponse)
        {
            _context.VerificationLogs.Add(new VerificationLog
            {
                CandidateId = candidate?.Id,
                DocumentType = documentType,
                Provider = provider,
                Status = status,
                Timestamp = DateTime.UtcNow,
                RawResponse = rawResponse == null
                    ? null
                    : JsonSerializer.Serialize(rawResponse)
            });
        }
    }

    public class VerificationStartResult
    {
        public bool Success { get; set; }

        public bool Verified { get; set; }

        public string? RedirectUrl { get; set; }

        public string Message { get; set; } = string.Empty;

        public static VerificationStartResult Failed(string message) =>
            new()
            {
                Success = false,
                Verified = false,
                Message = message
            };

        public static VerificationStartResult Completed(string message) =>
            new()
            {
                Success = true,
                Verified = true,
                Message = message
            };

        public static VerificationStartResult Redirect(string? redirectUrl, string? message) =>
            new()
            {
                Success = true,
                Verified = false,
                RedirectUrl = redirectUrl,
                Message = message ?? "Redirecting to verification provider"
            };
    }

    public class DigiLockerCallbackResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public string? RedirectUrl { get; set; }
    }

    public class DigiLockerState
    {
        public string DocumentType { get; set; } = string.Empty;

        public string? Number { get; set; }

        public string CandidateEmail { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }
    }
}
