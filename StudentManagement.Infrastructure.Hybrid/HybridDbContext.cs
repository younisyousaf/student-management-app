using System.Data;
using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Models;
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
        }

        public DbSet<Student> Students => Set<Student>();
        public DbSet<Course> Courses => Set<Course>();
        public DbSet<Enrollment> Enrollments => Set<Enrollment>();
        public DbSet<Fee> Fees => Set<Fee>();
        public DbSet<User> Users => Set<User>();
    }
}