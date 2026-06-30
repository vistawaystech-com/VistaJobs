using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using JBP.Data;
using JBP.Models;

namespace JBP.Controllers
{
    // Candidate profile APIs.
    // Jobseekers create/update one profile by email, employers read candidates for matching.
    [AllowAnonymous]
    //[Authorize(Roles = "jobseeker,employer")]
    [ApiController]
    [Route("api/[controller]")]
    public class CandidatesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CandidatesController(ApplicationDbContext context)
        {
            _context = context;
        }
        [AllowAnonymous]
        [HttpGet]
        public IActionResult GetCandidates()
        {
            // Used by employer matching screen to compare job skills with candidate skills.
            return Ok(_context.Candidates.ToList());
        }
        [HttpGet("{id}")]
        public IActionResult GetCandidateById(int id)
        {
            var candidate =
                _context.Candidates.Find(id);

            if (candidate == null)
            {
                return NotFound();
            }

            return Ok(candidate);
        }

        [HttpPost]
        public IActionResult AddCandidate(Candidate candidate)
        {
            // Logged-in jobseeker email is the profile key.
            // If the row exists, update it instead of creating duplicate profiles.
            var email =
                User.FindFirst(ClaimTypes.Email)?.Value ??
                User.Claims.FirstOrDefault(c => c.Type == "email")?.Value ??
                candidate.Email;

            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new
                {
                    message = "Email is required"
                });
            }

            candidate.Email = email;

            var existing =
                _context.Candidates
                    .FirstOrDefault(c => c.Email == email);

            candidate.AadhaarNumber =
                string.IsNullOrWhiteSpace(candidate.AadhaarNumber)
                    ? null
                    : candidate.AadhaarNumber.Trim().ToUpperInvariant();

            candidate.PanNumber =
                string.IsNullOrWhiteSpace(candidate.PanNumber)
                    ? null
                    : candidate.PanNumber.Trim().ToUpperInvariant();

            if (!candidate.AadhaarVerified ||
                string.IsNullOrWhiteSpace(candidate.AadhaarNumber))
            {
                return BadRequest(new
                {
                    message = "Please verify Aadhaar with DigiLocker before saving."
                });
            }

            if (!candidate.PanVerified ||
                string.IsNullOrWhiteSpace(candidate.PanNumber))
            {
                return BadRequest(new
                {
                    message = "Please verify PAN with DigiLocker before saving."
                });
            }

            var aadhaarAlreadyRegistered =
                _context.Candidates.Any(c =>
                    c.AadhaarNumber == candidate.AadhaarNumber &&
                    c.Email != email);

            if (aadhaarAlreadyRegistered)
            {
                return BadRequest(new
                {
                    message = "This Aadhaar is already registered."
                });
            }

            if (existing == null)
            {
                // First profile submission for this jobseeker.
                _context.Candidates.Add(candidate);
            }
            else
            {
                // Profile resubmission/edit keeps the same candidate id and replaces details.
                existing.FullName = candidate.FullName;
                existing.Phone = candidate.Phone;
                existing.Dob = candidate.Dob;
                existing.AadhaarVerified = candidate.AadhaarVerified;
                existing.PanVerified = candidate.PanVerified;
                existing.UanVerified = candidate.UanVerified;
                existing.PanNumber = candidate.PanNumber;
                existing.AadhaarNumber = candidate.AadhaarNumber;
                existing.UanNumber = candidate.UanNumber;
                existing.EmploymentHistory = candidate.EmploymentHistory;
                existing.Experience = candidate.Experience;
                existing.Skills = candidate.Skills;
                existing.Location = candidate.Location;
                existing.Salary = candidate.Salary;
                existing.CandidateType = candidate.CandidateType;

                if (!string.IsNullOrWhiteSpace(candidate.ResumePath))
                {
                    existing.ResumePath = candidate.ResumePath;
                }

                candidate = existing;
            }

            _context.SaveChanges();

            SaveEmploymentHistoryRows(candidate);

            _context.SaveChanges();

            return Ok(candidate);
        }

        private void SaveEmploymentHistoryRows(Candidate candidate)
        {
            var existingRows =
                _context.EmploymentHistories
                    .Where(item => item.CandidateId == candidate.Id);

            _context.EmploymentHistories.RemoveRange(existingRows);

            if (string.IsNullOrWhiteSpace(candidate.EmploymentHistory) ||
                candidate.CandidateType != "experienced")
            {
                return;
            }

            List<EmploymentHistory> history;

            try
            {
                history =
                    JsonSerializer.Deserialize<List<EmploymentHistory>>(
                        candidate.EmploymentHistory,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }) ?? new List<EmploymentHistory>();
            }
            catch
            {
                history = new List<EmploymentHistory>
                {
                    new EmploymentHistory
                    {
                        Company = candidate.EmploymentHistory
                    }
                };
            }

            for (var index = 0; index < history.Count; index++)
            {
                var item = history[index];

                if (string.IsNullOrWhiteSpace(item.Company))
                {
                    continue;
                }

                _context.EmploymentHistories.Add(new EmploymentHistory
                {
                    CandidateId = candidate.Id,
                    Uan = candidate.UanNumber ?? string.Empty,
                    Company = item.Company,
                    Doj = item.Doj ?? string.Empty,
                    Doe = item.Doe ?? string.Empty,
                    DisplayOrder = index + 1,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        [HttpPost("upload-resume/{id}")]
        public async Task<IActionResult> UploadResume(
     int id,
     IFormFile file)

        {
            // Resume upload is linked to the candidate id returned after profile save.
            var candidate =
                _context.Candidates.FirstOrDefault(
                    c => c.Id == id);

            if (candidate == null)
            {
                return NotFound(
                    "Candidate not found");
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(
                    "No file uploaded");
            }

            var allowedExtensions =
                new[] { ".pdf", ".doc", ".docx" };

            var extension =
                Path.GetExtension(file.FileName)
                    .ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new
                {
                    message = "Only PDF, DOC, and DOCX files allowed"
                });
            }

            // Prefix with a GUID so two users can upload files with the same original name.
            var fileName =
                $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";

            var folderPath =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Uploads");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var filePath =
                Path.Combine(folderPath, fileName);

            using (var stream =
                new FileStream(
                    filePath,
                    FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Store the public path only. The physical path stays on the server.
            candidate.ResumePath =
                $"/Uploads/{fileName}";

            _context.SaveChanges();

            var resumeUrl =
                $"{Request.Scheme}://{Request.Host}{candidate.ResumePath}";

            return Ok(new
            {
                message = "Resume uploaded successfully",

                path = candidate.ResumePath,

                url = resumeUrl
            });
        }
        [Authorize]
        [HttpGet("my-profile")]
        public IActionResult MyProfile()
        {
            // Frontend uses this on jobseeker login:
            // 200 = show submitted profile, 404 = show fresher/experienced chooser.
            var email =
                User.FindFirst(ClaimTypes.Email)?.Value ??
                User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

            if (email == null)
            {
                return Unauthorized();
            }

            var candidate = _context.Candidates
                .FirstOrDefault(c => c.Email == email);

            if (candidate == null)
            {
                return NotFound();
            }

            return Ok(candidate);
        }
    }
}
