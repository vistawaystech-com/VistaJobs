using Jobsy.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.API.Controllers
{
    [Authorize(Roles = "admin")]

    [ApiController]

    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // DASHBOARD
        [HttpGet("dashboard")]
        public IActionResult Dashboard()
        {
            var dashboard = new
            {
                TotalUsers =
                    _context.Users.Count(),

                TotalCandidates =
                    _context.Candidates.Count(),

                TotalJobs =
                    _context.Jobs.Count(),

                TotalApplications =
                    _context.JobApplications.Count()
            };

            return Ok(dashboard);
        }

        // ALL USERS
        [HttpGet("users")]
        public IActionResult Users()
        {
            return Ok(
                _context.Users.ToList());
        }

        // ALL JOBS
        [HttpGet("jobs")]
        public IActionResult Jobs()
        {
            return Ok(
                _context.Jobs.ToList());
        }

        // ALL APPLICATIONS
        [HttpGet("applications")]
        public IActionResult Applications()
        {
            return Ok(
                _context.JobApplications.ToList());
        }
    }
}