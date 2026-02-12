using SchoolSchedule.Context;
using SchoolSchedule.Entites;
using SchoolScheduleApp.Core;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SchoolScheduleApp.ViewModels
{
    public class FilterOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class TeacherScheduleRow
    {
        public string Day { get; set; } = "";
        public string TimeRange { get; set; } = "";
        public int LessonIndex { get; set; }
        public string AcademicClass { get; set; } = "";
        public string Subject { get; set; } = "";
        public string Classroom { get; set; } = "";
        public string Type { get; set; } = "—";
    }

    public class TeacherScheduleViewModel : ViewModelBase
    {
        public ObservableCollection<FilterOption> DayOptions { get; } = new();
        public ObservableCollection<FilterOption> WeekOptions { get; } = new();
        public ObservableCollection<FilterOption> ClassOptions { get; } = new();
        public ObservableCollection<FilterOption> SubjectOptions { get; } = new();
        public ObservableCollection<TeacherScheduleRow> ScheduleRows { get; } = new();

        private FilterOption? _selectedDay;
        public FilterOption? SelectedDay
        {
            get => _selectedDay;
            set { _selectedDay = value; OnPropertyChanged(); LoadSchedule(); }
        }

        private FilterOption? _selectedWeek;
        public FilterOption? SelectedWeek
        {
            get => _selectedWeek;
            set { _selectedWeek = value; OnPropertyChanged(); }
        }

        private FilterOption? _selectedClass;
        public FilterOption? SelectedClass
        {
            get => _selectedClass;
            set { _selectedClass = value; OnPropertyChanged(); LoadSchedule(); }
        }

        private FilterOption? _selectedSubject;
        public FilterOption? SelectedSubject
        {
            get => _selectedSubject;
            set { _selectedSubject = value; OnPropertyChanged(); LoadSchedule(); }
        }

        private string _weekRangeText = "";
        public string WeekRangeText
        {
            get => _weekRangeText;
            set { _weekRangeText = value; OnPropertyChanged(); }
        }

        private string _errorMessage = "";
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public RelayCommand ResetFiltersCommand { get; }

        public TeacherScheduleViewModel()
        {
            ResetFiltersCommand = new RelayCommand(_ => ResetFilters());
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

            WeekOptions.Add(new FilterOption { Id = 0, Name = "Вся неделя" });
            WeekOptions.Add(new FilterOption { Id = 1, Name = "Текущая неделя" });

            SelectedDay = DayOptions.FirstOrDefault();
            SelectedWeek = WeekOptions.FirstOrDefault();
        }

        private void LoadOptions()
        {
            var user = UserSession.CurrentUser;
            if (user == null || user.Role != UserRole.Teacher || user.TeacherId == null)
            {
                ErrorMessage = "Нет привязки учителя к учетной записи.";
                return;
            }

            using var db = new SchoolDbContext();

            var classItems = db.Lessons
                .Where(x => x.TeacherId == user.TeacherId)
                .Select(x => x.AcademicClass)
                .Where(x => x != null)
                .Distinct()
                .OrderBy(x => x!.Name)
                .ToList();

            ClassOptions.Clear();
            ClassOptions.Add(new FilterOption { Id = 0, Name = "Все классы" });
            foreach (var cls in classItems)
                ClassOptions.Add(new FilterOption { Id = cls!.Id, Name = cls.Name });

            SubjectOptions.Clear();
            SubjectOptions.Add(new FilterOption { Id = 0, Name = "Все предметы" });
            var subjects = db.Lessons
                .Where(x => x.TeacherId == user.TeacherId)
                .Select(x => x.Subject)
                .Where(x => x != null)
                .Distinct()
                .OrderBy(x => x!.Name)
                .ToList();
            foreach (var subj in subjects)
                SubjectOptions.Add(new FilterOption { Id = subj!.Id, Name = subj.Name });

            SelectedClass = ClassOptions.FirstOrDefault();
            SelectedSubject = SubjectOptions.FirstOrDefault();

            WeekRangeText = GetCurrentWeekRange();
        }

        private void LoadSchedule()
        {
            WeekRangeText = GetCurrentWeekRange();
            ScheduleRows.Clear();

            var user = UserSession.CurrentUser;
            if (user == null || user.Role != UserRole.Teacher || user.TeacherId == null)
            {
                ErrorMessage = "Роль учителя не подтверждена.";
                return;
            }

            ErrorMessage = "";

            using var db = new SchoolDbContext();
            var lessons = ScheduleQueries.BuildTeacherScheduleQuery(
                    db,
                    user.TeacherId.Value,
                    SelectedDay?.Id,
                    SelectedClass?.Id,
                    SelectedSubject?.Id)
                .ToList();

            foreach (var l in lessons)
            {
                ScheduleRows.Add(new TeacherScheduleRow
                {
                    Day = SchedulePresentationHelper.DayToText(l.DayOfWeek),
                    TimeRange = SchedulePresentationHelper.LessonIndexToTimeRange(l.LessonIndex),
                    LessonIndex = l.LessonIndex,
                    AcademicClass = l.AcademicClass?.Name ?? "",
                    Subject = l.Subject?.Name ?? "",
                    Classroom = l.Classroom?.Number ?? "—",
                    Type = string.IsNullOrWhiteSpace(l.Classroom?.Type) ? "—" : l.Classroom!.Type!
                });
            }
        }

        private void ResetFilters()
        {
            SelectedDay = DayOptions.FirstOrDefault();
            SelectedWeek = WeekOptions.FirstOrDefault();
            SelectedClass = ClassOptions.FirstOrDefault();
            SelectedSubject = SubjectOptions.FirstOrDefault();
        }

        private static string GetCurrentWeekRange()
        {
            var today = DateTime.Today;
            var diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var monday = today.AddDays(-diff);
            var friday = monday.AddDays(4);
            return $"{monday:dd.MM.yyyy} – {friday:dd.MM.yyyy}";
        }
    }
}
