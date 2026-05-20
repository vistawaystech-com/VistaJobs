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

        // REGISTER
        [HttpPost("register")]
        public IActionResult Register(RegisterDto dto)
        {
            // Check email already exists
            var exists = _context.Users
                .Any(u => u.Email == dto.Email);

            if (exists)
            {
                return BadRequest(
                    "Email already registered");
            }

            // Create user
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

        // LOGIN
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

            // JWT Claims
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

            // Secret Key
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _configuration["Jwt:Key"]!));

            var creds =
                new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256);

            // Create Token
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

                role = user.Role
            });
        }
    }
}