using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using JBP.DTOs; 
using JBP.Data;
using JBP.Models;
using JBP.Services;
using Microsoft.AspNetCore.Mvc;

namespace JBP.Controllers
{
    // Verification flow starts here: each endpoint updates the logged-in candidate's identity status.
    // Verification flow ikkada start avtundi: logged-in candidate Aadhaar/PAN/UAN status update avtundi.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VerificationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly VerificationService _verificationService;

        public VerificationController(
            ApplicationDbContext context,
            VerificationService verificationService)
        {
            _context = context;
            _verificationService = verificationService;
        }

        private Candidate? GetCurrentCandidate()
        {
            // All verification actions belong to the candidate row for the JWT email.
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
    [FromBody] VerificationRequest model)
        {
            // Aadhaar verification begins on the frontend button and ends here with a verified/redirect response.
            // Aadhaar verification frontend button nundi start ayi ikkada verified/redirect response tho end avtundi.
            var candidate = GetCurrentCandidate();
            var email = candidate?.Email
                ?? User.FindFirst(ClaimTypes.Email)?.Value
                ?? string.Empty;

            var result =
                _verificationService.VerifyAadhaar(
                    candidate,
                    model.Number ?? string.Empty,
                    email);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }

            return Ok(new
            {
                success = true,
                verified = result.Verified,
                redirectUrl = result.RedirectUrl,
                message = result.Message
            });
        }

        [HttpPost("verify-pan")]
        public IActionResult VerifyPan(
    [FromBody] VerificationRequest model)
        {
            // PAN verification shares the same service path as Aadhaar and returns the saved verification state.
            // PAN verification Aadhaar laga same service path use chesi saved status return chestundi.
            var candidate = GetCurrentCandidate();
            var email = candidate?.Email
                ?? User.FindFirst(ClaimTypes.Email)?.Value
                ?? string.Empty;

            var result =
                _verificationService.VerifyPan(
                    candidate,
                    model.Number ?? string.Empty,
                    email);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }

            return Ok(new
            {
                success = true,
                verified = result.Verified,
                redirectUrl = result.RedirectUrl,
                message = result.Message
            });
        }

        [HttpPost("verify-uan")]
        public async Task<IActionResult> VerifyUan(
     [FromBody] VerificationRequest model)
        {
            // UAN verification calls the EPFO provider and returns employment history for experienced candidates.
            // UAN verification EPFO provider ni call chesi experienced candidates employment history return chestundi.
            var candidate = GetCurrentCandidate();
            var number = model.Number ?? string.Empty;
            var result =
                await _verificationService.VerifyUanAsync(
                    candidate,
                    number,
                    HttpContext.RequestAborted);


            if (!result.Verified)
            {
                return BadRequest(new
                {
                    success = false,
                    verified = false,
                    message = string.IsNullOrWhiteSpace(result.Message)
                        ? "UAN verification failed"
                        : result.Message
                });
            }

            return Ok(new
            {
                success = true,
                verified = true,
                message = string.IsNullOrWhiteSpace(result.Message)
                    ? "UAN verified"
                    : result.Message,
                uanNumber = number,
                employmentHistory = result.EmploymentHistory
            });
        }
        [HttpGet("candidate-verification")]
        public IActionResult GetVerificationDetails()
        {
            // Verification sync ends here: the frontend reloads saved numbers and disables completed buttons.
            // Verification sync ikkada end avtundi: saved numbers load ayi completed buttons disable avtayi.
            var candidate = GetCurrentCandidate();

            if (candidate == null)
            {
                return NotFound();
            }

            var employmentHistory =
                !string.IsNullOrWhiteSpace(candidate.EmploymentHistory)
                    ? (object)candidate.EmploymentHistory
                    : _context.EmploymentHistories
                        .Where(item => item.CandidateId == candidate.Id)
                        .OrderBy(item => item.DisplayOrder)
                        .Select(item => new
                        {
                            company = item.Company,
                            doj = item.Doj,
                            doe = item.Doe
                        })
                        .ToList();

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
                    candidate.UanNumber,

                employmentHistory,

                candidateType =
                    candidate.CandidateType
            });
        }

        [HttpGet("digilocker/callback")]
        [AllowAnonymous]
        public IActionResult DigiLockerCallback(
            [FromQuery] string? code,
            [FromQuery] string? state)
        {
            if (string.IsNullOrWhiteSpace(code) ||
                string.IsNullOrWhiteSpace(state))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "DigiLocker callback missing code or state"
                });
            }

            var result =
                _verificationService.PersistDigiLockerCallback(
                    code,
                    state);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }

            if (!string.IsNullOrWhiteSpace(result.RedirectUrl))
            {
                return Redirect(result.RedirectUrl);
            }

            return Ok(new
            {
                success = true,
                message = result.Message
            });
        }
    }
}
