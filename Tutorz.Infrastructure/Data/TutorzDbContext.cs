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
        }
    }
}