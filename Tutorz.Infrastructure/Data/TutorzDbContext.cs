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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new StudentConfiguration());

        }
    }
}
