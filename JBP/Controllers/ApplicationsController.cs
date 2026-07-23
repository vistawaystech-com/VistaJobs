using JBP.Data;
using JBP.Models;
using JBP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JBP.API.Controllers
{
    // Job application flow starts here: a logged-in candidate applies to a job once.
    // Job application flow ikkada start avtundi: logged-in candidate oka job ki okasari apply cheyyagaladu.
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ApplicationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public ApplicationsController(
            ApplicationDbContext context,
            EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpPost]
        public IActionResult Apply(JobApplication application)
        {
            application.AppliedAt = DateTime.Now;

            var job = _context.Jobs.FirstOrDefault(j => j.Id == application.JobId);

            if (job != null)
            {
                application.JobTitle = job.Title;
            }

            var exists = _context.JobApplications.Any(a =>
                a.JobId == application.JobId &&
                a.CandidateEmail == application.CandidateEmail);

            if (exists)
            {
                return BadRequest("Already applied to this job");
            }

            _context.JobApplications.Add(application);

            _context.SaveChanges();

            // Application flow ends by saving the row and emailing the candidate confirmation.
            // Application flow ikkada end avtundi: row save ayi candidate ki confirmation email vellutundi.
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

                    <p>Thank you for using VistaJobs.</p>
                ").Wait();

            return Ok("Application submitted successfully");
        }

        // Application review flow: admin/employer dashboards read saved applications from here.
        // Application review flow: admin/employer dashboards ikkadi nundi saved applications chadutayi.
        [HttpGet]
        public IActionResult GetApplications()
        {
            return Ok(_context.JobApplications.ToList());
        }
    }
}
