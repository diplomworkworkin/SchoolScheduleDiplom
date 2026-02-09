using Microsoft.EntityFrameworkCore;
using SchoolSchedule.Context;
using SchoolSchedule.Entites;
using SchoolScheduleApp.Core;
using System.Linq;
using Xunit;

namespace SchoolSchedule.Tests
{
    public class ScheduleGeneratorTests
    {
        private static SchoolDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<SchoolDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new SchoolDbContext(options);
        }

        private static void SeedBaseData(SchoolDbContext db)
        {
            db.Subjects.AddRange(
                new Subject { Id = 1, Name = "Математика" },
                new Subject { Id = 2, Name = "Русский язык" }
            );

            db.Teachers.AddRange(
                new Teacher { Id = 1, FullName = "Иванов Иван Иванович", SubjectId = 1 },
                new Teacher { Id = 2, FullName = "Петров Петр Петрович", SubjectId = 2 }
            );

            db.AcademicClasses.AddRange(
                new AcademicClass { Id = 1, Name = "7-А", StudentCount = 24, Shift = 1 },
                new AcademicClass { Id = 2, Name = "7-Б", StudentCount = 26, Shift = 1 }
            );

            db.Classrooms.AddRange(
                new Classroom { Id = 1, Number = "101", Capacity = 30, Type = "Обычный" },
                new Classroom { Id = 2, Number = "102", Capacity = 30, Type = "Обычный" }
            );

            db.SaveChanges();
        }

        [Fact]
        public void Generate_CreatesLessons_WithoutConflicts()
        {
            using var db = CreateContext(nameof(Generate_CreatesLessons_WithoutConflicts));
            SeedBaseData(db);

            db.Workloads.AddRange(
                new Workload { Id = 1, AcademicClassId = 1, TeacherId = 1, SubjectId = 1, HoursPerWeek = 3 },
                new Workload { Id = 2, AcademicClassId = 2, TeacherId = 2, SubjectId = 2, HoursPerWeek = 3 }
            );
            db.SaveChanges();

            var result = ScheduleGenerator.Generate(db, clearOldSchedule: true);

            Assert.Empty(result.Problems);
            Assert.Equal(6, result.CreatedLessons);

            var teacherConflicts = db.Lessons
                .GroupBy(x => new { x.TeacherId, x.DayOfWeek, x.LessonIndex })
                .Any(g => g.Count() > 1);
            Assert.False(teacherConflicts);

            var roomConflicts = db.Lessons
                .Where(x => x.ClassroomId != null)
                .GroupBy(x => new { x.ClassroomId, x.DayOfWeek, x.LessonIndex })
                .Any(g => g.Count() > 1);
            Assert.False(roomConflicts);
        }

        [Fact]
        public void Generate_Skips_MismatchedSubject()
        {
            using var db = CreateContext(nameof(Generate_Skips_MismatchedSubject));
            SeedBaseData(db);

            db.Workloads.Add(new Workload
            {
                Id = 1,
                AcademicClassId = 1,
                TeacherId = 1,
                SubjectId = 2,
                HoursPerWeek = 2
            });
            db.SaveChanges();

            var result = ScheduleGenerator.Generate(db, clearOldSchedule: true);

            Assert.NotEmpty(result.Problems);
            Assert.Equal(0, result.CreatedLessons);
        }

        [Fact]
        public void TeacherScheduleQuery_Filters_ByTeacherAndClass()
        {
            using var db = CreateContext(nameof(TeacherScheduleQuery_Filters_ByTeacherAndClass));
            SeedBaseData(db);

            db.Lessons.AddRange(
                new Lesson { Id = 1, TeacherId = 1, SubjectId = 1, AcademicClassId = 1, DayOfWeek = 1, LessonIndex = 1 },
                new Lesson { Id = 2, TeacherId = 1, SubjectId = 1, AcademicClassId = 2, DayOfWeek = 2, LessonIndex = 2 },
                new Lesson { Id = 3, TeacherId = 2, SubjectId = 2, AcademicClassId = 1, DayOfWeek = 3, LessonIndex = 3 }
            );
            db.SaveChanges();

            var query = ScheduleQueries.BuildTeacherScheduleQuery(db, teacherId: 1, dayOfWeek: null, academicClassId: 1, subjectId: null);
            var rows = query.ToList();

            Assert.Single(rows);
            Assert.Equal(1, rows[0].AcademicClassId);
            Assert.Equal(1, rows[0].TeacherId);
        }
    }
}
