using System;

namespace SchoolScheduleApp.Core
{
    public static class ScheduleStatusNotifier
    {
        public static event Action ScheduleChanged;

        public static void NotifyScheduleChanged()
        {
            ScheduleChanged?.Invoke();
        }
    }
}
