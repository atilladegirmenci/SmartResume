using Microsoft.EntityFrameworkCore;
using SmartResume.Data.Models;

namespace SmartResume.Data
{
    public class ApplicationDbContext : DbContext
    {
        // This constructor is essential. It allows our application (in Program.cs)
        // to pass in the configuration, most importantly the database connection string.
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // --- DbSets ---
        // Each DbSet property tells Entity Framework that we want a table in our database based on the corresponding model.

        public DbSet<User> Users { get; set; }
        public DbSet<Resume> Resumes { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<Experience> Experiences { get; set; }
        public DbSet<Education> Educations { get; set; }

        // for the many-to-many relationship.
        public DbSet<ResumeSkill> ResumeSkills { get; set; }


        // --- Fluent API Configuration ---
        // This method is optional, but it's the best place to define complex configurations that can't be expressed with simple attributes.
        // We use it here to define UNIQUE constraints for data integrity.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Call the base method first
            base.OnModelCreating(modelBuilder);

            // 1. Ensure User.Email is unique
            // We want to prevent two users from registering with the same email.
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // 2. Ensure Skill.SkillName is unique
            // This enforces that our 'Skills' table acts as a true lookup dictionary.
            // "Java" should only exist once in the Skills table.
            modelBuilder.Entity<Skill>()
                .HasIndex(s => s.SkillName)
                .IsUnique();

            // 3. Ensure a Resume cannot have the same Skill twice.
            // We create a composite unique index on the junction table (ResumeSkills).
            // This prevents duplicate entries like (ResumeID=1, SkillID=5) and (ResumeID=1, SkillID=5).
            modelBuilder.Entity<ResumeSkill>()
                .HasIndex(rs => new { rs.ResumeID, rs.SkillID })
                .IsUnique();
        }
    }
}
