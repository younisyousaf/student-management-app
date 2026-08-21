using System.Data;
using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Models;
using StudentManagement.Core.Enums;
using Microsoft.Data.SqlClient;

namespace StudentManagement.Infrastructure.Hybrid
{
    public class HybridDbContext : DbContext
    {
        public HybridDbContext(DbContextOptions<HybridDbContext> options) : base(options)
        {
        }

        public IDbConnection Connection
        {
            get
            {
                var conn = Database.GetDbConnection();
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }
                return conn;
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Copy over fluent API mappings
            modelBuilder.Entity<Student>(entity =>
            {
                entity.ToTable("Students");
                entity.HasKey(s => s.Id);
                entity.Property(s => s.RollNumber).IsRequired();
                entity.Property(s => s.FirstName).IsRequired();
                entity.Property(s => s.LastName).IsRequired();
                entity.Property(s => s.Email).IsRequired();
            });

            modelBuilder.Entity<Course>(entity =>
            {
                entity.ToTable("Courses");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Code).IsRequired();
                entity.Property(c => c.Name).IsRequired();
                entity.Property(c => c.FeeAmount).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<Enrollment>(entity =>
            {
                entity.ToTable("Enrollments");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).IsRequired();
            });

            modelBuilder.Entity<Fee>(entity =>
            {
                entity.ToTable("Fees");
                entity.HasKey(f => f.Id);
                entity.Property(f => f.AmountDue).HasColumnType("decimal(18,2)");
                entity.Property(f => f.AmountPaid).HasColumnType("decimal(18,2)");
            });

            //Users
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.Role).IsRequired().HasMaxLength(20);
            });

            //Attendances
            modelBuilder.Entity<Attendance>(entity =>
            {
                entity.ToTable("Attendances");
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Status).HasConversion<byte>();
                entity.Property(a => a.Remarks).HasMaxLength(255);
            });

            // Agent Session Mapping
            modelBuilder.Entity<AgentSessionRecord>(entity =>
            {
                entity.ToTable("AgentSessions");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.SessionId)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.HasIndex(x => x.SessionId)
                      .IsUnique();

                entity.Property(x => x.SerializedSession)
                      .IsRequired()
                      .HasColumnType("nvarchar(max)");

                entity.Property(x => x.PendingApprovalRequestId)
                    .HasMaxLength(200);

                entity.Property(x => x.PendingApprovalCallId)
                    .HasMaxLength(200);

                entity.Property(x => x.PendingApprovalFunctionName)
                    .HasMaxLength(200);

                entity.Property(x => x.PendingApprovalArgumentsJson)
                    .HasColumnType("nvarchar(max)");

                entity.Property(x => x.CreatedAt)
                      .IsRequired();

                entity.Property(x => x.UpdatedAt)
                      .IsRequired();

                entity.Property(x => x.ExpiresAt)
                      .IsRequired(false);
            });

            //enrollment workflow
            modelBuilder.Entity<EnrollmentWorkflowRecord>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.RequestId)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.HasIndex(x => x.RequestId)
                    .IsUnique();

                entity.Property(x => x.Status)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.Property(x => x.CheckpointRunId)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.CheckpointId)
                    .HasMaxLength(200)
                    .IsRequired();
                entity.Property(x => x.ActiveKey)
                    .HasMaxLength(100);

                entity.HasIndex(x => x.ActiveKey)
                    .IsUnique()
                    .HasFilter("[ActiveKey] IS NOT NULL");
            });

            //workflow checkpoint
            modelBuilder.Entity<WorkflowCheckpointRecord>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.SessionId)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.CheckpointId)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.ParentCheckpointId)
                    .HasMaxLength(200);

                entity.Property(x => x.CheckpointData)
                    .IsRequired();

                entity.HasIndex(
                    x => new
                    {
                        x.SessionId,
                        x.CheckpointId
                    })
                    .IsUnique();
            });

            //workflow history
            modelBuilder.Entity<EnrollmentWorkflowHistory>(
                entity =>
                {
                    entity.ToTable(
                        "EnrollmentWorkflowHistories");

                    entity.HasKey(x => x.Id);

                    entity.Property(x => x.RequestId)
                        .HasMaxLength(100)
                        .IsRequired();

                    entity.Property(x => x.EventType)
                        .HasMaxLength(100)
                        .IsRequired();

                    entity.Property(x => x.ExecutorId)
                        .HasMaxLength(200);

                    entity.Property(x => x.Message)
                        .HasMaxLength(1000);

                    entity.Property(x => x.OccurredAt)
                        .IsRequired();

                    entity.HasIndex(x => x.RequestId);
                });
        }

        public DbSet<Student> Students => Set<Student>();
        public DbSet<Course> Courses => Set<Course>();
        public DbSet<Enrollment> Enrollments => Set<Enrollment>();
        public DbSet<Fee> Fees => Set<Fee>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Attendance> Attendances => Set<Attendance>();
        public DbSet<AgentSessionRecord> AgentSessions
            => Set<AgentSessionRecord>();
        public DbSet<EnrollmentWorkflowRecord> EnrollmentWorkflowRecords 
            => Set<EnrollmentWorkflowRecord>();
        public DbSet<WorkflowCheckpointRecord> WorkflowCheckpoints 
            => Set<WorkflowCheckpointRecord>();
        public DbSet<EnrollmentWorkflowHistory> EnrollmentWorkflowHistories 
            => Set<EnrollmentWorkflowHistory>();
    }
}