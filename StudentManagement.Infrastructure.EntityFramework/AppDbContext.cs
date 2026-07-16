using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Models;

namespace StudentManagement.Infrastructure.EntityFramework
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Student Mapping
            modelBuilder.Entity<Student>(entity =>
            {
                entity.ToTable("Students");
                entity.HasKey(s => s.Id);

                // Map backing fields or allow private setter write access
                entity.Property(s => s.RollNumber).IsRequired();
                entity.Property(s => s.FirstName).IsRequired();
                entity.Property(s => s.LastName).IsRequired();
                entity.Property(s => s.Email).IsRequired();
            });

            // 2. Course Mapping
            modelBuilder.Entity<Course>(entity =>
            {
                entity.ToTable("Courses");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Code).IsRequired();
                entity.Property(c => c.Name).IsRequired();
                entity.Property(c => c.FeeAmount).HasColumnType("decimal(18,2)");
            });

            // 3. Enrollment Mapping
            modelBuilder.Entity<Enrollment>(entity =>
            {
                entity.ToTable("Enrollments");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).IsRequired();
            });

            // 4. Fee Mapping
            modelBuilder.Entity<Fee>(entity =>
            {
                entity.ToTable("Fees");
                entity.HasKey(f => f.Id);
                entity.Property(f => f.AmountDue).HasColumnType("decimal(18,2)");
                entity.Property(f => f.AmountPaid).HasColumnType("decimal(18,2)");
            });
        }

        public DbSet<Student> Students => Set<Student>();
        public DbSet<Course> Courses => Set<Course>();
        public DbSet<Enrollment> Enrollments => Set<Enrollment>();
        public DbSet<Fee> Fees => Set<Fee>();
    }
}