using Microsoft.EntityFrameworkCore;
using JBP.Models;

namespace JBP.Data
{
    // EF Core database context for the project.
    // Add new tables here as DbSet<T> so migrations can track them.
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Jobseeker submitted profiles.
        public DbSet<Candidate> Candidates { get; set; }

        // Employer job requirements.
        public DbSet<Job> Jobs { get; set; }

        // Login accounts and roles.
        public DbSet<User> Users { get; set; }

        public DbSet<EmailOtpVerification> EmailOtpVerifications { get; set; }

        // Candidate applications submitted against jobs.
        public DbSet<JobApplication> JobApplications { get; set; }

        // EPFO/PF-linked employment records returned by UAN verification.
        public DbSet<EmploymentHistory> EmploymentHistories { get; set; }

        // Enterprise audit trail for all document verification attempts.
        public DbSet<VerificationLog> VerificationLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EmploymentHistory>()
                .HasOne(history => history.Candidate)
                .WithMany()
                .HasForeignKey(history => history.CandidateId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VerificationLog>()
                .HasOne(log => log.Candidate)
                .WithMany()
                .HasForeignKey(log => log.CandidateId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
