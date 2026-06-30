using JBP.Data;
using JBP.DTOs;
using JBP.Models;
using JBP.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jobsy.API.Controllers
{
    // Handles account creation and login.
    // Successful login returns a JWT plus display details used by the frontend navbar.
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthController(
            ApplicationDbContext context,
            IConfiguration configuration,
            EmailService emailService,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("request-otp")]
        public async Task<IActionResult> RequestOtp(OtpRequestDto dto)
        {
            var email = NormalizeEmail(dto.Email);
            var purpose = NormalizePurpose(dto.Purpose);

            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("Email is required");
            }

            if (purpose == "register" && _context.Users.Any(u => u.Email == email))
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

        // Registers a new user after blocking duplicate email addresses.
        [HttpPost("register")]
        public IActionResult Register(RegisterDto dto)
        {
            dto.Email = NormalizeEmail(dto.Email);

            var exists = _context.Users.Any(u => u.Email == dto.Email);

            if (exists)
            {
                return BadRequest("Email already registered");
            }

            if (!VerifyOtp(dto.Email, dto.Otp, "register"))
            {
                return BadRequest("Invalid or expired OTP");
            }

            var role = NormalizeRole(dto.Role);

            if (string.IsNullOrWhiteSpace(role))
            {
                return BadRequest("Please select a valid role");
            }

            // Store only the BCrypt hash. The original password is never saved.
            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = role,
                AuthProvider = "Email",
                EmailVerified = true
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok("User registered successfully");
        }

        // Validates credentials and asks for OTP before issuing a JWT.
        [HttpPost("login")]
        public IActionResult Login(LoginDto dto)
        {
            dto.Email = NormalizeEmail(dto.Email);

            var user = _context.Users.FirstOrDefault(u => u.Email == dto.Email);

            if (user == null || string.IsNullOrWhiteSpace(user.Password))
            {
                return Unauthorized("Invalid email or password");
            }

            var isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.Password);

            if (!isPasswordValid)
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
            dto.Email = NormalizeEmail(dto.Email);

            var user = _context.Users.FirstOrDefault(u => u.Email == dto.Email);

            if (user == null ||
                string.IsNullOrWhiteSpace(user.Password) ||
                !BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
            {
                return Unauthorized("Invalid email or password");
            }

            if (!VerifyOtp(dto.Email, dto.Otp, "login"))
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

        private bool VerifyOtp(string email, string otp, string purpose)
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

            match.IsUsed = true;
            _context.SaveChanges();

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
                : "register";

        private static string NormalizeRole(string role)
        {
            var normalized = (role ?? string.Empty).Trim().ToLowerInvariant();

            return normalized is "jobseeker" or "employer" or "admin"
                ? normalized
                : string.Empty;
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
