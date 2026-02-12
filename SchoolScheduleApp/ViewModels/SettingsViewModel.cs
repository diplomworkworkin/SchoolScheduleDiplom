using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using SchoolSchedule.Context;
using SchoolSchedule.Entites;
using SchoolScheduleApp.Core;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace SchoolScheduleApp.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly AppSettings _settings;

        public SettingsViewModel()
        {
            _settings = AppSettingsService.Load();

            ThemeManager.SetTheme(_settings.IsDarkTheme);

            _isDarkTheme = _settings.IsDarkTheme;
            _schoolName = _settings.SchoolName;
            _lessonDuration = _settings.LessonDuration;
            _startTime = _settings.StartTime;

            SaveCommand = new RelayCommand(_ => ExecuteSave());
            BackupCommand = new RelayCommand(_ => ExecuteBackup());
            ExportExcelCommand = new RelayCommand(_ => ExecuteExport("excel"));
            ExportWordCommand = new RelayCommand(_ => ExecuteExport("word"));
        }

        public bool CanManageAcademicProcess => UserSession.CurrentUser?.Role == UserRole.Admin;
        public bool CanBackup => UserSession.CurrentUser?.Role == UserRole.Admin;
        public bool CanExport => UserSession.CurrentUser?.Role == UserRole.Admin || UserSession.CurrentUser?.Role == UserRole.Teacher;

        private bool _isDarkTheme;
        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (_isDarkTheme != value)
                {
                    _isDarkTheme = value;
                    OnPropertyChanged();
                    ThemeManager.SetTheme(_isDarkTheme);
                }
            }
        }

        private string _schoolName;
        public string SchoolName
        {
            get => _schoolName;
            set { _schoolName = value; OnPropertyChanged(); }
        }

        private int _lessonDuration;
        public int LessonDuration
        {
            get => _lessonDuration;
            set { _lessonDuration = value; OnPropertyChanged(); }
        }

        private string _startTime;
        public string StartTime
        {
            get => _startTime;
            set { _startTime = value; OnPropertyChanged(); }
        }

        public RelayCommand SaveCommand { get; }
        public RelayCommand BackupCommand { get; }
        public RelayCommand ExportExcelCommand { get; }
        public RelayCommand ExportWordCommand { get; }

        private void ExecuteSave()
        {
            if (CanManageAcademicProcess)
            {
                if (string.IsNullOrWhiteSpace(SchoolName))
                {
                    ToastService.Show("Название учреждения не может быть пустым.", "Ошибка", true);
                    return;
                }

                if (LessonDuration <= 0 || LessonDuration > _settings.MaxLessonDuration)
                {
                    ToastService.Show("Некорректная длительность урока.", "Ошибка", true);
                    return;
                }

                if (string.IsNullOrWhiteSpace(StartTime) || !TimeSpan.TryParse(StartTime, out _))
                {
                    ToastService.Show("Начало первого урока должно быть в формате ЧЧ:ММ (например 08:00).", "Ошибка", true);
                    return;
                }

                _settings.SchoolName = SchoolName.Trim();
                _settings.LessonDuration = LessonDuration;
                _settings.StartTime = StartTime.Trim();
            }

            _settings.IsDarkTheme = IsDarkTheme;
            AppSettingsService.Save(_settings);
            ToastService.Show("Настройки сохранены.", "Система");
        }

        private void ExecuteBackup()
        {
            if (!CanBackup)
            {
                return;
            }

            try
            {
                var backupFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
                Directory.CreateDirectory(backupFolder);

                var fileName = $"School11_Schedule_DB_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                var fullPath = Path.Combine(backupFolder, fileName);

                using var db = new SchoolDbContext();
                var sql = $"BACKUP DATABASE [School11_Schedule_DB] TO DISK = N'{fullPath}' WITH INIT";
                db.Database.ExecuteSqlRaw(sql);

                ToastService.Show($"Бэкап создан: {fullPath}", "Бэкап");
            }
            catch (Exception ex)
            {
                ToastService.Show("Не удалось создать бэкап. " + ex.Message, "Ошибка", true);
            }
        }

        private void ExecuteExport(string mode)
        {
            if (!CanExport)
            {
                return;
            }

            try
            {
                var exportData = LoadExportLessons();
                if (exportData.Length == 0)
                {
                    ToastService.Show("Нет данных для экспорта.", "Экспорт");
                    return;
                }

                if (mode == "excel")
                {
                    ExportToCsv(exportData);
                }
                else
                {
                    ExportToWordLike(exportData);
                }
            }
            catch (Exception ex)
            {
                ToastService.Show("Ошибка экспорта: " + ex.Message, "Ошибка", true);
            }
        }

        private (string Day, int LessonIndex, string ClassName, string SubjectName, string TeacherName, string RoomNumber)[] LoadExportLessons()
        {
            using var db = new SchoolDbContext();
            var query = db.Lessons
                .Include(x => x.AcademicClass)
                .Include(x => x.Subject)
                .Include(x => x.Teacher)
                .Include(x => x.Classroom)
                .AsQueryable();

            if (UserSession.CurrentUser?.Role == UserRole.Teacher && UserSession.CurrentUser.TeacherId.HasValue)
            {
                var teacherId = UserSession.CurrentUser.TeacherId.Value;
                query = query.Where(x => x.TeacherId == teacherId);
            }

            return query
                .OrderBy(x => x.DayOfWeek)
                .ThenBy(x => x.LessonIndex)
                .Select(x => (
                    Day: SchedulePresentationHelper.DayToText(x.DayOfWeek),
                    LessonIndex: x.LessonIndex,
                    ClassName: x.AcademicClass != null ? x.AcademicClass.Name : "—",
                    SubjectName: x.Subject != null ? x.Subject.Name : "—",
                    TeacherName: x.Teacher != null ? x.Teacher.FullName : "—",
                    RoomNumber: x.Classroom != null ? x.Classroom.Number : "—"))
                .ToArray();
        }

        private void ExportToCsv((string Day, int LessonIndex, string ClassName, string SubjectName, string TeacherName, string RoomNumber)[] data)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Экспорт расписания в Excel (CSV)",
                Filter = "CSV файл (*.csv)|*.csv",
                FileName = $"schedule_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("День;Урок;Класс;Предмет;Учитель;Кабинет");
            foreach (var row in data)
            {
                sb.AppendLine($"{EscapeCsv(row.Day)};{row.LessonIndex};{EscapeCsv(row.ClassName)};{EscapeCsv(row.SubjectName)};{EscapeCsv(row.TeacherName)};{EscapeCsv(row.RoomNumber)}");
            }

            File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
            ToastService.Show("Экспорт в Excel (CSV) завершён.", "Экспорт");
        }

        private void ExportToWordLike((string Day, int LessonIndex, string ClassName, string SubjectName, string TeacherName, string RoomNumber)[] data)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Экспорт расписания в Word",
                Filter = "Word Document (*.doc)|*.doc",
                FileName = $"schedule_{DateTime.Now:yyyyMMdd_HHmm}.doc"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Расписание");
            sb.AppendLine(new string('=', 70));

            foreach (var row in data)
            {
                sb.AppendLine($"{row.Day}, урок {row.LessonIndex}");
                sb.AppendLine($"Класс: {row.ClassName}");
                sb.AppendLine($"Предмет: {row.SubjectName}");
                sb.AppendLine($"Учитель: {row.TeacherName}");
                sb.AppendLine($"Кабинет: {row.RoomNumber}");
                sb.AppendLine(new string('-', 70));
            }

            File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
            ToastService.Show("Экспорт в Word завершён.", "Экспорт");
        }

        private static string EscapeCsv(string text)
        {
            var value = text ?? string.Empty;
            if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }
    }
}
