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

            // ===== STUDENT CONFIGURATION =====
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

            // ===== TUTOR CONFIGURATION =====
            modelBuilder.Entity<Tutor>(entity =>
            {
                entity.ToTable("tutor");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("tutor_id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.PasswordHash).HasColumnName("password");
                entity.Property(e => e.Experience).HasColumnName("experience");

              
                entity.Ignore(e => e.YearsExperience);
                entity.Ignore(e => e.TotalRatings);
                entity.Ignore(e => e.IsVerified);
                entity.Ignore(e => e.Modules);            
                entity.Ignore(e => e.ReceivedMessages);     
                entity.Ignore(e => e.CreatedMaterials);     
            });

            // ===== ADMINISTRATOR CONFIGURATION =====
            modelBuilder.Entity<Administrator>(entity =>
            {
                entity.ToTable("admin");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("admin_id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.Password).HasColumnName("password");
            });

            // ===== MODULE CONFIGURATION =====
            modelBuilder.Entity<Module>(entity =>
            {
                entity.ToTable("module");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("module_id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.ModuleName).HasColumnName("module_name");
                entity.Property(e => e.Description).HasColumnName("description");

                // Ignore properties that don't exist in database
                entity.Ignore(e => e.ModuleCode);
                entity.Ignore(e => e.Credits);
                entity.Ignore(e => e.AcademicYear); 
                entity.Ignore(e => e.Semester);
                entity.Ignore(e => e.IsActive);
                entity.Ignore(e => e.Topics);      
                entity.Ignore(e => e.Tutors);      
                entity.Ignore(e => e.Students);    
            });

            // ===== TOPIC CONFIGURATION =====
            modelBuilder.Entity<Topic>(entity =>
            {
                entity.ToTable("topic");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("topic_id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.Title).HasColumnName("title");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.ModuleId).HasColumnName("module_id");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");

                // Ignore properties that don't exist in database
                entity.Ignore(e => e.UpdatedAt);
                entity.Ignore(e => e.Status);
                entity.Ignore(e => e.ViewCount);
                entity.Ignore(e => e.Priority);
                entity.Ignore(e => e.IsArchived);
                entity.Ignore(e => e.StudentId);
                entity.Ignore(e => e.CreatedBy);
                entity.Ignore(e => e.Module);
                entity.Ignore(e => e.Subscriptions);
                entity.Ignore(e => e.Materials);
                entity.Ignore(e => e.Messages);
            });

            // ===== FORUM POSTS CONFIGURATION =====
            modelBuilder.Entity<ForumPosts>(entity =>
            {
                entity.ToTable("forum_post");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("post_id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.TopicId).HasColumnName("topic_id");
                entity.Property(e => e.AuthorId).HasColumnName("author_id");
                entity.Property(e => e.AuthorType).HasColumnName("author_type");
                entity.Property(e => e.PostContent).HasColumnName("post_content");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");

                // Ignore properties that don't exist in database
                entity.Ignore(e => e.Title);
                entity.Ignore(e => e.Content);
                entity.Ignore(e => e.PostedAt);
                entity.Ignore(e => e.IsAnonymous);
                entity.Ignore(e => e.UpvoteCount);
                entity.Ignore(e => e.DownvoteCount);
                entity.Ignore(e => e.IsModerated);
                entity.Ignore(e => e.IsApproved);
                entity.Ignore(e => e.ModerationNotes);
                entity.Ignore(e => e.PostedById);
                entity.Ignore(e => e.ModuleId);        // ← This was causing the error!
                entity.Ignore(e => e.ParentPostId);    // ← This too!
                entity.Ignore(e => e.PostedBy);
                entity.Ignore(e => e.Module);
                entity.Ignore(e => e.ParentPost);
                entity.Ignore(e => e.Replies);
            });

            // ===== PRIVATE MESSAGE CONFIGURATION =====
            modelBuilder.Entity<PrivateMessage>(entity =>
            {
                entity.ToTable("message");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("message_id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.MessageContent).HasColumnName("message_content");
                entity.Property(e => e.SenderType).HasColumnName("sender_type");
                entity.Property(e => e.SenderId).HasColumnName("sender_id");
                entity.Property(e => e.ReceiverType).HasColumnName("receiver_type");
                entity.Property(e => e.ReceiverId).HasColumnName("receiver_id");
                entity.Property(e => e.Timestamp).HasColumnName("sent_at");

                // Ignore properties that don't exist in database
                entity.Ignore(e => e.Content);
                entity.Ignore(e => e.SentAt);
                entity.Ignore(e => e.IsRead);
                entity.Ignore(e => e.Status);
                entity.Ignore(e => e.ReadAt);
                entity.Ignore(e => e.IsDeleted);
                entity.Ignore(e => e.TopicId);         // ← This was causing errors!
                entity.Ignore(e => e.ParentMessageId); // ← This too!
                entity.Ignore(e => e.Sender);
                entity.Ignore(e => e.Receiver);
                entity.Ignore(e => e.Topic);
                entity.Ignore(e => e.ParentMessage);
            });

            // ===== LEARNING MATERIAL CONFIGURATION =====
            modelBuilder.Entity<LearningMaterial>(entity =>
            {
                entity.ToTable("learning_material");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("material_id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.Title).HasColumnName("title");
                entity.Property(e => e.FilePath).HasColumnName("file_path");
                entity.Property(e => e.FileType).HasColumnName("file_type");
                entity.Property(e => e.TopicId).HasColumnName("topic_id");
                entity.Property(e => e.PosterId).HasColumnName("poster_id");
                entity.Property(e => e.PosterType).HasColumnName("poster_type");
                entity.Property(e => e.UploadedAt).HasColumnName("uploaded_at");

                // Ignore properties that don't exist in database
                entity.Ignore(e => e.Description);
                entity.Ignore(e => e.FileName);
                entity.Ignore(e => e.FileSize);
                entity.Ignore(e => e.MaterialType);
                entity.Ignore(e => e.DownloadCount);
                entity.Ignore(e => e.IsPublic);
                entity.Ignore(e => e.UploadedByUserId);
                entity.Ignore(e => e.Topic);
                entity.Ignore(e => e.UploadedBy);
            });

            // ===== NOTIFICATION CONFIGURATION =====
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("notification");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("notification_id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.Message).HasColumnName("message");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.RecipientId).HasColumnName("recipient_id");
                entity.Property(e => e.RecipientType).HasColumnName("recipient_type");

                // Ignore properties that don't exist in database
                entity.Ignore(e => e.Title);
                entity.Ignore(e => e.IsRead);
                entity.Ignore(e => e.NotificationType);
                entity.Ignore(e => e.ReadAt);
                entity.Ignore(e => e.IsSent);
                entity.Ignore(e => e.SentAt);
                entity.Ignore(e => e.ActionUrl);
                entity.Ignore(e => e.UserId);
                entity.Ignore(e => e.User);
            });

            // ===== JUNCTION TABLES =====
            modelBuilder.Entity<StudentModule>(entity =>
            {
                entity.ToTable("student_module");
                entity.Property(e => e.Id).HasColumnName("student_module_id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.StudentId).HasColumnName("student_id");
                entity.Property(e => e.ModuleId).HasColumnName("module_id");
                entity.Property(e => e.Grade).HasColumnName("grade");
            });

            modelBuilder.Entity<TutorModule>(entity =>
            {
                entity.ToTable("tutor_module");
                entity.Property(e => e.Id).HasColumnName("tutor_module_id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.TutorId).HasColumnName("tutor_id");
                entity.Property(e => e.ModuleId).HasColumnName("module_id");
            });

            modelBuilder.Entity<Subscriptions>(entity =>
            {
                entity.ToTable("subscription");
                entity.Property(e => e.Id).HasColumnName("subscription_id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.StudentId).HasColumnName("student_id");
                entity.Property(e => e.TopicId).HasColumnName("topic_id");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");

                // Ignore properties that don't exist in database
                entity.Ignore(e => e.SubscribedAt);
                entity.Ignore(e => e.IsActive);
                entity.Ignore(e => e.ReceiveNotifications);
                entity.Ignore(e => e.Student);
                entity.Ignore(e => e.Topic);
            });

            modelBuilder.Entity<TutorSubscription>(entity =>
            {
                entity.ToTable("tutor_subscription");
                entity.Property(e => e.Id).HasColumnName("subscription_id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.StudentId).HasColumnName("student_id");
                entity.Property(e => e.TutorId).HasColumnName("tutor_id");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            modelBuilder.Entity<TutorTopic>(entity =>
            {
                entity.ToTable("tutor_topic");
                entity.Property(e => e.Id).HasColumnName("tutor_topic_id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.TutorId).HasColumnName("tutor_id");
                entity.Property(e => e.TopicId).HasColumnName("topic_id");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
