using JBP.Data;
using JBP.DTOs;
using JBP.Models;
using JBP.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jobsy.API.Controllers
{
    // Authentication flow starts here: OTP, registration, login, Google auth, and password reset are handled here.
    // Authentication flow ikkada start avtundi: OTP, registration, login, Google auth, password reset ikkade handle avtayi.
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _environment;

        public AuthController(
            ApplicationDbContext context,
            IConfiguration configuration,
            EmailService emailService,
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment environment)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
            _httpClientFactory = httpClientFactory;
            _environment = environment;
        }

        [HttpPost("request-otp")]
        public async Task<IActionResult> RequestOtp(OtpRequestDto dto)
        {
            // OTP flow creates a short-lived hashed code for register, employer-register, or login.
            // OTP flow register/employer-register/login kosam short-lived hashed code create chestundi.
            var email = NormalizeEmail(dto.Email);
            var purpose = NormalizePurpose(dto.Purpose);

            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("Email is required");
            }

            if (purpose == "employer-register" && !IsOfficialEmail(email))
            {
                return BadRequest("Please use an official company email address");
            }

            if (purpose == "register" && _context.Users.Any(u => u.Email == email))
            {
                return BadRequest("Email already registered");
            }

            if (purpose == "employer-register" && _context.Users.Any(u => u.Email == email))
            {
                return BadRequest("Email already registered");
            }

            if (purpose == "login" && !_context.Users.Any(u => u.Email == email))
            {
                return BadRequest("Email is not registered");
            }

            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            _context.EmailOtpVerifications.Add(new EmailOtpVerification
            {
                Email = email,
                Purpose = purpose,
                CodeHash = BCrypt.Net.BCrypt.HashPassword(code),
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            });

            await _context.SaveChangesAsync();

            await _emailService.SendEmail(
                email,
                "Your VistaJobs OTP",
                $"<p>Your VistaJobs OTP is <strong>{code}</strong>.</p><p>This code expires in 10 minutes.</p>");

            return Ok("OTP sent to your email");
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            var email = NormalizeEmail(dto.Email);

            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("Email is required");
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                return BadRequest("Email is not registered");
            }

            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            _context.EmailOtpVerifications.Add(new EmailOtpVerification
            {
                Email = email,
                Purpose = "reset-password",
                CodeHash = BCrypt.Net.BCrypt.HashPassword(code),
                ExpiresAt = DateTime.UtcNow.AddMinutes(5)
            });

            await _context.SaveChangesAsync();

            try
            {
                await _emailService.SendEmail(
                    email,
                    "Your VistaJobs password reset OTP",
                    $"<p>Your VistaJobs password reset OTP is <strong>{code}</strong>.</p><p>This code expires in 5 minutes.</p>");
            }
            catch
            {
                if (!_environment.IsDevelopment())
                {
                    return StatusCode(503, "Unable to send OTP email. Please try again later.");
                }

                return Ok(new
                {
                    message = "OTP sent successfully.",
                    developmentOtp = code
                });
            }

            return Ok("OTP sent successfully.");
        }

        [HttpPost("verify-reset-otp")]
        public IActionResult VerifyResetOtp(VerifyResetOtpDto dto)
        {
            var email = NormalizeEmail(dto.Email);

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(dto.Otp))
            {
                return BadRequest("Email and OTP are required");
            }

            if (!_context.Users.Any(u => u.Email == email))
            {
                return BadRequest("Email is not registered");
            }

            if (!VerifyOtp(email, dto.Otp, "reset-password", markUsed: false))
            {
                return BadRequest("Invalid or expired OTP");
            }

            return Ok("OTP verified successfully.");
        }

        [HttpPost("reset-password")]
        public IActionResult ResetPassword(ResetPasswordDto dto)
        {
            var email = NormalizeEmail(dto.Email);

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(dto.Otp) ||
                string.IsNullOrWhiteSpace(dto.NewPassword) ||
                string.IsNullOrWhiteSpace(dto.ConfirmPassword))
            {
                return BadRequest("Please fill all reset password fields");
            }

            if (dto.NewPassword != dto.ConfirmPassword)
            {
                return BadRequest("New Password and Confirm Password must match");
            }

            if (!IsValidPassword(dto.NewPassword))
            {
                return BadRequest("Password must be at least 6 characters and include at least one letter and one number");
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                return BadRequest("Email is not registered");
            }

            if (!VerifyOtp(email, dto.Otp, "reset-password", markUsed: true))
            {
                return BadRequest("Invalid or expired OTP");
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.AuthProvider = string.IsNullOrWhiteSpace(user.AuthProvider)
                ? "Email"
                : user.AuthProvider;

            _context.SaveChanges();

            return Ok("Password changed successfully.");
        }

        // Registers a new user after blocking duplicate email addresses.
        [HttpPost("register")]
        public IActionResult Register(RegisterDto dto)
        {
            // Jobseeker registration flow ends here after OTP verification and BCrypt password storage.
            // Jobseeker registration flow OTP verify ayi BCrypt password save ayyaka ikkada end avtundi.
            dto.Email = NormalizeEmail(dto.Email);

            var exists = _context.Users.Any(u => u.Email == dto.Email);

            if (exists)
            {
                return BadRequest("Email already registered");
            }

            if (!VerifyOtp(dto.Email, dto.Otp, "register", markUsed: true))
            {
                return BadRequest("Invalid or expired OTP");
            }

            var role = NormalizeRole(dto.Role);

            if (role != "jobseeker")
            {
                return BadRequest("Use employer registration for employer accounts");
            }

            // Store only the BCrypt hash. The original password is never saved.
            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "jobseeker",
                AuthProvider = "Email",
                EmailVerified = true
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok("User registered successfully");
        }

        [HttpPost("register-employer")]
        public IActionResult RegisterEmployer(EmployerRegisterDto dto)
        {
            // Employer registration flow validates company identity fields before creating an employer login.
            // Employer registration flow company fields validate chesi employer login create chestundi.
            var email = NormalizeEmail(dto.OfficialEmail);

            if (string.IsNullOrWhiteSpace(dto.CompanyName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(dto.Password) ||
                string.IsNullOrWhiteSpace(dto.GstNumber) ||
                string.IsNullOrWhiteSpace(dto.CinNumber) ||
                string.IsNullOrWhiteSpace(dto.Website))
            {
                return BadRequest("Please fill all employer registration fields");
            }

            if (!IsOfficialEmail(email))
            {
                return BadRequest("Please use an official company email address");
            }

            if (!IsValidGst(dto.GstNumber))
            {
                return BadRequest("Enter a valid 15-character GST number");
            }

            if (!IsValidCin(dto.CinNumber))
            {
                return BadRequest("Enter a valid 21-character CIN number");
            }

            if (!IsValidWebsite(dto.Website))
            {
                return BadRequest("Enter a valid company website");
            }

            if (!WebsiteMatchesEmail(dto.Website, email))
            {
                return BadRequest("Official email domain must match the company website");
            }

            if (_context.Users.Any(u => u.Email == email))
            {
                return BadRequest("Email already registered");
            }

            if (_context.EmployerProfiles.Any(e => e.OfficialEmail == email))
            {
                return BadRequest("Employer already registered");
            }

            if (!VerifyOtp(email, dto.Otp, "employer-register", markUsed: true))
            {
                return BadRequest("Invalid or expired OTP");
            }

            var user = new User
            {
                FullName = dto.CompanyName.Trim(),
                Email = email,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "employer",
                AuthProvider = "Email",
                EmailVerified = true
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            _context.EmployerProfiles.Add(new EmployerProfile
            {
                UserId = user.Id,
                CompanyName = dto.CompanyName.Trim(),
                OfficialEmail = email,
                GstNumber = dto.GstNumber.Trim().ToUpperInvariant(),
                CinNumber = dto.CinNumber.Trim().ToUpperInvariant(),
                Website = NormalizeWebsite(dto.Website)
            });

            // Employer registration ends by storing the verified company profile linked to the user row.
            // Employer registration ikkada end avtundi: verified company profile user row ki link ayi save avtundi.
            _context.SaveChanges();

            return Ok("Employer registered successfully");
        }

        // Validates credentials and asks for OTP before issuing a JWT.
        [HttpPost("login")]
        public IActionResult Login(LoginDto dto)
        {
            // Login starts with password validation; OTP is requested only after password is correct.
            // Login first password validate chestundi; password correct ayyaka matrame OTP request avtundi.
            dto.Email = NormalizeEmail(dto.Email);

            var user = _context.Users.FirstOrDefault(u => u.Email == dto.Email);

            if (user == null || string.IsNullOrWhiteSpace(user.Password))
            {
                return Unauthorized("Invalid email or password");
            }

            if (!VerifyStoredPassword(user, dto.Password))
            {
                return Unauthorized("Invalid email or password");
            }

            return Ok(new
            {
                requiresOtp = true,
                message = "Password verified. Please enter the OTP sent to your email."
            });
        }

        [HttpPost("verify-login-otp")]
        public IActionResult VerifyLoginOtp(LoginOtpVerifyDto dto)
        {
            // Login ends here: OTP is verified and a JWT with role claims is returned.
            // Login ikkada end avtundi: OTP verify ayi role claims unna JWT return avtundi.
            dto.Email = NormalizeEmail(dto.Email);

            var user = _context.Users.FirstOrDefault(u => u.Email == dto.Email);

            if (user == null ||
                string.IsNullOrWhiteSpace(user.Password) ||
                !VerifyStoredPassword(user, dto.Password))
            {
                return Unauthorized("Invalid email or password");
            }

            if (!VerifyOtp(dto.Email, dto.Otp, "login", markUsed: true))
            {
                return BadRequest("Invalid or expired OTP");
            }

            return Ok(BuildLoginResponse(user));
        }

        [HttpPost("google")]
        public async Task<IActionResult> Google(GoogleAuthDto dto)
        {
            var clientId = _configuration["GoogleAuth:ClientId"];

            if (string.IsNullOrWhiteSpace(clientId))
            {
                return BadRequest("Google client id is not configured");
            }

            var googleUser = await VerifyGoogleCredential(dto.Credential, clientId);

            if (googleUser == null || !googleUser.IsEmailVerified)
            {
                return Unauthorized("Invalid Google sign in");
            }

            var email = NormalizeEmail(googleUser.Email);
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                var role = NormalizeRole(dto.Role);

                if (string.IsNullOrWhiteSpace(role))
                {
                    return BadRequest("Please select a role before using Google sign in");
                }

                if (role == "employer")
                {
                    return BadRequest("Use employer registration for employer accounts");
                }

                user = new User
                {
                    FullName = string.IsNullOrWhiteSpace(googleUser.Name)
                        ? email
                        : googleUser.Name,
                    Email = email,
                    Password = string.Empty,
                    Role = role,
                    AuthProvider = "Google",
                    GoogleSubject = googleUser.Subject,
                    EmailVerified = true
                };

                _context.Users.Add(user);
            }
            else
            {
                user.AuthProvider = string.IsNullOrWhiteSpace(user.AuthProvider)
                    ? "Google"
                    : user.AuthProvider;
                user.GoogleSubject = googleUser.Subject;
                user.EmailVerified = true;
            }

            _context.SaveChanges();

            return Ok(BuildLoginResponse(user));
        }

        private object BuildLoginResponse(User user)
        {
            // Claims are the source for role checks and current-user profile lookup.
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            // Token signing key must match Jwt:Key in appsettings.
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Token lifetime is intentionally short for this local project.
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return new
            {
                token = jwt,
                name = user.FullName,
                role = user.Role,
                email = user.Email
            };
        }

        private bool VerifyStoredPassword(User user, string password)
        {
            if (string.IsNullOrWhiteSpace(user.Password))
            {
                return false;
            }

            var storedPassword = user.Password.Trim();
            var looksLikeBcrypt =
                storedPassword.StartsWith("$2a$") ||
                storedPassword.StartsWith("$2b$") ||
                storedPassword.StartsWith("$2y$");

            if (looksLikeBcrypt)
            {
                try
                {
                    return BCrypt.Net.BCrypt.Verify(password, storedPassword);
                }
                catch
                {
                    return false;
                }
            }

            // Legacy accounts may still have plain passwords from older builds.
            // On successful login, migrate them to BCrypt transparently.
            if (!string.Equals(storedPassword, password, StringComparison.Ordinal))
            {
                return false;
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(password);
            _context.SaveChanges();
            return true;
        }

        private bool VerifyOtp(
            string email,
            string otp,
            string purpose,
            bool markUsed)
        {
            var normalizedEmail = NormalizeEmail(email);
            var normalizedPurpose = NormalizePurpose(purpose);

            var candidates = _context.EmailOtpVerifications
                .Where(o =>
                    o.Email == normalizedEmail &&
                    o.Purpose == normalizedPurpose &&
                    !o.IsUsed &&
                    o.ExpiresAt >= DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToList();

            var match = candidates.FirstOrDefault(o =>
                BCrypt.Net.BCrypt.Verify(otp ?? string.Empty, o.CodeHash));

            if (match == null)
            {
                return false;
            }

            if (markUsed)
            {
                match.IsUsed = true;
                _context.SaveChanges();
            }

            return true;
        }

        private async Task<GoogleTokenInfo?> VerifyGoogleCredential(
            string credential,
            string clientId)
        {
            if (string.IsNullOrWhiteSpace(credential))
            {
                return null;
            }

            var httpClient = _httpClientFactory.CreateClient();
            var url =
                $"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(credential)}";
            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var payload = await response.Content.ReadAsStringAsync();
            var tokenInfo = JsonSerializer.Deserialize<GoogleTokenInfo>(
                payload,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (tokenInfo == null ||
                tokenInfo.Audience != clientId ||
                string.IsNullOrWhiteSpace(tokenInfo.Email) ||
                string.IsNullOrWhiteSpace(tokenInfo.Subject))
            {
                return null;
            }

            return tokenInfo;
        }

        private static string NormalizeEmail(string email) =>
            (email ?? string.Empty).Trim().ToLowerInvariant();

        private static string NormalizePurpose(string purpose) =>
            string.Equals(purpose, "login", StringComparison.OrdinalIgnoreCase)
                ? "login"
                : string.Equals(purpose, "employer-register", StringComparison.OrdinalIgnoreCase)
                    ? "employer-register"
                    : string.Equals(purpose, "reset-password", StringComparison.OrdinalIgnoreCase)
                        ? "reset-password"
                        : "register";

        private static string NormalizeRole(string role)
        {
            var normalized = (role ?? string.Empty).Trim().ToLowerInvariant();

            return normalized is "jobseeker" or "employer" or "admin"
                ? normalized
                : string.Empty;
        }

        private static bool IsValidPassword(string password) =>
            !string.IsNullOrWhiteSpace(password) &&
            password.Length >= 6 &&
            password.Any(char.IsLetter) &&
            password.Any(char.IsDigit);

        private static bool IsOfficialEmail(string email)
        {
            var domain = email.Split('@').LastOrDefault()?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(domain) || !domain.Contains('.'))
            {
                return false;
            }

            var blockedDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "gmail.com", "googlemail.com", "yahoo.com", "yahoo.co.in",
                "outlook.com", "hotmail.com", "live.com", "msn.com",
                "icloud.com", "me.com", "aol.com", "proton.me",
                "protonmail.com", "zoho.com", "mail.com", "gmx.com",
                "rediffmail.com", "yandex.com"
            };

            return !blockedDomains.Contains(domain);
        }

        private static bool IsValidGst(string value) =>
            Regex.IsMatch((value ?? string.Empty).Trim().ToUpperInvariant(),
                "^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$");

        private static bool IsValidCin(string value) =>
            Regex.IsMatch((value ?? string.Empty).Trim().ToUpperInvariant(),
                "^[A-Z][0-9]{5}[A-Z]{2}[0-9]{4}[A-Z]{3}[0-9]{6}$");

        private static bool IsValidWebsite(string value) =>
            Uri.TryCreate(NormalizeWebsite(value), UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
            !string.IsNullOrWhiteSpace(uri.Host) &&
            uri.Host.Contains('.');

        private static string NormalizeWebsite(string value)
        {
            var website = (value ?? string.Empty).Trim();

            if (!website.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !website.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                website = $"https://{website}";
            }

            return website;
        }

        private static bool WebsiteMatchesEmail(string website, string email)
        {
            if (!Uri.TryCreate(NormalizeWebsite(website), UriKind.Absolute, out var uri))
            {
                return false;
            }

            var websiteHost = uri.Host.ToLowerInvariant();
            var emailDomain = email.Split('@').LastOrDefault()?.ToLowerInvariant() ?? string.Empty;

            if (websiteHost.StartsWith("www."))
            {
                websiteHost = websiteHost[4..];
            }

            return emailDomain == websiteHost || emailDomain.EndsWith($".{websiteHost}");
        }

        private class GoogleTokenInfo
        {
            [JsonPropertyName("aud")]
            public string Audience { get; set; } = string.Empty;

            [JsonPropertyName("email")]
            public string Email { get; set; } = string.Empty;

            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("sub")]
            public string Subject { get; set; } = string.Empty;

            [JsonPropertyName("email_verified")]
            public JsonElement EmailVerified { get; set; }

            public bool IsEmailVerified =>
                EmailVerified.ValueKind == JsonValueKind.True ||
                (EmailVerified.ValueKind == JsonValueKind.String &&
                    string.Equals(
                        EmailVerified.GetString(),
                        "true",
                        StringComparison.OrdinalIgnoreCase));
        }
    }
}
