using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Tutorz.Domain.Entities;
using Tutorz.Infrastructure.Configurations;

namespace Tutorz.Infrastructure.Data
{
    public class TutorzDbContext : DbContext
    {
        public TutorzDbContext(DbContextOptions<TutorzDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Tutor> Tutors { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Institute> Institutes { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserSequence> UserSequences { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Province> Provinces { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<Hall> Halls { get; set; }
        public DbSet<InstituteStudent> InstituteStudents { get; set; }
        public DbSet<InstituteTutor> InstituteTutors { get; set; }
        public DbSet<InstituteJoinRequest> InstituteJoinRequests { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<SmsLog> SmsLogs { get; set; }
        public DbSet<ApiUsageLog> ApiUsageLogs { get; set; }
        public DbSet<ApiDailyUsageSummary> ApiDailyUsageSummaries { get; set; }
        public DbSet<APIMonthlyUsageSummary> APIMonthlyUsageSummaries { get; set; }
        public DbSet<ClassPayment> ClassPayments { get; set; }
        public DbSet<Bank> Banks { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        // Global application settings (e.g., MinTokenDate for forced logout on deploy)
        public DbSet<AppSetting> AppSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Tutor>()
                .HasOne(t => t.User)
                .WithOne(u => u.Tutor)
                .HasForeignKey<Tutor>(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Student>()
                .HasOne(s => s.User)
                .WithMany(u => u.Students)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Class>()
                .Property(c => c.Fee)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Institute>()
                .HasOne(i => i.User)
                .WithOne()
                .HasForeignKey<Institute>(i => i.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Enrollment>()
                .HasIndex(e => new { e.StudentId, e.ClassId })
                .IsUnique();

            modelBuilder.Entity<InstituteStudent>()
                .HasKey(is_ => new { is_.InstituteId, is_.StudentId });

            modelBuilder.Entity<InstituteStudent>()
                .HasOne(is_ => is_.Institute)
                .WithMany(i => i.InstituteStudents)
                .HasForeignKey(is_ => is_.InstituteId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<InstituteStudent>()
                .HasOne(is_ => is_.Student)
                .WithMany(s => s.InstituteStudents)
                .HasForeignKey(is_ => is_.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<InstituteTutor>()
                .HasKey(it => new { it.InstituteId, it.TutorId });

            modelBuilder.Entity<InstituteTutor>()
                .HasOne(it => it.Institute)
                .WithMany(i => i.InstituteTutors)
                .HasForeignKey(it => it.InstituteId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<InstituteTutor>()
                .HasOne(it => it.Tutor)
                .WithMany(t => t.InstituteTutors)
                .HasForeignKey(it => it.TutorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<InstituteJoinRequest>()
                .HasOne(r => r.Institute)
                .WithMany()
                .HasForeignKey(r => r.InstituteId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<InstituteJoinRequest>()
                .HasOne(r => r.Student)
                .WithMany()
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<InstituteJoinRequest>()
                .HasOne(r => r.Tutor)
                .WithMany()
                .HasForeignKey(r => r.TutorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Class)
                .WithMany()
                .HasForeignKey(a => a.ClassId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Institute)
                .WithMany()
                .HasForeignKey(a => a.InstituteId)
                .OnDelete(DeleteBehavior.NoAction);

            // Apply SmsLog Configuration
            modelBuilder.ApplyConfiguration(new SmsLogConfiguration());

            // ClassPayment Configuration
            modelBuilder.Entity<ClassPayment>()
                .Property(p => p.AmountPaid)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<ClassPayment>()
                .HasOne(p => p.Student)
                .WithMany()
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ClassPayment>()
                .HasOne(p => p.Class)
                .WithMany()
                .HasForeignKey(p => p.ClassId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ClassPayment>()
                .HasOne(p => p.Institute)
                .WithMany()
                .HasForeignKey(p => p.InstituteId)
                .OnDelete(DeleteBehavior.NoAction);

            // Prevent duplicate payment for same student+class+month+year
            modelBuilder.Entity<ClassPayment>()
                .HasIndex(p => new { p.StudentId, p.ClassId, p.Month, p.Year })
                .IsUnique();

            // ApiUsageLog Configuration (Cascading delete if User is deleted)
            modelBuilder.Entity<ApiUsageLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull); // Or Cascade depending on requirements. Choosing SetNull to keep usage data even if user is deleted, but user_id is nullable.

            // ApiDailyUsageSummary Configuration
            modelBuilder.Entity<ApiDailyUsageSummary>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // APIMonthlyUsageSummary Configuration
            modelBuilder.Entity<APIMonthlyUsageSummary>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Bank + Branch Configuration
            modelBuilder.Entity<Branch>()
                .HasOne(br => br.Bank)
                .WithMany(b => b.Branches)
                .HasForeignKey(br => br.BankCode)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Branch>()
                .HasIndex(br => new { br.BankCode, br.BranchCode }); // Fast branch lookup

            // Notification Configuration
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Composite index: fast query of latest notifications per user
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.CreatedAt });
        }
    }
}