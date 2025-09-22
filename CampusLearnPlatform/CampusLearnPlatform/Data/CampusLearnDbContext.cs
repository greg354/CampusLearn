using Microsoft.EntityFrameworkCore;
using CampusLearnPlatform.Models.Users;
using CampusLearnPlatform.Models.Learning;
using CampusLearnPlatform.Models.Communication;
using CampusLearnPlatform.Models.AI;
using CampusLearnPlatform.Models.System;

namespace CampusLearnPlatform.Data
{
    public class CampusLearnDbContext : DbContext
    {
        public CampusLearnDbContext(DbContextOptions<CampusLearnDbContext> options) : base(options) { }


        // User entities
        public DbSet<Student> Students { get; set; }
        public DbSet<Tutor> Tutors { get; set; }
        public DbSet<Administrator> Administrators { get; set; }

        // Learning entities
        public DbSet<Module> Modules { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<LearningMaterial> LearningMaterials { get; set; }

        // Communication entities
        public DbSet<ForumPosts> ForumPosts { get; set; }
        public DbSet<PrivateMessage> Messages { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        // Junction tables
        public DbSet<StudentModule> StudentModules { get; set; }
        public DbSet<TutorModule> TutorModules { get; set; }
        public DbSet<Subscriptions> Subscriptions { get; set; }
        public DbSet<TutorSubscription> TutorSubscriptions { get; set; }
        public DbSet<TutorTopic> TutorTopics { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Ignore<User>();
            modelBuilder.Ignore<UserProfile>();
            // Configure PostgreSQL to generate UUIDs for all entities
            modelBuilder.Entity<Student>()
                .Property(s => s.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            modelBuilder.Entity<Tutor>()
                .Property(t => t.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            modelBuilder.Entity<Administrator>()
                .Property(a => a.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            modelBuilder.Entity<Module>()
                .Property(m => m.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            modelBuilder.Entity<Topic>()
                .Property(t => t.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            modelBuilder.Entity<LearningMaterial>()
                .Property(lm => lm.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            modelBuilder.Entity<ForumPosts>()
                .Property(fp => fp.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            modelBuilder.Entity<PrivateMessage>()
                .Property(pm => pm.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            modelBuilder.Entity<Notification>()
                .Property(n => n.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            modelBuilder.Entity<StudentModule>()
                .Property(sm => sm.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            modelBuilder.Entity<TutorModule>()
                .Property(tm => tm.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            modelBuilder.Entity<Subscriptions>()
                .Property(s => s.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            modelBuilder.Entity<TutorSubscription>()
                .Property(ts => ts.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            modelBuilder.Entity<TutorTopic>()
                .Property(tt => tt.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            base.OnModelCreating(modelBuilder);
        }
    }
}
