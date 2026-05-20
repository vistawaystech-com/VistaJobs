using Microsoft.AspNetCore.Authorization;
using JBP.Data;
using JBP.Models;
using Microsoft.AspNetCore.Mvc;

namespace JBP.Controllers
{
    
    [Authorize(Roles = "employer")]
    [ApiController]
    [Route("api/[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public JobsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult GetJobs()
        {
            return Ok(_context.Jobs.ToList());
        }

        [HttpPost]
        public IActionResult AddJob(Job job)
        {
            _context.Jobs.Add(job);

            _context.SaveChanges();

            return Ok(job);
        }

        [HttpGet("dashboard")]
public IActionResult EmployerDashboard()
        {
            var jobs = _context.Jobs.ToList();

            var applications =
                _context.JobApplications.ToList();

            var dashboard = jobs.Select(job => new
            {
                JobId = job.Id,

                job.Title,

                job.CompanyName,

                job.Location,

                job.Salary,

                Applicants = applications
                    .Where(a => a.JobId == job.Id)
                    .Select(a => new { a.CandidateName, a.CandidateEmail, a.AppliedAt, Resume = _context.Candidates.Where(c => c.Email == a.CandidateEmail).Select(c => c.ResumePath).FirstOrDefault() })
                    .ToList()
            });

            return Ok(dashboard);
        }
    }
}