using JBP.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JBP.Controllers
{
    // Admin-only reporting APIs for dashboard totals and raw lists.
    // These endpoints are useful for tester verification after user/job/application flows.
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

        // Summary counts shown in admin dashboard cards.
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

        // Raw user list for admin validation.
        [HttpGet("users")]
        public IActionResult Users()
        {
            return Ok(
                _context.Users.ToList());
        }

        // Raw job list for admin validation.
        [HttpGet("jobs")]
        public IActionResult Jobs()
        {
            return Ok(
                _context.Jobs.ToList());
        }

        // Raw application list for admin validation.
        [HttpGet("applications")]
        public IActionResult Applications()
        {
            return Ok(
                _context.JobApplications.ToList());
        }
    }
}
