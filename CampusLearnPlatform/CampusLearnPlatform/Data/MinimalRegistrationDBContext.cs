using CampusLearnPlatform.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace CampusLearnPlatform.Data
{
    public class MinimalRegistrationDBContext: DbContext
    {
        public MinimalRegistrationDBContext(DbContextOptions<MinimalRegistrationDBContext> options) : base(options) { }

        // Only Student - no other entities to cause relationship problems
        public DbSet<Student> Students { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure Student entity explicitly with exact database mapping
            modelBuilder.Entity<Student>(entity =>
            {
                entity.ToTable("student");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("student_id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.PasswordHash).HasColumnName("password");
                entity.Property(e => e.ProfileInfo).HasColumnName("profile_info");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
