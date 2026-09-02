using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Enums;
using StudentManagement.Core.Models;
using StudentManagement.Infrastructure.Hybrid.Identity;
using StudentManagement.Infrastructure.Hybrid.Security;
using System.Data;

namespace StudentManagement.Infrastructure.Hybrid
{
    public class HybridDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
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

            // ASP.NET Core Identity
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("IdentityUsers");
                entity.Property(x => x.IsActive).HasDefaultValue(true);
                entity.Property(x => x.CreatedAt).IsRequired();
            });

            modelBuilder.Entity<ApplicationRole>(entity =>
            {
                entity.ToTable("IdentityRoles");
                entity.Property(x => x.Description).HasMaxLength(250);
                entity.Property(x => x.Scope).HasConversion<string>().HasMaxLength(20);
                entity.Property(x => x.IsSystemRole).HasDefaultValue(true);
            });

            modelBuilder.Entity<IdentityUserRole<int>>()
                .ToTable("IdentityUserRoles");

            modelBuilder.Entity<IdentityUserClaim<int>>()
                .ToTable("IdentityUserClaims");

            modelBuilder.Entity<IdentityRoleClaim<int>>()
                .ToTable("IdentityRoleClaims");

            modelBuilder.Entity<IdentityUserLogin<int>>()
                .ToTable("IdentityUserLogins");

            modelBuilder.Entity<IdentityUserToken<int>>()
                .ToTable("IdentityUserTokens");

            // Copy over fluent API mappings
            modelBuilder.Entity<Student>(entity =>
            {
                entity.ToTable("Students");
                entity.HasKey(s => s.Id);
                entity.Property(s => s.RollNumber).IsRequired();
                entity.Property(s => s.FirstName).IsRequired();
                entity.Property(s => s.LastName).IsRequired();
                entity.Property(s => s.Email).IsRequired();
                entity.HasOne<School>()
                      .WithMany()
                      .HasForeignKey(x => x.SchoolId)
                      .OnDelete(DeleteBehavior.Restrict);
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

            //conversation history
            modelBuilder.Entity<CopilotConversationRecord>(
                entity =>
                {
                    entity.ToTable(
                        "CopilotConversations");

                    entity.HasKey(
                        x => x.Id);

                    entity.Property(
                            x => x.ThreadId)
                        .IsRequired()
                        .HasMaxLength(100);

                    entity.Property(
                            x => x.Title)
                        .IsRequired()
                        .HasMaxLength(200);

                    entity.Property(
                            x => x.LastRunId)
                        .HasMaxLength(200);

                    entity.Property(
                            x => x.CreatedAt)
                        .IsRequired();

                    entity.Property(
                            x => x.UpdatedAt)
                        .IsRequired();

                    entity.HasIndex(
                            x => new
                            {
                                x.UserId,
                                x.ThreadId
                            })
                        .IsUnique();

                    entity.Property(x => x.ActiveBranchId).HasMaxLength(100);

                    entity.HasIndex(
                        x => new
                        {
                            x.UserId,
                            x.UpdatedAt
                        });
                });

            //copilot turns
            modelBuilder.Entity<CopilotTurnRecord>(
                entity =>
                {
                    entity.ToTable("CopilotTurns");

                    entity.HasKey(x => x.Id);

                    entity.Property(x => x.ThreadId)
                        .IsRequired()
                        .HasMaxLength(100);

                    entity.Property(x => x.UserMessageId)
                        .IsRequired()
                        .HasMaxLength(200);

                    entity.Property(x => x.Status)
                        .HasConversion<string>()
                        .HasMaxLength(30)
                        .IsRequired();
                    entity.Property(x => x.CurrentVersionNumber)
                        .IsRequired()
                        .HasDefaultValue(1);

                    entity.Property(x => x.ActivitiesJson)
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    entity.Property(x => x.CreatedAt)
                        .IsRequired();

                    entity.Property(x => x.UpdatedAt)
                        .IsRequired();

                    entity.HasIndex(x => new
                    {
                        x.UserId,
                        x.ThreadId,
                        x.UserMessageId
                    })
                    .IsUnique();

                    entity.HasIndex(x => new
                    {
                        x.UserId,
                        x.ThreadId,
                        x.CreatedAt
                    });
                });

            // copilot turn versions
            modelBuilder.Entity<CopilotTurnVersionRecord>(
                entity =>
                {
                    entity.ToTable("CopilotTurnVersions");

                    entity.HasKey(x => x.Id);

                    entity.Property(x => x.ThreadId)
                        .IsRequired()
                        .HasMaxLength(100);

                    entity.Property(x => x.UserMessageId)
                        .IsRequired()
                        .HasMaxLength(200);

                    entity.Property(x => x.VersionNumber)
                        .IsRequired();

                    entity.Property(x => x.UserContent)
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    entity.Property(x => x.AssistantMessageId)
                        .HasMaxLength(200);

                    entity.Property(x => x.AssistantContent)
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    entity.Property(x => x.Status)
                        .HasConversion<string>()
                        .HasMaxLength(30)
                        .IsRequired();

                    entity.Property(x => x.ActivitiesJson)
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    entity.Property(x => x.CreatedAt)
                        .IsRequired();

                    entity.Property(x => x.UpdatedAt)
                        .IsRequired();

                    entity.HasIndex(x => new
                    {
                        x.UserId,
                        x.ThreadId,
                        x.UserMessageId,
                        x.VersionNumber
                    })
                    .IsUnique();

                    entity.HasIndex(x => new
                    {
                        x.UserId,
                        x.ThreadId,
                        x.UserMessageId
                    });
                });

            modelBuilder.Entity<CopilotConversationBranchRecord>(entity =>
            {
                entity.ToTable("CopilotConversationBranches");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.ThreadId).IsRequired().HasMaxLength(100);
                entity.Property(x => x.BranchId).IsRequired().HasMaxLength(100);
                entity.Property(x => x.ParentBranchId).HasMaxLength(100);
                entity.Property(x => x.BranchedFromUserMessageId).HasMaxLength(200);
                entity.Property(x => x.CreatedAt).IsRequired();
                entity.Property(x => x.UpdatedAt).IsRequired();

                entity.HasIndex(x => new
                {
                    x.UserId,
                    x.ThreadId,
                    x.BranchId
                }).IsUnique();

                entity.HasIndex(x => new
                {
                    x.UserId,
                    x.ThreadId,
                    x.ParentBranchId
                });
            });

            modelBuilder.Entity<CopilotBranchTurnRecord>(entity =>
            {
                entity.ToTable("CopilotBranchTurns");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.ThreadId).IsRequired().HasMaxLength(100);
                entity.Property(x => x.BranchId).IsRequired().HasMaxLength(100);
                entity.Property(x => x.UserMessageId).IsRequired().HasMaxLength(200);
                entity.Property(x => x.VersionNumber).IsRequired();
                entity.Property(x => x.Position).IsRequired();
                entity.Property(x => x.CreatedAt).IsRequired();

                entity.HasIndex(x => new
                {
                    x.UserId,
                    x.ThreadId,
                    x.BranchId,
                    x.Position
                }).IsUnique();

                entity.HasIndex(x => new
                {
                    x.UserId,
                    x.ThreadId,
                    x.BranchId,
                    x.UserMessageId
                }).IsUnique();
            });

            //school
            modelBuilder.Entity<School>(entity =>
            {
                entity.ToTable("Schools");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.Code)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.TimeZoneId)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.HasIndex(x => x.Code)
                    .IsUnique();
            });

            //school membership
            modelBuilder.Entity<SchoolMembership>(entity =>
            {
                entity.ToTable("SchoolMemberships");
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => new { x.SchoolId, x.UserId })
                    .IsUnique();

                entity.HasOne(x => x.School)
                    .WithMany()
                    .HasForeignKey(x => x.SchoolId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            //school user roles
            modelBuilder.Entity<SchoolUserRole>(entity =>
            {
                entity.ToTable("SchoolUserRoles");
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => new { x.SchoolMembershipId, x.RoleId })
                    .IsUnique();

                entity.HasOne(x => x.SchoolMembership)
                    .WithMany()
                    .HasForeignKey(x => x.SchoolMembershipId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Role)
                    .WithMany()
                    .HasForeignKey(x => x.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.AssignedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.AssignedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            //permissions
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.ToTable("Permissions");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Description)
                    .HasMaxLength(250);

                entity.HasIndex(x => x.Name)
                    .IsUnique();
            });

            //rolepermission
            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.ToTable("RolePermissions");
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => new { x.RoleId, x.PermissionId })
                    .IsUnique();

                entity.HasOne(x => x.Role)
                    .WithMany()
                    .HasForeignKey(x => x.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Permission)
                    .WithMany()
                    .HasForeignKey(x => x.PermissionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        public DbSet<Student> Students => Set<Student>();
        public DbSet<Course> Courses => Set<Course>();
        public DbSet<Enrollment> Enrollments => Set<Enrollment>();
        public DbSet<Fee> Fees => Set<Fee>();
        public DbSet<Attendance> Attendances => Set<Attendance>();
        public DbSet<AgentSessionRecord> AgentSessions
            => Set<AgentSessionRecord>();
        public DbSet<EnrollmentWorkflowRecord> EnrollmentWorkflowRecords
            => Set<EnrollmentWorkflowRecord>();
        public DbSet<WorkflowCheckpointRecord> WorkflowCheckpoints
            => Set<WorkflowCheckpointRecord>();
        public DbSet<EnrollmentWorkflowHistory> EnrollmentWorkflowHistories
            => Set<EnrollmentWorkflowHistory>();

        public DbSet<CopilotConversationRecord> CopilotConversations 
            => Set<CopilotConversationRecord>();
        public DbSet<CopilotTurnRecord> CopilotTurns
            => Set<CopilotTurnRecord>();
        public DbSet<CopilotTurnVersionRecord> CopilotTurnVersions
            => Set<CopilotTurnVersionRecord>();
        public DbSet<CopilotConversationBranchRecord> CopilotConversationBranches
            => Set<CopilotConversationBranchRecord>();
        public DbSet<CopilotBranchTurnRecord> CopilotBranchTurns
            => Set<CopilotBranchTurnRecord>();

        public DbSet<School> Schools => Set<School>();
        public DbSet<SchoolMembership> SchoolMemberships => Set<SchoolMembership>();
        public DbSet<SchoolUserRole> SchoolUserRoles => Set<SchoolUserRole>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    }
}