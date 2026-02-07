using SchoolSchedule.Context;
using SchoolScheduleApp.Core;
using System;
using System.Linq;
using System.Windows.Media;

namespace SchoolScheduleApp.ViewModels
{
    public class AdminViewModel : ViewModelBase
    {
        // === ПОЛЯ ДЛЯ ПРИВЯЗКИ (Binding) ===
        // Эти свойства отображаются на карточках

        private int _teachersCount;
        public int TeachersCount
        {
            get => _teachersCount;
            set { _teachersCount = value; OnPropertyChanged(); }
        }

        private int _studentsCount;
        public int StudentsCount
        {
            get => _studentsCount;
            set { _studentsCount = value; OnPropertyChanged(); }
        }

        private string _scheduleStatus;
        public string ScheduleStatus
        {
            get => _scheduleStatus;
            set { _scheduleStatus = value; OnPropertyChanged(); }
        }

        private Brush _scheduleStatusBrush = Brushes.Red;
        public Brush ScheduleStatusBrush
        {
            get => _scheduleStatusBrush;
            set { _scheduleStatusBrush = value; OnPropertyChanged(); }
        }

        // === ГРАФИК ЗАГРУЖЕННОСТИ АУДИТОРИЙ (в %) ===
        // Строим линию по дням недели: Пн..Пт
        private PointCollection _roomLoadLine = new();
        public PointCollection RoomLoadLine
        {
            get => _roomLoadLine;
            set { _roomLoadLine = value; OnPropertyChanged(); }
        }

        private PointCollection _roomLoadArea = new();
        public PointCollection RoomLoadArea
        {
            get => _roomLoadArea;
            set { _roomLoadArea = value; OnPropertyChanged(); }
        }

        private string _roomLoadPercentText = "0%";
        public string RoomLoadPercentText
        {
            get => _roomLoadPercentText;
            set { _roomLoadPercentText = value; OnPropertyChanged(); }
        }

        public RelayCommand RefreshRoomLoadCommand { get; }

        public AdminViewModel()
        {
            RefreshRoomLoadCommand = new RelayCommand(_ => LoadRoomLoadChart());

            // 2. Загрузка данных из БД
            LoadDashboardData();
            LoadRoomLoadChart();

            ScheduleStatusNotifier.ScheduleChanged += OnScheduleChanged;
        }

        private void OnScheduleChanged()
        {
            LoadDashboardData();
            LoadRoomLoadChart();
        }

        private void LoadDashboardData()
        {
            // Используем using, чтобы соединение с БД закрывалось сразу после запроса
            using (var db = new SchoolDbContext())
            {
                // Считаем количество учителей
                TeachersCount = db.Teachers.Count();

                // Считаем количество классов
                StudentsCount = db.AcademicClasses.Count();

                // Проверяем статус расписания
                // Если в таблице Lessons есть хоть одна запись - значит расписание составлено
                bool hasLessons = db.Lessons.Any();

                if (hasLessons)
                {
                    ScheduleStatus = "Готово";
                    ScheduleStatusBrush = Brushes.LimeGreen;
                }
                else
                {
                    ScheduleStatus = "Не готово";
                    ScheduleStatusBrush = Brushes.IndianRed;
                }
            }
        }

        private void LoadRoomLoadChart()
        {
            // График строим в координатах Canvas как в твоём макете.
            // X: 0..800 (5 точек), Y: 20..250
            const double xMax = 800;
            const double yTop = 20;
            const double yBottom = 250;

            using var db = new SchoolDbContext();

            int roomsCount = db.Classrooms.Count();
            if (roomsCount == 0)
            {
                RoomLoadPercentText = "0%";
                RoomLoadLine = new PointCollection();
                RoomLoadArea = new PointCollection();
                return;
            }

            // Максимум слотов на один кабинет в день.
            // У тебя LessonIndex используется в пределах 1..12 (две смены).
            const int maxSlotsPerDay = 12;

            // Считаем % загруженности по дням: (занятые слоты / (кабинеты * максимум)) * 100
            // Берём только уроки, где указан кабинет.
            var lessonsByDay = db.Lessons
                .Where(l => l.ClassroomId != null && l.DayOfWeek >= 1 && l.DayOfWeek <= 5)
                .GroupBy(l => l.DayOfWeek)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToList();

            double[] percents = new double[5];
            for (int day = 1; day <= 5; day++)
            {
                int busy = lessonsByDay.FirstOrDefault(x => x.Day == day)?.Count ?? 0;
                double total = roomsCount * maxSlotsPerDay;
                percents[day - 1] = total > 0 ? (busy / total) * 100.0 : 0;
            }

            // среднее значение для подписи
            double avg = percents.Average();
            RoomLoadPercentText = $"{Math.Round(avg, 1)}%";

            // строим точки
            var line = new PointCollection();
            var area = new PointCollection();

            double step = xMax / 4.0; // 5 точек => 4 промежутка

            // область начинается снизу слева
            area.Add(new System.Windows.Point(0, yBottom));

            for (int i = 0; i < 5; i++)
            {
                double x = step * i;
                double y = yBottom - (percents[i] / 100.0) * (yBottom - yTop);

                line.Add(new System.Windows.Point(x, y));
                area.Add(new System.Windows.Point(x, y));
            }

            // область закрываем вниз справа
            area.Add(new System.Windows.Point(xMax, yBottom));

            RoomLoadLine = line;
            RoomLoadArea = area;
        }
    }
}
