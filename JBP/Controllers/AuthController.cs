using JBP.Data;
using JBP.DTOs;
using JBP.Models;

using Microsoft.AspNetCore.Mvc;

using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;

using System.Security.Claims;

using System.Text;

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

        public AuthController(
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _context = context;

            _configuration = configuration;
        }

        // Registers a new user after blocking duplicate email addresses.
        [HttpPost("register")]
        public IActionResult Register(RegisterDto dto)
        {
            var exists = _context.Users
                .Any(u => u.Email == dto.Email);

            if (exists)
            {
                return BadRequest(
                    "Email already registered");
            }

            // Store only the BCrypt hash. The original password is never saved.
            var user = new User
            {
                FullName = dto.FullName,

                Email = dto.Email,

                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),

                Role = dto.Role
            };

            _context.Users.Add(user);

            _context.SaveChanges();

            return Ok(
                "User registered successfully");
        }

        // Validates credentials and issues a short-lived JWT for API authorization.
        [HttpPost("login")]
        public IActionResult Login(LoginDto dto)
        {
            var user = _context.Users
    .FirstOrDefault(u => u.Email == dto.Email);

            if (user == null)
            {
                return Unauthorized(
                    "Invalid email or password");
            }

            bool isPasswordValid =
                BCrypt.Net.BCrypt.Verify(
                    dto.Password,
                    user.Password);

            if (!isPasswordValid)
            {
                return Unauthorized(
                    "Invalid email or password");
            }

            // Claims are the source for role checks and current-user profile lookup.
            var claims = new[]
            {
                new Claim(
                    ClaimTypes.Name,
                    user.FullName),

                new Claim(
                    ClaimTypes.Email,
                    user.Email),

                new Claim(
                    ClaimTypes.Role,
                    user.Role)
            };

            // Token signing key must match Jwt:Key in appsettings.
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _configuration["Jwt:Key"]!));

            var creds =
                new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256);

            // Token lifetime is intentionally short for this local project.
            var token = new JwtSecurityToken(
                issuer:
                    _configuration["Jwt:Issuer"],

                audience:
                    _configuration["Jwt:Audience"],

                claims: claims,

                expires:
                    DateTime.Now.AddHours(2),

                signingCredentials: creds
            );

            var jwt =
                new JwtSecurityTokenHandler()
                    .WriteToken(token);

            return Ok(new
            {
                token = jwt,

                name = user.FullName,

                role = user.Role,

                email = user.Email
            });
        }
    }
}
