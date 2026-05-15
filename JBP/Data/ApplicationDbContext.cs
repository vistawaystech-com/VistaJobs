using Microsoft.EntityFrameworkCore;
using Jobsy.API.Models;

namespace Jobsy.API.Data
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