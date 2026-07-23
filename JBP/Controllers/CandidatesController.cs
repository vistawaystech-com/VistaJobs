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
    // Candidate profile flow starts here: jobseekers save profiles and employers read safe match summaries.
    // Candidate profile flow ikkada start avtundi: jobseekers profiles save chestaru, employers safe summaries chustaru.
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CandidatesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CandidatesController(ApplicationDbContext context)
        {
            _context = context;
        }
        [Authorize(Roles = "employer,admin")]
        [HttpGet]
        public IActionResult GetCandidates()
        {
            // Employer matching read flow returns only non-sensitive candidate fields.
            // Employer matching read flow sensitive details kakunda safe candidate fields matrame return chestundi.
            return Ok(_context.Candidates
                .Select(candidate => ToEmployerCandidateDto(candidate))
                .ToList());
        }
        [Authorize(Roles = "employer,admin")]
        [HttpGet("{id}")]
        public IActionResult GetCandidateById(int id)
        {
            var candidate =
                _context.Candidates.Find(id);

            if (candidate == null)
            {
                return NotFound();
            }

            return Ok(ToEmployerCandidateDto(candidate));
        }

        [Authorize(Roles = "jobseeker")]
        [HttpPost]
        public IActionResult AddCandidate(Candidate candidate)
        {
            // Profile save uses the JWT email as the unique key and updates an existing row if present.
            // Profile save JWT email ni key ga use chesi existing row unte update chestundi.
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
            if (existing == null)
            {
                return BadRequest(new
                {
                    message = "Complete Aadhaar and PAN verification before saving profile."
                });
            }

            if (!existing.AadhaarVerified ||
                string.IsNullOrWhiteSpace(existing.AadhaarNumber))
            {
                return BadRequest(new
                {
                    message = "Aadhaar verification required."
                });
            }

            if (!existing.PanVerified ||
                string.IsNullOrWhiteSpace(existing.PanNumber))
            {
                return BadRequest(new
                {
                    message = "PAN verification required."
                });
            }

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
                _context.Candidates.Add(candidate);
            }
            else
            {
                existing.FullName = candidate.FullName;
                existing.Phone = candidate.Phone;
                existing.Dob = candidate.Dob;
                
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

            // Profile save ends by refreshing structured employment-history rows for UAN-backed history.
            // Profile save ikkada end avtundi: UAN employment history rows refresh avtayi.
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

        [Authorize(Roles = "jobseeker")]
        [HttpPost("upload-resume/{id}")]
        public async Task<IActionResult> UploadResume(
     int id,
     IFormFile file)

        {
            // Resume upload flow links the selected document to the candidate id returned after profile save.
            // Resume upload flow selected document ni profile save taruvata candidate id ki attach chestundi.
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

            // Resume upload ends by storing only the public URL path, not the physical server path.
            // Resume upload ikkada end avtundi: public URL path matrame save avtundi, server physical path kaadu.
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
            // My-profile flow lets the frontend decide between submitted-summary and new-profile form.
            // My-profile flow frontend ki submitted summary leka new profile form decide cheyyadaniki use avtundi.
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

        private static object ToEmployerCandidateDto(Candidate candidate) => new
        {
            candidate.Id,
            candidate.FullName,
            candidate.Skills,
            candidate.Experience,
            candidate.Location,
            candidate.Salary,
            candidate.CandidateType,
            candidate.ResumePath,
            candidate.AadhaarVerified,
            candidate.PanVerified,
            candidate.UanVerified
        };
    }
}
