using Microsoft.EntityFrameworkCore;
using SchoolSchedule.Context;
using SchoolSchedule.Entites;
using SchoolScheduleApp.Core;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace SchoolScheduleApp.ViewModels
{
    public class LessonRow
    {
        public string Day { get; set; } = string.Empty;
        public int DayOfWeek { get; set; }
        public int LessonIndex { get; set; }
        public string TimeRange { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Teacher { get; set; } = string.Empty;
        public string Classroom { get; set; } = string.Empty;
    }

    public class LessonSlot
    {
        public int DisplayIndex { get; set; }
        public int RealLessonIndex { get; set; }
        public bool HasLesson { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Teacher { get; set; } = string.Empty;
        public string Classroom { get; set; } = string.Empty;
    }

    public class ScheduleViewModel : ViewModelBase
    {
        public RelayCommand AutoGenerateScheduleCommand { get; }

        public ObservableCollection<AcademicClass> Classes { get; set; } = new();
        public ObservableCollection<LessonSlot> DayGrid { get; set; } = new();
        public ObservableCollection<LessonRow> ScheduleTable { get; set; } = new();

        private int _selectedDay = 1;
        public int SelectedDay
        {
            get => _selectedDay;
            set
            {
                if (_selectedDay == value) return;

                _selectedDay = value;
                OnPropertyChanged();

                var idx = _selectedDay - 1;
                if (idx < 0) idx = 0;
                if (idx > 4) idx = 4;

                if (_selectedDayTabIndex != idx)
                {
                    _selectedDayTabIndex = idx;
                    OnPropertyChanged(nameof(SelectedDayTabIndex));
                }

                RefreshData();
            }
        }

        private int _selectedDayTabIndex;
        public int SelectedDayTabIndex
        {
            get => _selectedDayTabIndex;
            set
            {
                if (_selectedDayTabIndex == value) return;

                _selectedDayTabIndex = value;
                OnPropertyChanged();

                var day = _selectedDayTabIndex + 1;
                if (day < 1) day = 1;
                if (day > 5) day = 5;

                if (_selectedDay != day)
                {
                    _selectedDay = day;
                    OnPropertyChanged(nameof(SelectedDay));
                    RefreshData();
                }
            }
        }

        private int _selectedClassId;
        public int SelectedClassId
        {
            get => _selectedClassId;
            set
            {
                _selectedClassId = value;
                OnPropertyChanged();
                RefreshData();
            }
        }

        public ScheduleViewModel()
        {
            AutoGenerateScheduleCommand = new RelayCommand(_ => ExecuteAutoGenerate());
            LoadClasses();
        }

        public void RefreshData()
        {
            LoadSchedule();
            LoadDayGrid();
        }

        private void LoadClasses()
        {
            using var db = new SchoolDbContext();

            Classes = new ObservableCollection<AcademicClass>(
                db.AcademicClasses.OrderBy(c => c.Name).ToList()
            );
            OnPropertyChanged(nameof(Classes));

            if (Classes.Count > 0 && SelectedClassId <= 0)
            {
                SelectedClassId = Classes[0].Id;
            }
        }

        private void ExecuteAutoGenerate()
        {
            var confirm = MessageBox.Show(
                "Автоматически составить расписание?\nСтарое расписание будет удалено.",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var result = ScheduleGenerator.Generate(clearOldSchedule: true);
            RefreshData();

            int lessonsForSelectedClass;
            using (var db = new SchoolDbContext())
            {
                lessonsForSelectedClass = db.Lessons.Count(x => x.AcademicClassId == SelectedClassId);
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Создано уроков всего: {result.CreatedLessons}");
            sb.AppendLine($"Уроков для выбранного класса: {lessonsForSelectedClass}");

            if (lessonsForSelectedClass == 0)
            {
                sb.AppendLine();
                sb.AppendLine("Внимание: для выбранного класса нет нагрузки (Workloads) — поэтому расписание пустое.");
            }

            if (result.Problems.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Проблемы:");
                foreach (var problem in result.Problems)
                {
                    sb.AppendLine("• " + problem);
                }
            }
            else
            {
                sb.AppendLine();
                sb.AppendLine("Генерация завершена.");
            }

            MessageBox.Show(sb.ToString(), "Результат", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadSchedule()
        {
            ScheduleTable.Clear();

            if (SelectedClassId <= 0) return;

            using var db = new SchoolDbContext();

            var lessons = db.Lessons
                .Include(x => x.Subject)
                .Include(x => x.Teacher)
                .Include(x => x.Classroom)
                .Where(x => x.AcademicClassId == SelectedClassId && x.DayOfWeek == SelectedDay)
                .OrderBy(x => x.LessonIndex)
                .ToList();

            foreach (var lesson in lessons)
            {
                ScheduleTable.Add(new LessonRow
                {
                    Day = DayToText(lesson.DayOfWeek),
                    DayOfWeek = lesson.DayOfWeek,
                    LessonIndex = lesson.LessonIndex,
                    TimeRange = SchedulePresentationHelper.LessonIndexToTimeRange(lesson.LessonIndex),
                    Subject = lesson.Subject?.Name ?? string.Empty,
                    Teacher = lesson.Teacher?.FullName ?? string.Empty,
                    Classroom = lesson.Classroom?.Number ?? "-"
                });
            }
        }

        private void LoadDayGrid()
        {
            DayGrid.Clear();

            if (SelectedClassId <= 0) return;

            var selectedClass = Classes.FirstOrDefault(c => c.Id == SelectedClassId);
            int shift = selectedClass?.Shift ?? 1;

            int start = shift == 1 ? 1 : 7;
            int end = shift == 1 ? 6 : 12;

            using var db = new SchoolDbContext();

            var lessons = db.Lessons
                .Include(x => x.Subject)
                .Include(x => x.Teacher)
                .Include(x => x.Classroom)
                .Where(x => x.AcademicClassId == SelectedClassId && x.DayOfWeek == SelectedDay)
                .ToList();

            int displayIndex = 1;
            for (int idx = start; idx <= end; idx++)
            {
                var lesson = lessons.FirstOrDefault(x => x.LessonIndex == idx);

                DayGrid.Add(new LessonSlot
                {
                    DisplayIndex = displayIndex,
                    RealLessonIndex = idx,
                    HasLesson = lesson != null,
                    Subject = lesson?.Subject?.Name ?? "Нет урока",
                    Teacher = lesson?.Teacher?.FullName ?? string.Empty,
                    Classroom = lesson?.Classroom?.Number ?? "-"
                });

                displayIndex++;
            }

            OnPropertyChanged(nameof(DayGrid));
        }

        private static string DayToText(int day)
        {
            return day switch
            {
                1 => "Понедельник",
                2 => "Вторник",
                3 => "Среда",
                4 => "Четверг",
                5 => "Пятница",
                _ => string.Empty
            };
        }
    }
}
