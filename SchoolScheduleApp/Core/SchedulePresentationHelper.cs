using System.Collections.Generic;

namespace SchoolScheduleApp.Core
{
    public static class SchedulePresentationHelper
    {
        private static readonly Dictionary<int, string> LessonTimes = new()
        {
            { 1, "08:00–08:40" },
            { 2, "08:50–09:30" },
            { 3, "09:40–10:20" },
            { 4, "10:30–11:10" },
            { 5, "11:20–12:00" },
            { 6, "12:10–12:50" },
            { 7, "13:00–13:40" },
            { 8, "13:50–14:30" },
            { 9, "14:40–15:20" },
            { 10, "15:30–16:10" },
            { 11, "16:20–17:00" },
            { 12, "17:10–17:50" }
        };

        public static string DayToText(int day)
        {
            return day switch
            {
                1 => "Понедельник",
                2 => "Вторник",
                3 => "Среда",
                4 => "Четверг",
                5 => "Пятница",
                _ => "—"
            };
        }

        public static string LessonIndexToTimeRange(int lessonIndex)
        {
            return LessonTimes.TryGetValue(lessonIndex, out var time) ? time : "—";
        }
    }
}
