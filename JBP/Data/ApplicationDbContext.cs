using Microsoft.EntityFrameworkCore;
using JBP.Models;

namespace JBP.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Candidate> Candidates { get; set; }

        public DbSet<Job> Jobs { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<JobApplication> JobApplications { get; set; }
    }
}