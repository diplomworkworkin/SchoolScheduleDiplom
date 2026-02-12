using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using SchoolSchedule.Context;
using SchoolSchedule.Entites;
using SchoolScheduleApp.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace SchoolScheduleApp.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly AppSettings _settings;
        private readonly UserRole _currentRole;

        public SettingsViewModel()
        {
            _settings = AppSettingsService.Load();
            _currentRole = UserSession.CurrentUser?.Role ?? UserRole.Student;

            ThemeManager.SetTheme(_settings.IsDarkTheme);

            _isDarkTheme = _settings.IsDarkTheme;
            _schoolName = _settings.SchoolName;
            _lessonDuration = _settings.LessonDuration;
            _startTime = _settings.StartTime;
            _selectedExportFormat = ExportFormats[0];

            SaveCommand = new RelayCommand(_ => ExecuteSave());
            BackupCommand = new RelayCommand(_ => ExecuteBackup(), _ => CanManageAcademicSettings);
            ExportCommand = new RelayCommand(_ => ExecuteExport(), _ => CanExportSchedule);
        }

        public bool CanManageAcademicSettings => _currentRole == UserRole.Admin;
        public bool CanExportSchedule => _currentRole == UserRole.Admin || _currentRole == UserRole.Teacher;

        private bool _isDarkTheme;
        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (_isDarkTheme == value)
                {
                    return;
                }

                _isDarkTheme = value;
                OnPropertyChanged();
                ThemeManager.SetTheme(_isDarkTheme);
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

        private string _selectedExportFormat;
        public string SelectedExportFormat
        {
            get => _selectedExportFormat;
            set { _selectedExportFormat = value; OnPropertyChanged(); }
        }

        public List<string> ExportFormats { get; } = ["Word (*.docx)", "Excel (*.xlsx)"];

        public RelayCommand SaveCommand { get; }
        public RelayCommand BackupCommand { get; }
        public RelayCommand ExportCommand { get; }

        private void ExecuteSave()
        {
            if (CanManageAcademicSettings && string.IsNullOrWhiteSpace(SchoolName))
            {
                ToastService.Show("Название учреждения не может быть пустым.", "Ошибка", true);
                return;
            }

            if (CanManageAcademicSettings && (LessonDuration <= 0 || LessonDuration > _settings.MaxLessonDuration))
            {
                ToastService.Show("Некорректная длительность урока.", "Ошибка", true);
                return;
            }

            if (CanManageAcademicSettings && (string.IsNullOrWhiteSpace(StartTime) || !TimeSpan.TryParse(StartTime, out _)))
            {
                ToastService.Show("Начало первого урока должно быть в формате ЧЧ:ММ (например 08:00).", "Ошибка", true);
                return;
            }

            _settings.IsDarkTheme = IsDarkTheme;

            if (CanManageAcademicSettings)
            {
                _settings.SchoolName = SchoolName.Trim();
                _settings.LessonDuration = LessonDuration;
                _settings.StartTime = StartTime.Trim();
            }

            AppSettingsService.Save(_settings);
            ToastService.Show("Настройки сохранены (settings.json).", "Система");
        }

        private void ExecuteBackup()
        {
            if (!CanManageAcademicSettings)
            {
                ToastService.Show("Резервное копирование доступно только администратору.", "Доступ", true);
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

        private void ExecuteExport()
        {
            if (!CanExportSchedule)
            {
                ToastService.Show("Экспорт доступен только учителю и администратору.", "Доступ", true);
                return;
            }

            using var db = new SchoolDbContext();
            var rows = BuildExportRows(db);
            if (rows.Count == 0)
            {
                ToastService.Show("Нет данных для экспорта.", "Экспорт", true);
                return;
            }

            var exportWord = SelectedExportFormat.Contains("docx", StringComparison.OrdinalIgnoreCase);
            var dialog = new SaveFileDialog
            {
                Filter = exportWord ? "Word (*.docx)|*.docx" : "Excel (*.xlsx)|*.xlsx",
                FileName = exportWord ? "schedule_export.docx" : "schedule_export.xlsx"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            if (exportWord)
            {
                ExportToWord(dialog.FileName, rows);
            }
            else
            {
                ExportToExcel(dialog.FileName, rows);
            }

            ToastService.Show($"Файл сохранен: {dialog.FileName}", "Экспорт");
        }

        private List<LessonExportRow> BuildExportRows(SchoolDbContext db)
        {
            var query = db.Lessons
                .Include(x => x.AcademicClass)
                .Include(x => x.Subject)
                .Include(x => x.Teacher)
                .Include(x => x.Classroom)
                .AsQueryable();

            if (_currentRole == UserRole.Teacher)
            {
                var teacherId = UserSession.CurrentUser?.TeacherId;
                if (!teacherId.HasValue)
                {
                    return [];
                }

                query = query.Where(x => x.TeacherId == teacherId.Value);
            }

            return query
                .OrderBy(x => x.DayOfWeek)
                .ThenBy(x => x.LessonIndex)
                .Select(x => new LessonExportRow
                {
                    Day = x.DayOfWeek,
                    LessonNumber = x.LessonIndex,
                    Time = SchedulePresentationHelper.LessonIndexToTimeRange(x.LessonIndex),
                    ClassName = x.AcademicClass.Name,
                    Subject = x.Subject.Name,
                    Teacher = x.Teacher.FullName,
                    Room = x.Classroom != null ? x.Classroom.Number : "-"
                })
                .ToList();
        }

        private static void ExportToWord(string filePath, IReadOnlyCollection<LessonExportRow> rows)
        {
            using var archive = ZipFile.Open(filePath, ZipArchiveMode.Create);

            AddArchiveEntry(archive, "[Content_Types].xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/></Types>");

            AddArchiveEntry(archive, "_rels/.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/></Relationships>");

            var rowsXml = string.Join(Environment.NewLine, rows.Select(r =>
                $"<w:p><w:r><w:t>{EscapeXml(DayToText(r.Day))}: {r.LessonNumber} ({r.Time}), {r.ClassName}, {r.Subject}, {r.Teacher}, каб. {r.Room}</w:t></w:r></w:p>"));

            var documentXml =
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">" +
                "<w:body>" +
                "<w:p><w:r><w:t>Экспорт расписания</w:t></w:r></w:p>" +
                rowsXml +
                "</w:body></w:document>";

            AddArchiveEntry(archive, "word/document.xml", documentXml);
        }

        private static void ExportToExcel(string filePath, IReadOnlyCollection<LessonExportRow> rows)
        {
            using var archive = ZipFile.Open(filePath, ZipArchiveMode.Create);

            AddArchiveEntry(archive, "[Content_Types].xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");

            AddArchiveEntry(archive, "_rels/.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");

            AddArchiveEntry(archive, "xl/workbook.xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Schedule\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");

            AddArchiveEntry(archive, "xl/_rels/workbook.xml.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");

            var rowsXml = new List<string>
            {
                RowXml("День", "Урок", "Время", "Класс", "Предмет", "Учитель", "Кабинет")
            };

            rowsXml.AddRange(rows.Select(x => RowXml(
                DayToText(x.Day),
                x.LessonNumber.ToString(),
                x.Time,
                x.ClassName,
                x.Subject,
                x.Teacher,
                x.Room)));

            var sheetXml =
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>" +
                string.Join(Environment.NewLine, rowsXml) +
                "</sheetData></worksheet>";

            AddArchiveEntry(archive, "xl/worksheets/sheet1.xml", sheetXml);
        }

        private static string RowXml(params string[] values)
        {
            var row = new StringBuilder("<row>");
            foreach (var value in values)
            {
                row.Append($"<c t=\"inlineStr\"><is><t>{EscapeXml(value)}</t></is></c>");
            }

            row.Append("</row>");
            return row.ToString();
        }

        private static string EscapeXml(string? value)
            => System.Security.SecurityElement.Escape(value) ?? string.Empty;

        private static void AddArchiveEntry(ZipArchive archive, string path, string content)
        {
            var entry = archive.CreateEntry(path);
            using var stream = entry.Open();
            using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            writer.Write(content);
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
                _ => "—"
            };
        }

        private sealed class LessonExportRow
        {
            public int Day { get; set; }
            public int LessonNumber { get; set; }
            public string Time { get; set; } = string.Empty;
            public string ClassName { get; set; } = string.Empty;
            public string Subject { get; set; } = string.Empty;
            public string Teacher { get; set; } = string.Empty;
            public string Room { get; set; } = string.Empty;
        }
    }
}
