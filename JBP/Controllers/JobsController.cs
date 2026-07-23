using Microsoft.AspNetCore.Authorization;
using JBP.Data;
using JBP.Models;
using Microsoft.AspNetCore.Mvc;

namespace JBP.Controllers
{
    // Job flow starts here: employers save requirements and dashboards read applicant data.
    // Job flow ikkada start avtundi: employers requirements save chestaru, dashboards applicants chustayi.
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
            // Public list; no login required to browse jobs.
            return Ok(_context.Jobs.ToList());
        }

        [HttpPost]
        public IActionResult AddJob(Job job)
        {
            // Employer search/job requirement is saved before the frontend renders matching candidates.
            // Employer search/job requirement save ayyaka frontend matching candidates chupistundi.
            _context.Jobs.Add(job);

            _context.SaveChanges();

            return Ok(job);
        }

        [HttpGet("dashboard")]
        public IActionResult EmployerDashboard()
        {
            // Employer dashboard flow ends here by joining jobs, applications, and resume paths.
            // Employer dashboard flow ikkada end avtundi: jobs, applications, resumes join ayi return avtayi.
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
                    .Select(a => new
                    {
                        a.CandidateName,
                        a.CandidateEmail,
                        a.AppliedAt,
                        Resume = _context.Candidates
                            .Where(c => c.Email == a.CandidateEmail)
                            .Select(c => c.ResumePath)
                            .FirstOrDefault()
                    })
                    .ToList()
            });

            return Ok(dashboard);
        }
    }
}
