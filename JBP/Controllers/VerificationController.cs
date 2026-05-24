using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;
using JBP.Data;
using JBP.Models;
using Microsoft.AspNetCore.Mvc;

namespace JBP.Controllers
{
    
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VerificationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public VerificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        private Candidate? GetCurrentCandidate()
        {
            var email =
                User.FindFirst(ClaimTypes.Email)
                    ?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return null;
            }

            return _context.Candidates
                .FirstOrDefault(c =>
                    c.Email == email);
        }

        [HttpPost("verify-aadhaar")]
        public IActionResult VerifyAadhaar(
    [FromBody] dynamic model)
        {
            string aadhaar =
    model.GetProperty("aadhaarNumber")
         .GetString();

            var candidate =
    GetCurrentCandidate();

            if (candidate == null)
            {
                return NotFound();
            }

            candidate.AadhaarVerified = true;

            candidate.AadhaarNumber = aadhaar;

            _context.SaveChanges();

            return Ok(new
            {
                success = true
            });
        }

        [HttpPost("verify-pan")]
        public IActionResult VerifyPan(
    [FromBody] dynamic model)
        {
            string pan =
    model.GetProperty("panNumber")
         .GetString();

            var candidate =
    GetCurrentCandidate();

            if (candidate == null)
            {
                return NotFound();
            }

            candidate.PanVerified = true;

            candidate.PanNumber = pan;

            _context.SaveChanges();

            return Ok(new
            {
                success = true
            });
        }

        [HttpPost("verify-uan")]
        public IActionResult VerifyUan(
    [FromBody] dynamic model)
        {
            string uan =
    model.GetProperty("uanNumber")
         .GetString();

            var candidate =
    GetCurrentCandidate();

            if (candidate == null)
            {
                return NotFound();
            }

            candidate.UanVerified = true;

            candidate.UanNumber = uan;

            candidate.EmploymentHistory =
                "Infosys,TCS,Wipro";

            _context.SaveChanges();

            return Ok(new
            {
                success = true
            });
        }
        [HttpGet("candidate-verification")]
        public IActionResult GetVerificationDetails()
        {
            var candidate =
                _context.Candidates
                    .FirstOrDefault();

            if (candidate == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                aadhaarVerified =
                    candidate.AadhaarVerified,

                panVerified =
                    candidate.PanVerified,

                uanVerified =
                    candidate.UanVerified,

                aadhaarNumber =
                    candidate.AadhaarNumber,

                panNumber =
                    candidate.PanNumber,

                uanNumber =
                    candidate.UanNumber
            });
        }
    }
}