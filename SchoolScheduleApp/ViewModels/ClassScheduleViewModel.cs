using SchoolSchedule.Context;
using SchoolSchedule.Entites;
using SchoolScheduleApp.Core;
using System.Collections.ObjectModel;
using System.Linq;

namespace SchoolScheduleApp.ViewModels
{
    public class ClassScheduleRow
    {
        public string Day { get; set; } = "";
        public string TimeRange { get; set; } = "";
        public int LessonIndex { get; set; }
        public string Subject { get; set; } = "";
        public string Teacher { get; set; } = "";
        public string Classroom { get; set; } = "";
    }

    public class ClassScheduleViewModel : ViewModelBase
    {
        public ObservableCollection<FilterOption> DayOptions { get; } = new();
        public ObservableCollection<FilterOption> ClassOptions { get; } = new();
        public ObservableCollection<ClassScheduleRow> ScheduleRows { get; } = new();

        private FilterOption? _selectedDay;
        public FilterOption? SelectedDay
        {
            get => _selectedDay;
            set { _selectedDay = value; OnPropertyChanged(); LoadSchedule(); }
        }

        private FilterOption? _selectedClass;
        public FilterOption? SelectedClass
        {
            get => _selectedClass;
            set { _selectedClass = value; OnPropertyChanged(); LoadSchedule(); }
        }

        private string _errorMessage = "";
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public ClassScheduleViewModel()
        {
            BuildDefaultFilters();
            LoadOptions();
            LoadSchedule();
        }

        private void BuildDefaultFilters()
        {
            DayOptions.Add(new FilterOption { Id = 0, Name = "Все дни" });
            DayOptions.Add(new FilterOption { Id = 1, Name = "Понедельник" });
            DayOptions.Add(new FilterOption { Id = 2, Name = "Вторник" });
            DayOptions.Add(new FilterOption { Id = 3, Name = "Среда" });
            DayOptions.Add(new FilterOption { Id = 4, Name = "Четверг" });
            DayOptions.Add(new FilterOption { Id = 5, Name = "Пятница" });

            SelectedDay = DayOptions.FirstOrDefault();
        }

        private void LoadOptions()
        {
            using var db = new SchoolDbContext();
            var classes = db.AcademicClasses
                .OrderBy(x => x.Name)
                .ToList();

            ClassOptions.Clear();
            ClassOptions.Add(new FilterOption { Id = 0, Name = "Все классы" });
            foreach (var cls in classes)
                ClassOptions.Add(new FilterOption { Id = cls.Id, Name = cls.Name });

            var user = UserSession.CurrentUser;
            if (user?.Role == UserRole.Student && user.AcademicClassId.HasValue)
            {
                SelectedClass = ClassOptions.FirstOrDefault(x => x.Id == user.AcademicClassId.Value)
                    ?? ClassOptions.FirstOrDefault();
            }
            else
            {
                SelectedClass = ClassOptions.FirstOrDefault();
            }
        }

        private void LoadSchedule()
        {
            ScheduleRows.Clear();

            if (SelectedClass == null || SelectedClass.Id == 0)
            {
                ErrorMessage = "Выберите класс.";
                return;
            }

            ErrorMessage = "";

            using var db = new SchoolDbContext();
            var lessons = ScheduleQueries.BuildClassScheduleQuery(
                    db,
                    SelectedClass.Id,
                    SelectedDay?.Id)
                .ToList();

            foreach (var l in lessons)
            {
                ScheduleRows.Add(new ClassScheduleRow
                {
                    Day = SchedulePresentationHelper.DayToText(l.DayOfWeek),
                    TimeRange = SchedulePresentationHelper.LessonIndexToTimeRange(l.LessonIndex),
                    LessonIndex = l.LessonIndex,
                    Subject = l.Subject?.Name ?? "",
                    Teacher = l.Teacher?.FullName ?? "",
                    Classroom = l.Classroom?.Number ?? "—"
                });
            }
        }
    }
}
