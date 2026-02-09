using Microsoft.EntityFrameworkCore;
using SchoolSchedule.Context;
using SchoolSchedule.Entites;
using System.Linq;

namespace SchoolScheduleApp.Core
{
    public static class ScheduleQueries
    {
        public static IQueryable<Lesson> BuildTeacherScheduleQuery(
            SchoolDbContext db,
            int teacherId,
            int? dayOfWeek,
            int? academicClassId,
            int? subjectId)
        {
            var query = db.Lessons
                .Include(x => x.Subject)
                .Include(x => x.Teacher)
                .Include(x => x.Classroom)
                .Include(x => x.AcademicClass)
                .Where(x => x.TeacherId == teacherId);

            if (dayOfWeek.HasValue && dayOfWeek.Value > 0)
                query = query.Where(x => x.DayOfWeek == dayOfWeek.Value);

            if (academicClassId.HasValue && academicClassId.Value > 0)
                query = query.Where(x => x.AcademicClassId == academicClassId.Value);

            if (subjectId.HasValue && subjectId.Value > 0)
                query = query.Where(x => x.SubjectId == subjectId.Value);

            return query.OrderBy(x => x.DayOfWeek).ThenBy(x => x.LessonIndex);
        }

        public static IQueryable<Lesson> BuildClassScheduleQuery(
            SchoolDbContext db,
            int academicClassId,
            int? dayOfWeek)
        {
            var query = db.Lessons
                .Include(x => x.Subject)
                .Include(x => x.Teacher)
                .Include(x => x.Classroom)
                .Include(x => x.AcademicClass)
                .Where(x => x.AcademicClassId == academicClassId);

            if (dayOfWeek.HasValue && dayOfWeek.Value > 0)
                query = query.Where(x => x.DayOfWeek == dayOfWeek.Value);

            return query.OrderBy(x => x.DayOfWeek).ThenBy(x => x.LessonIndex);
        }
    }
}
