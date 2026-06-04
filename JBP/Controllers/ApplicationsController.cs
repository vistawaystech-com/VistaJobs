using JBP.Data;
using JBP.Models;
using JBP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JBP.API.Controllers
{
    // Job application APIs.
    // A candidate can apply once per job; a confirmation email is sent after save.
    [Authorize]

    [ApiController]

    [Route("api/[controller]")]
    public class ApplicationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public ApplicationsController(ApplicationDbContext context, EmailService emailService) { _context = context; _emailService = emailService; }    

        // Creates an application after checking duplicate candidate/job combination.
        [HttpPost]
        public IActionResult Apply(JobApplication application)
        {
            application.AppliedAt = DateTime.Now;

            // Keep a copy of the job title on the application for easy dashboard display.
            var job = _context.Jobs.FirstOrDefault(j => j.Id == application.JobId); if (job != null) { application.JobTitle = job.Title; }

           var exists = _context.JobApplications.Any(a =>

    a.JobId == application.JobId &&

    a.CandidateEmail ==
        application.CandidateEmail
);

            if (exists)
            {
                return BadRequest(
                    "Already applied to this job"
                );
            }

            _context.JobApplications.Add(application);

            _context.SaveChanges();

            // Email failures will currently fail the request; handle with care in production.
            _emailService.SendEmail(
    application.CandidateEmail,

    "Job Application Submitted",

    $@"
        <h2>Application Successful</h2>

        <p>Hello {application.CandidateName},</p>

        <p>
            Your application for
            <strong>{application.JobTitle}</strong>
            has been submitted successfully.
        </p>

        <p>Thank you for using Jobsy 🚀</p>
    ").Wait();

            return Ok(
                "Application submitted successfully");
        }

        // Admin/testing endpoint for reviewing all application rows.
        [HttpGet]
        public IActionResult GetApplications()
        {
            return Ok(
                _context.JobApplications.ToList());
        }
    }
}
