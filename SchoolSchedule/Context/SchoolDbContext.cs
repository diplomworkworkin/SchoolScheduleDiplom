using Microsoft.EntityFrameworkCore;
using SchoolSchedule.Entites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolSchedule.Context
{
    public class SchoolDbContext : DbContext
    {
        public SchoolDbContext()
        {
        }

        public SchoolDbContext(DbContextOptions<SchoolDbContext> options) : base(options)
        {
            // EnsureCreated не используем, так как проект работает через миграции (Database.Migrate)
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Classroom> Classrooms { get; set; }
        public DbSet<AcademicClass> AcademicClasses { get; set; }
        public DbSet<Workload> Workloads { get; set; }
        public DbSet<Lesson> Lessons { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=School11_Schedule_DB;Trusted_Connection=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<AcademicClass>()
                .HasOne(c => c.CuratorTeacher)
                .WithMany()
                .HasForeignKey(c => c.CuratorTeacherId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AcademicClass>()
                .HasIndex(c => c.CuratorTeacherId)
                .IsUnique();

            modelBuilder.Entity<Teacher>()
                .HasOne(t => t.Subject)
                .WithMany()
                .HasForeignKey(t => t.SubjectId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Teacher>()
                .HasOne(t => t.Classroom)
                .WithMany()
                .HasForeignKey(t => t.ClassroomId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Lesson>()
                .HasIndex(x => new { x.AcademicClassId, x.DayOfWeek, x.LessonIndex })
                .IsUnique();

            modelBuilder.Entity<Lesson>()
                .HasIndex(x => new { x.TeacherId, x.DayOfWeek, x.LessonIndex })
                .IsUnique();

            modelBuilder.Entity<Lesson>()
                .HasIndex(x => new { x.ClassroomId, x.DayOfWeek, x.LessonIndex })
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasOne(u => u.Teacher)
                .WithMany()
                .HasForeignKey(u => u.TeacherId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<User>()
                .HasOne(u => u.AcademicClass)
                .WithMany()
                .HasForeignKey(u => u.AcademicClassId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 1,
                Username = "admin",
                Password = "admin", 
                FullName = "Системный Администратор",
                Role = UserRole.Admin
            });

            modelBuilder.Entity<Subject>().HasData(
                new Subject { Id = 1, Name = "Математика" },
                new Subject { Id = 2, Name = "Русский язык" },
                new Subject { Id = 3, Name = "Информатика" },
                new Subject { Id = 4, Name = "Физкультура" }
            );

            modelBuilder.Entity<Classroom>().HasData(
                new Classroom { Id = 1, Number = "101", Capacity = 30, Type = "Обычный" },
                new Classroom { Id = 2, Number = "202", Capacity = 15, Type = "Компьютерный" },
                new Classroom { Id = 3, Number = "Спортзал", Capacity = 60, Type = "Спортзал" }
            );

            modelBuilder.Entity<Teacher>().HasData(
                new Teacher { Id = 1, FullName = "Петров Петр Петрович", SubjectId = 1, ClassroomId = 1 },
                new Teacher { Id = 2, FullName = "Сидорова Анна Ивановна", SubjectId = 2, ClassroomId = 2 }
            );
            modelBuilder.Entity<AcademicClass>().HasData(
                new AcademicClass { Id = 1, Name = "11-А", StudentCount = 25, Shift = 1, CuratorTeacherId = 1 },
                new AcademicClass { Id = 2, Name = "9-Б", StudentCount = 28, Shift = 2, CuratorTeacherId = 2 }
            );
            modelBuilder.Entity<Workload>().HasData(
                 new Workload { Id = 1, AcademicClassId = 1, TeacherId = 1, SubjectId = 1, HoursPerWeek = 5 },
                 new Workload { Id = 2, AcademicClassId = 1, TeacherId = 2, SubjectId = 2, HoursPerWeek = 3 },

                 new Workload { Id = 3, AcademicClassId = 2, TeacherId = 1, SubjectId = 1, HoursPerWeek = 4 },
                 new Workload { Id = 4, AcademicClassId = 2, TeacherId = 2, SubjectId = 2, HoursPerWeek = 3 }
            );

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 2,
                    Username = "teacher1",
                    Password = "teacher1",
                    FullName = "Петров Петр Петрович",
                    Role = UserRole.Teacher,
                    TeacherId = 1
                },
                new User
                {
                    Id = 3,
                    Username = "student1",
                    Password = "student1",
                    FullName = "Ученик 11-А",
                    Role = UserRole.Student,
                    AcademicClassId = 1
                }
            );

        }
    }
}
