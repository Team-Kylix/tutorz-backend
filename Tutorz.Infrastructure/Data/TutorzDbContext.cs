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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Tutor>()
                .HasOne(t => t.User)
                .WithMany() 
                .HasForeignKey(t => t.UserId)
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
        }
    }
}
