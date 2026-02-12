using Microsoft.EntityFrameworkCore;
using SchoolSchedule.Context;
using SchoolSchedule.Entites;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SchoolScheduleApp.Core
{
    public class ScheduleGenerateResult
    {
        public int CreatedLessons { get; set; }
        public List<string> Problems { get; set; } = new();
    }

    public static class ScheduleGenerator
    {
        private const int DaysPerWeek = 5;
        private const int LessonsPerShift = 6; // в смене 6 уроков

        public static event Action? ScheduleChanged;

        public static ScheduleGenerateResult Generate(bool clearOldSchedule = true)
        {
            using var db = new SchoolDbContext();
            return Generate(db, clearOldSchedule);
        }

        public static ScheduleGenerateResult Generate(SchoolDbContext db, bool clearOldSchedule = true)
        {
            var res = new ScheduleGenerateResult();

            var workloads = db.Workloads
                .Include(w => w.Subject)
                .Include(w => w.Teacher)
                    .ThenInclude(t => t.Classroom)
                .Include(w => w.AcademicClass)
                .ToList();

            if (workloads.Count == 0)
            {
                res.Problems.Add("Нет нагрузки (Workloads). Сначала заполните учебную нагрузку.");
                return res;
            }

            if (clearOldSchedule)
            {
                db.Lessons.RemoveRange(db.Lessons);
                db.SaveChanges();
            }

            var rooms = db.Classrooms.ToList();

            // занятость
            var busyClass = new HashSet<string>();
            var busyTeacher = new HashSet<string>();
            var busyRoom = new HashSet<string>();

            var lessonsToCreate = new List<Lesson>();

            // сначала большие нагрузки
            var ordered = workloads.OrderByDescending(w => w.HoursPerWeek).ToList();

            foreach (var w in ordered)
            {
                if (w.AcademicClass == null || w.Subject == null || w.Teacher == null)
                {
                    res.Problems.Add($"Workload #{w.Id}: не хватает связей (Class/Subject/Teacher).");
                    continue;
                }

                if (w.HoursPerWeek <= 0 || w.HoursPerWeek > DaysPerWeek * LessonsPerShift)
                {
                    res.Problems.Add($"Workload #{w.Id}: некорректное HoursPerWeek = {w.HoursPerWeek}.");
                    continue;
                }

                // Проверка: предмет должен соответствовать учителю
                // (в модели Teacher выбран один основной предмет)
                if (w.Teacher.SubjectId != null && w.Teacher.SubjectId.Value != w.SubjectId)
                {
                    res.Problems.Add(
                        $"Нагрузка #{w.Id}: предмет \"{w.Subject.Name}\" не соответствует предмету учителя \"{w.Teacher.FullName}\".");
                    continue;
                }

                int shift = w.AcademicClass.Shift;
                if (shift != 1 && shift != 2)
                {
                    res.Problems.Add($"Класс {w.AcademicClass.Name}: некорректная смена (Shift={shift}).");
                    continue;
                }

                int startIndex = shift == 1 ? 1 : 7;
                int endIndex = shift == 1 ? 6 : 12;

                // простое правило: не ставим один предмет два раза в один день этому классу
                var classSubjectDayUsed = new HashSet<string>();

                for (int i = 0; i < w.HoursPerWeek; i++)
                {
                    bool ok = TryPlace(w, startIndex, endIndex, rooms,
                        busyClass, busyTeacher, busyRoom,
                        classSubjectDayUsed,
                        out Lesson lesson);

                    if (!ok)
                    {
                        res.Problems.Add($"Не удалось поставить: класс {w.AcademicClass.Name}, предмет {w.Subject.Name} (час {i + 1}/{w.HoursPerWeek}).");
                        continue;
                    }

                    lessonsToCreate.Add(lesson);
                }
            }

            if (lessonsToCreate.Count > 0)
            {
                // Доп. проверка конфликтов перед сохранением
                var teacherConflicts = lessonsToCreate
                    .GroupBy(l => new { l.TeacherId, l.DayOfWeek, l.LessonIndex })
                    .Where(g => g.Count() > 1)
                    .ToList();

                var roomConflicts = lessonsToCreate
                    .Where(l => l.ClassroomId != null)
                    .GroupBy(l => new { l.ClassroomId, l.DayOfWeek, l.LessonIndex })
                    .Where(g => g.Count() > 1)
                    .ToList();

                // если есть хоть один конфликт — не сохраняем
                if (teacherConflicts.Count > 0 || roomConflicts.Count > 0)
                {
                    foreach (var c in teacherConflicts)
                        res.Problems.Add($"Конфликт: учитель #{c.Key.TeacherId} ведёт 2 урока одновременно (день {c.Key.DayOfWeek}, урок {c.Key.LessonIndex}).");

                    foreach (var c in roomConflicts)
                        res.Problems.Add($"Конфликт: кабинет #{c.Key.ClassroomId} занят 2 уроками одновременно (день {c.Key.DayOfWeek}, урок {c.Key.LessonIndex}).");

                    res.CreatedLessons = 0;
                    return res;
                }

                db.Lessons.AddRange(lessonsToCreate);
                db.SaveChanges();
            }

            res.CreatedLessons = lessonsToCreate.Count;
            NotifyScheduleChanged();
            return res;
        }

        private static void NotifyScheduleChanged()
        {
            ScheduleChanged?.Invoke();
        }

        private static bool TryPlace(
            Workload w,
            int startIndex,
            int endIndex,
            List<Classroom> rooms,
            HashSet<string> busyClass,
            HashSet<string> busyTeacher,
            HashSet<string> busyRoom,
            HashSet<string> classSubjectDayUsed,
            out Lesson lesson)
        {
            lesson = null;

            for (int day = 1; day <= DaysPerWeek; day++)
            {
                for (int idx = startIndex; idx <= endIndex; idx++)
                {
                    string classKey = $"{w.AcademicClassId}-{day}-{idx}";
                    string teacherKey = $"{w.TeacherId}-{day}-{idx}";

                    if (busyClass.Contains(classKey)) continue;
                    if (busyTeacher.Contains(teacherKey)) continue;

                    string subDayKey = $"{w.AcademicClassId}-{w.SubjectId}-{day}";
                    if (classSubjectDayUsed.Contains(subDayKey)) continue;

                    int? roomId = PickRoom(w, rooms, busyRoom, day, idx);

                    busyClass.Add(classKey);
                    busyTeacher.Add(teacherKey);
                    classSubjectDayUsed.Add(subDayKey);

                    if (roomId != null)
                        busyRoom.Add($"{roomId}-{day}-{idx}");

                    lesson = new Lesson
                    {
                        DayOfWeek = day,
                        LessonIndex = idx,
                        AcademicClassId = w.AcademicClassId,
                        TeacherId = w.TeacherId,
                        SubjectId = w.SubjectId,
                        ClassroomId = roomId
                    };

                    return true;
                }
            }

            return false;
        }

        private static int? PickRoom(Workload w, List<Classroom> rooms, HashSet<string> busyRoom, int day, int idx)
        {
            int students = w.AcademicClass?.StudentCount ?? 0;

            if (w.Teacher?.ClassroomId != null)
            {
                var ownRoom = rooms.FirstOrDefault(r => r.Id == w.Teacher.ClassroomId.Value);
                if (ownRoom != null)
                {
                    var ownRoomKey = $"{ownRoom.Id}-{day}-{idx}";
                    var roomFits = ownRoom.Capacity <= 0 || students <= 0 || ownRoom.Capacity >= students;
                    if (roomFits && !busyRoom.Contains(ownRoomKey))
                    {
                        return ownRoom.Id;
                    }
                }
            }

            foreach (var r in rooms)
            {
                // если есть Capacity — проверим
                if (r.Capacity > 0 && students > 0 && r.Capacity < students)
                    continue;

                string roomKey = $"{r.Id}-{day}-{idx}";
                if (busyRoom.Contains(roomKey))
                    continue;

                return r.Id;
            }

            return null;
        }
    }
}
