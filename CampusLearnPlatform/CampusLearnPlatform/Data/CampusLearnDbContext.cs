using Microsoft.EntityFrameworkCore;
using CampusLearnPlatform.Models.Users;
using CampusLearnPlatform.Models.Learning;
using CampusLearnPlatform.Models.Communication;
using CampusLearnPlatform.Models.AI;
using CampusLearnPlatform.Controllers;
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
        public DbSet<ForumPostReply> ForumPostReplies { get; set; }
        public DbSet<TopicReply> TopicReplies { get; set; }
        public DbSet<PrivateMessage> Messages { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        // Junction tables
        public DbSet<StudentModule> StudentModules { get; set; }
        public DbSet<TutorModule> TutorModules { get; set; }
        public DbSet<Subscriptions> Subscriptions { get; set; }
        public DbSet<TutorSubscription> TutorSubscriptions { get; set; }

        // Chatbot entities
        public DbSet<ChatBot> ChatBots { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<FAQ> FAQs { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<EscalationRequest> EscalationRequests { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
           

            // Configure LearningMaterial FileType to use the enum properly
            modelBuilder.Entity<LearningMaterial>(entity =>
            {
                entity.Property(e => e.FileType)
                      .HasConversion<string>() // Convert enum to string for PostgreSQL
                      .HasColumnType("file_kind"); // Use the PostgreSQL enum type
            });

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
                entity.Property(e => e.StudentCreatorId).HasColumnName("student_creator_id");
                entity.Property(e => e.TutorCreatorId).HasColumnName("tutor_creator_id");

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
                entity.Property(e => e.PostContent).HasColumnName("post_content").IsRequired();
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.StudentAuthorId).HasColumnName("student_author_id");
                entity.Property(e => e.TutorAuthorId).HasColumnName("tutor_author_id");
                entity.Property(e => e.IsAnonymous).HasColumnName("is_anonymous").HasDefaultValue(false);
                entity.Property(e => e.UpvoteCount).HasColumnName("upvote_count").HasDefaultValue(0);
                entity.Property(e => e.DownvoteCount).HasColumnName("downvote_count").HasDefaultValue(0);
            });

            // ===== FORUM POST REPLY CONFIGURATION =====
            modelBuilder.Entity<ForumPostReply>(entity =>
            {
                entity.ToTable("forum_post_reply");
                entity.HasKey(e => e.ReplyId);
                entity.Property(e => e.ReplyId).HasColumnName("reply_id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.PostId).HasColumnName("post_id");
                entity.Property(e => e.StudentPosterId).HasColumnName("student_poster_id");
                entity.Property(e => e.TutorPosterId).HasColumnName("tutor_poster_id");
                entity.Property(e => e.ReplyContent).HasColumnName("reply_content");
                entity.Property(e => e.IsAnonymous).HasColumnName("is_anonymous");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            // ===== TOPIC REPLY CONFIGURATION =====
            modelBuilder.Entity<TopicReply>(entity =>
            {
                entity.ToTable("topic_reply");
                entity.HasKey(e => e.ReplyId);
                entity.Property(e => e.ReplyId).HasColumnName("reply_id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.TopicId).HasColumnName("topic_id");
                entity.Property(e => e.StudentPosterId).HasColumnName("student_poster_id");
                entity.Property(e => e.TutorPosterId).HasColumnName("tutor_poster_id");
                entity.Property(e => e.ReplyContent).HasColumnName("reply_content");
                entity.Property(e => e.IsAnonymous).HasColumnName("is_anonymous");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            // ===== PRIVATE MESSAGE CONFIGURATION =====
            modelBuilder.Entity<PrivateMessage>(entity =>
            {
                entity.ToTable("message");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("message_id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.MessageContent).HasColumnName("message_content");
                entity.Property(e => e.Timestamp).HasColumnName("sent_at");
                entity.Property(e => e.StudentSenderId).HasColumnName("student_sender_id");
                entity.Property(e => e.TutorSenderId).HasColumnName("tutor_sender_id");
                entity.Property(e => e.AdminSenderId).HasColumnName("admin_sender_id");
                entity.Property(e => e.StudentReceiverId).HasColumnName("student_receiver_id");
                entity.Property(e => e.TutorReceiverId).HasColumnName("tutor_receiver_id");
                entity.Property(e => e.AdminReceiverId).HasColumnName("admin_receiver_id");

                entity.Ignore(e => e.Content);
                entity.Ignore(e => e.SentAt);
                entity.Ignore(e => e.IsRead);
                entity.Ignore(e => e.Status);
                entity.Ignore(e => e.ReadAt);
                entity.Ignore(e => e.IsDeleted);
                entity.Ignore(e => e.TopicId);
                entity.Ignore(e => e.ParentMessageId);
                entity.Ignore(e => e.SenderId);
                entity.Ignore(e => e.SenderType);
                entity.Ignore(e => e.ReceiverId);
                entity.Ignore(e => e.ReceiverType);
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
                entity.Property(e => e.UploadedAt).HasColumnName("uploaded_at");
                entity.Property(e => e.StudentPosterId).HasColumnName("student_poster_id");
                entity.Property(e => e.TutorPosterId).HasColumnName("tutor_poster_id");
                entity.Property(e => e.AdminPosterId).HasColumnName("admin_poster_id");

                entity.Ignore(e => e.Description);
                entity.Ignore(e => e.FileName);
                entity.Ignore(e => e.FileSize);
                entity.Ignore(e => e.MaterialType);
                entity.Ignore(e => e.DownloadCount);
                entity.Ignore(e => e.IsPublic);
                entity.Ignore(e => e.UploadedByUserId);
                entity.Ignore(e => e.PosterId);
                entity.Ignore(e => e.PosterType);
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
                entity.Property(e => e.StudentRecipientId).HasColumnName("student_recipient_id");
                entity.Property(e => e.TutorRecipientId).HasColumnName("tutor_recipient_id");
                entity.Property(e => e.AdminRecipientId).HasColumnName("admin_recipient_id");

                entity.Ignore(e => e.Title);
                entity.Ignore(e => e.IsRead);
                entity.Ignore(e => e.NotificationType);
                entity.Ignore(e => e.ReadAt);
                entity.Ignore(e => e.IsSent);
                entity.Ignore(e => e.SentAt);
                entity.Ignore(e => e.ActionUrl);
                entity.Ignore(e => e.UserId);
                entity.Ignore(e => e.RecipientId);
                entity.Ignore(e => e.RecipientType);
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

            // ===== CHATBOT CONFIGURATION =====
            modelBuilder.Entity<ChatBot>(entity =>
            {
                entity.ToTable("chatbot");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
                entity.Property(e => e.Version).HasColumnName("version").HasMaxLength(50).IsRequired();
                entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
                entity.Property(e => e.LastUpdated).HasColumnName("last_updated");
                entity.Property(e => e.ConfidenceThreshold).HasColumnName("confidence_threshold").HasColumnType("decimal(3,2)").HasDefaultValue(0.70);
                entity.Property(e => e.TotalQueries).HasColumnName("total_queries").HasDefaultValue(0);
                entity.Property(e => e.SuccessfulQueries).HasColumnName("successful_queries").HasDefaultValue(0);
            });

            // ===== FAQ CONFIGURATION =====
            modelBuilder.Entity<FAQ>(entity =>
            {
                entity.ToTable("faq");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Question).HasColumnName("question").IsRequired();
                entity.Property(e => e.Answer).HasColumnName("answer").IsRequired();
                entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(100);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
                entity.Property(e => e.ViewCount).HasColumnName("view_count").HasDefaultValue(0);
                entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
                entity.Property(e => e.Keywords).HasColumnName("keywords");
                entity.Property(e => e.ChatbotId).HasColumnName("chatbot_id");

                // Relationship
                entity.HasOne(e => e.Chatbot)
                      .WithMany(c => c.FAQs)
                      .HasForeignKey(e => e.ChatbotId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== CHAT SESSION CONFIGURATION =====
            modelBuilder.Entity<ChatSession>(entity =>
            {
                entity.ToTable("chat_session");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.StartedAt).HasColumnName("started_at");
                entity.Property(e => e.EndedAt).HasColumnName("ended_at");
                entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
                entity.Property(e => e.SessionData).HasColumnName("session_data");
                entity.Property(e => e.MessageCount).HasColumnName("message_count").HasDefaultValue(0);
                entity.Property(e => e.WasEscalated).HasColumnName("was_escalated").HasDefaultValue(false);
                entity.Property(e => e.SessionSummary).HasColumnName("session_summary");

                // Both should be nullable
                entity.Property(e => e.StudentId).HasColumnName("student_id").IsRequired(false);
                entity.Property(e => e.TutorId).HasColumnName("tutor_id").IsRequired(false);

                entity.Property(e => e.ChatbotId).HasColumnName("chatbot_id");

                // Relationships
                entity.HasOne(e => e.Student)
                      .WithMany()
                      .HasForeignKey(e => e.StudentId)
                      .OnDelete(DeleteBehavior.Cascade)
                      .IsRequired(false);

                entity.HasOne(e => e.Tutor)
                      .WithMany()
                      .HasForeignKey(e => e.TutorId)
                      .OnDelete(DeleteBehavior.Cascade)
                      .IsRequired(false);

                entity.HasOne(e => e.Chatbot)
                      .WithMany(c => c.ChatSessions)
                      .HasForeignKey(e => e.ChatbotId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // check constraint
            modelBuilder.Entity<ChatSession>()
                .ToTable(t =>
                {
                    t.HasCheckConstraint(
                        "ck_one_user_only",
                        "(student_id IS NOT NULL AND tutor_id IS NULL) OR (student_id IS NULL AND tutor_id IS NOT NULL)"
                    );
                });

            // ===== CHAT MESSAGE CONFIGURATION =====
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.ToTable("chat_message");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.SessionId).HasColumnName("session_id");
                entity.Property(e => e.Message).HasColumnName("message").IsRequired();
                entity.Property(e => e.Response).HasColumnName("response").IsRequired();
                entity.Property(e => e.IsFromStudent).HasColumnName("is_from_student").HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.RequiresEscalation).HasColumnName("requires_escalation").HasDefaultValue(false);
            });

            // ===== ESCALATION REQUEST CONFIGURATION =====
            modelBuilder.Entity<EscalationRequest>(entity =>
            {
                entity.ToTable("escalation_request");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.SessionId).HasColumnName("session_id");
                entity.Property(e => e.StudentId).HasColumnName("student_id");
                entity.Property(e => e.Query).HasColumnName("query").IsRequired();
                entity.Property(e => e.Module).HasColumnName("module").HasMaxLength(200).IsRequired();
                entity.Property(e => e.Priority).HasColumnName("priority").HasMaxLength(50).HasDefaultValue("Medium");
                entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).HasDefaultValue("Pending");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            base.OnModelCreating(modelBuilder);


        }
    }
}
