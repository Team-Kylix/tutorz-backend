using System;
using System.Collections.Generic;
using System.Linq;
using Tutorz.Domain.Entities;

namespace Tutorz.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static void Initialize(TutorzDbContext context)
        {
            // Ensure Database Exists
            context.Database.EnsureCreated();

            // Check if data already exists
            if (context.Users.Any())
            {
                return;
            }

            var roleAdmin = new Role { RoleId = Guid.NewGuid(), Name = "Admin", Description = "System Administrator" };
            var roleTutor = new Role { RoleId = Guid.NewGuid(), Name = "Tutor", Description = "Registered Tutor" };
            var roleStudent = new Role { RoleId = Guid.NewGuid(), Name = "Student", Description = "Student or Parent" };
            var roleInstitute = new Role { RoleId = Guid.NewGuid(), Name = "Institute", Description = "Educational Institute" };

            context.Roles.AddRange(roleAdmin, roleTutor, roleStudent, roleInstitute);
            context.SaveChanges();

            // Use Fully Qualified Name
            string dummyHash = BCrypt.Net.BCrypt.HashPassword("Test@123");

            // Tutor 1: John Doe (Maths)
            var userTutor1 = new User
            {
                UserId = Guid.NewGuid(),
                Email = "tutor.math@tutorz.com",
                PhoneNumber = "0771111111",
                PasswordHash = dummyHash,
                RoleId = roleTutor.RoleId,
                IsActive = true
            };
            var tutor1 = new Tutor
            {
                TutorId = Guid.NewGuid(),
                UserId = userTutor1.UserId,
                RegistrationNumber = "TUT-001",
                FirstName = "John",
                LastName = "Doe",
                Bio = "Expert in Combined Mathematics with 10 years experience.",
                ExperienceYears = 10,
                BankName = "Commercial Bank",
                BankAccountNumber = "88990011",
                IsActive = true
            };

            // Tutor 2: Sarah Smith (Science)
            var userTutor2 = new User
            {
                UserId = Guid.NewGuid(),
                Email = "tutor.science@tutorz.com",
                PhoneNumber = "0772222222",
                PasswordHash = dummyHash,
                RoleId = roleTutor.RoleId,
                IsActive = true
            };
            var tutor2 = new Tutor
            {
                TutorId = Guid.NewGuid(),
                UserId = userTutor2.UserId,
                RegistrationNumber = "TUT-002",
                FirstName = "Sarah",
                LastName = "Smith",
                Bio = "Science for O/L students. Simple explanations.",
                ExperienceYears = 5,
                BankName = "HNB",
                BankAccountNumber = "55667788",
                IsActive = true
            };

            context.Users.AddRange(userTutor1, userTutor2);
            context.Tutors.AddRange(tutor1, tutor2);
            context.SaveChanges();

            var classes = new List<Class>
            {
                // Classes for Tutor 1 (Math)
                new Class
                {
                    ClassId = Guid.NewGuid(),
                    TutorId = tutor1.TutorId,
                    InstituteName = "Rotary Hall",
                    ClassType = "Class",
                    Subject = "Combined Maths",
                    Grade = "12",
                    ClassName = "2025 Theory",
                    DayOfWeek = "Saturday",
                    StartTime = "08:00 AM",
                    EndTime = "12:00 PM",
                    HallName = "Main Hall A",
                    Fee = 2500.00m,
                    IsActive = true
                },
                new Class
                {
                    ClassId = Guid.NewGuid(),
                    TutorId = tutor1.TutorId,
                    InstituteName = "Online",
                    ClassType = "Seminar",
                    Subject = "Pure Maths",
                    Grade = "13",
                    ClassName = "Final Revision",
                    DayOfWeek = "Sunday",
                    StartTime = "06:00 PM",
                    EndTime = "10:00 PM",
                    HallName = "Zoom",
                    Fee = 1500.00m,
                    IsActive = true
                },
                // Classes for Tutor 2 (Science)
                new Class
                {
                    ClassId = Guid.NewGuid(),
                    TutorId = tutor2.TutorId,
                    InstituteName = "Sakya",
                    ClassType = "Class",
                    Subject = "Science",
                    Grade = "10",
                    ClassName = "O/L Prep",
                    DayOfWeek = "Monday",
                    StartTime = "02:30 PM",
                    EndTime = "05:30 PM",
                    HallName = "Room 4",
                    Fee = 1200.00m,
                    IsActive = true
                },
                new Class
                {
                    ClassId = Guid.NewGuid(),
                    TutorId = tutor2.TutorId,
                    InstituteName = "Sakya",
                    ClassType = "Workshop",
                    Subject = "Biology",
                    Grade = "11",
                    ClassName = "Diagrams Workshop",
                    DayOfWeek = "Friday",
                    StartTime = "08:00 AM",
                    EndTime = "12:00 PM",
                    HallName = "Lab 1",
                    Fee = 1000.00m,
                    IsActive = true
                }
            };

            context.Classes.AddRange(classes);
            context.SaveChanges();

            // Student 1: Kamal (Grade 12)
            var userStudent1 = new User
            {
                UserId = Guid.NewGuid(),
                Email = "student1@gmail.com",
                PhoneNumber = "0711111111",
                PasswordHash = dummyHash,
                RoleId = roleStudent.RoleId,
                IsActive = true
            };
            var student1 = new Student
            {
                StudentId = Guid.NewGuid(),
                UserId = userStudent1.UserId,
                RegistrationNumber = "STU-1001",
                FirstName = "Kamal",
                LastName = "Perera",
                SchoolName = "Royal College",
                Grade = "12",
                ParentName = "Saman Perera",
                DateOfBirth = new DateTime(2006, 5, 20),
                IsPrimary = true
            };

            // Student 2: Nimal (Grade 10)
            var userStudent2 = new User
            {
                UserId = Guid.NewGuid(),
                Email = "student2@gmail.com",
                PhoneNumber = "0712222222",
                PasswordHash = dummyHash,
                RoleId = roleStudent.RoleId,
                IsActive = true
            };
            var student2 = new Student
            {
                StudentId = Guid.NewGuid(),
                UserId = userStudent2.UserId,
                RegistrationNumber = "STU-1002",
                FirstName = "Nimal",
                LastName = "Silva",
                SchoolName = "Ananda College",
                Grade = "10",
                ParentName = "Sunil Silva",
                DateOfBirth = new DateTime(2008, 8, 15),
                IsPrimary = true
            };

            context.Users.AddRange(userStudent1, userStudent2);
            context.Students.AddRange(student1, student2);
            context.SaveChanges();

            var enrollments = new List<Enrollment>
            {
                // Kamal (Student 1) joins Math Class (Approved)
                new Enrollment
                {
                    Id = Guid.NewGuid(),
                    StudentId = student1.StudentId,
                    ClassId = classes[0].ClassId, // Math Theory
                    Status = EnrollmentStatus.Approved,
                    RequestedAt = DateTime.UtcNow.AddDays(-5),
                    EnrolledAt = DateTime.UtcNow.AddDays(-4)
                },
                // Kamal (Student 1) requests Science Class (Pending)
                new Enrollment
                {
                    Id = Guid.NewGuid(),
                    StudentId = student1.StudentId,
                    ClassId = classes[2].ClassId, // Science
                    Status = EnrollmentStatus.Pending,
                    RequestedAt = DateTime.UtcNow.AddHours(-2)
                },
                 // Nimal (Student 2) joins Science Class (Approved)
                new Enrollment
                {
                    Id = Guid.NewGuid(),
                    StudentId = student2.StudentId,
                    ClassId = classes[2].ClassId, // Science
                    Status = EnrollmentStatus.Approved,
                    RequestedAt = DateTime.UtcNow.AddDays(-10),
                    EnrolledAt = DateTime.UtcNow.AddDays(-9)
                }
            };

            context.Enrollments.AddRange(enrollments);
            context.SaveChanges();
        }
    }
}