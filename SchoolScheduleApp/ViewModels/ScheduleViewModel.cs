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
        public string Day { get; set; }
        public int DayOfWeek { get; set; }
        public int LessonIndex { get; set; }
        public int DisplayLessonIndex { get; set; }
        public string Subject { get; set; }
        public string Teacher { get; set; }
        public string Classroom { get; set; }
    }

    public class LessonSlot
    {
        public int DisplayIndex { get; set; }     
        public int RealLessonIndex { get; set; }  
        public bool HasLesson { get; set; }
        public string Subject { get; set; }
        public string Teacher { get; set; }
        public string Classroom { get; set; }
    }

    public class ScheduleViewModel : ViewModelBase
    {
        public RelayCommand AutoGenerateScheduleCommand { get; }
        public ObservableCollection<AcademicClass> Classes { get; set; } = new();
        public ObservableCollection<LessonSlot> DayGrid { get; set; } = new();
        private void LoadClasses()
        {
            using var db = new SchoolDbContext();

            var list = db.AcademicClasses
                .OrderBy(c => c.Name)
                .ToList();

            Classes = new ObservableCollection<AcademicClass>(list);
            OnPropertyChanged(nameof(Classes));

            if (Classes.Count > 0 && SelectedClassId <= 0)
                SelectedClassId = Classes[0].Id;
        }
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

                // синхронизируем вкладки: 1..5 -> 0..4
                var idx = _selectedDay - 1;
                if (idx < 0) idx = 0;
                if (idx > 4) idx = 4;
                if (_selectedDayTabIndex != idx)
                {
                    _selectedDayTabIndex = idx;
                    OnPropertyChanged(nameof(SelectedDayTabIndex));
                }

                LoadSchedule();
                LoadDayGrid();
            }
        }

        // 0..4 (вкладки) -> 1..5 (DayOfWeek)
        private int _selectedDayTabIndex;
        public int SelectedDayTabIndex
        {
            get => _selectedDayTabIndex;
            set
            {
                if (_selectedDayTabIndex == value) return;

                _selectedDayTabIndex = value;
                OnPropertyChanged();

                // вкладки могут дать -1 при инициализации
                var day = _selectedDayTabIndex + 1;
                if (day < 1) day = 1;
                if (day > 5) day = 5;

                if (_selectedDay != day)
                {
                    _selectedDay = day;
                    OnPropertyChanged(nameof(SelectedDay));
                    LoadSchedule();
                    LoadDayGrid();
                }
            }
        }

        private int _selectedClassId;
        public int SelectedClassId
        {
            get => _selectedClassId;
            set { _selectedClassId = value; OnPropertyChanged(); LoadSchedule(); LoadDayGrid(); }
        }

        public ScheduleViewModel()
        {
            AutoGenerateScheduleCommand = new RelayCommand(_ => ExecuteAutoGenerate());
            LoadClasses();

        }

        private void ExecuteAutoGenerate()
        {
            var confirm = MessageBox.Show(
                "Автоматически составить расписание?\nСтарое расписание будет удалено.",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            var res = ScheduleGenerator.Generate(clearOldSchedule: true);

            LoadSchedule();
            LoadDayGrid();

            int lessonsForSelectedClass;
            using (var db = new SchoolDbContext())
            {
                lessonsForSelectedClass = db.Lessons.Count(x => x.AcademicClassId == SelectedClassId);
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Создано уроков всего: {res.CreatedLessons}");
            sb.AppendLine($"Уроков для выбранного класса: {lessonsForSelectedClass}");

            if (lessonsForSelectedClass == 0)
            {
                sb.AppendLine();
                sb.AppendLine("Внимание: для выбранного класса нет нагрузки (Workloads) — поэтому расписание пустое.");
            }

            if (res.Problems.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Проблемы:");
                foreach (var p in res.Problems)
                    sb.AppendLine("• " + p);
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
            int shift = GetClassShift(db);

            var lessons = db.Lessons
                .Include(x => x.Subject)
                .Include(x => x.Teacher)
                .Include(x => x.Classroom)
                .Where(x => x.AcademicClassId == SelectedClassId && x.DayOfWeek == SelectedDay)
                .OrderBy(x => x.LessonIndex)
                .ToList();

            foreach (var l in lessons)
            {
                ScheduleTable.Add(new LessonRow
                {
                    Day = DayToText(l.DayOfWeek),
                    DayOfWeek = l.DayOfWeek,
                    LessonIndex = l.LessonIndex,
                    DisplayLessonIndex = NormalizeLessonIndex(l.LessonIndex, shift),
                    Subject = l.Subject?.Name ?? "",
                    Teacher = l.Teacher?.FullName ?? "",
                    Classroom = l.Classroom != null ? l.Classroom.Number : "-"
                });
            }
        }

        private void LoadDayGrid()
        {
            DayGrid.Clear();

            if (SelectedClassId <= 0) return;

            var cls = Classes.FirstOrDefault(c => c.Id == SelectedClassId);
            int shift = cls?.Shift ?? 1;
            if (shift != 1 && shift != 2)
                shift = 1;

            int start = shift == 1 ? 1 : 7;
            int end = shift == 1 ? 6 : 12;

            using var db = new SchoolDbContext();

            var lessons = db.Lessons
                .Include(x => x.Subject)
                .Include(x => x.Teacher)
                .Include(x => x.Classroom)
                .Where(x => x.AcademicClassId == SelectedClassId && x.DayOfWeek == SelectedDay)
                .ToList();

            int display = 1;
            for (int idx = start; idx <= end; idx++)
            {
                var l = lessons.FirstOrDefault(x => x.LessonIndex == idx);

                DayGrid.Add(new LessonSlot
                {
                    DisplayIndex = display,
                    RealLessonIndex = idx,
                    HasLesson = l != null,
                    Subject = l?.Subject?.Name ?? "Нет урока",
                    Teacher = l?.Teacher?.FullName ?? "",
                    Classroom = l?.Classroom != null ? l.Classroom.Number : "-"
                });

                display++;
            }

            OnPropertyChanged(nameof(DayGrid));
        }

        private string DayToText(int day)
        {
            return day switch
            {
                1 => "Понедельник",
                2 => "Вторник",
                3 => "Среда",
                4 => "Четверг",
                5 => "Пятница",
                _ => ""
            };
        }

        private int GetClassShift(SchoolDbContext db)
        {
            int shift = db.AcademicClasses
                .Where(c => c.Id == SelectedClassId)
                .Select(c => c.Shift)
                .FirstOrDefault();

            if (shift != 1 && shift != 2)
                shift = 1;

            return shift;
        }

        private int NormalizeLessonIndex(int lessonIndex, int shift)
        {
            if (shift == 2 && lessonIndex >= 7)
                return lessonIndex - 6;

            return lessonIndex;
        }
    }
}
