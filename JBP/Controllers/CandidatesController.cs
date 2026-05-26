using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JBP.Data;
using JBP.Models;

namespace JBP.Controllers
{
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
            _context.Candidates.Add(candidate);

            _context.SaveChanges();

            return Ok(candidate);
        }

        [HttpPost("upload-resume/{id}")]
public async Task<IActionResult> UploadResume(
    int id,
    IFormFile file)
        {
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

            // Allow PDF only
            if (!file.FileName.EndsWith(".pdf"))
            {
                return BadRequest(
                    "Only PDF files allowed");
            }

            // Unique file name
            var fileName =
                $"{Guid.NewGuid()}_{file.FileName}";

            var folderPath =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Uploads");

            // Create folder if not exists
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

            // Save path in DB
            candidate.ResumePath =
                $"/Uploads/{fileName}";

            _context.SaveChanges();

            return Ok(new
            {
                message = "Resume uploaded successfully",

                path = candidate.ResumePath
            });
        }
    }
}