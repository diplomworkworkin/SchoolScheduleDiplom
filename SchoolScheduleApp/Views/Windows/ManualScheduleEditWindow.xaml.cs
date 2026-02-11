using SchoolSchedule.Context;
using SchoolSchedule.Entites;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace SchoolScheduleApp.Views.Windows
{
    public partial class ManualScheduleEditWindow : Window
    {
        public class EditableLessonRow
        {
            public int Id { get; set; }
            public int LessonIndex { get; set; }
            public int SubjectId { get; set; }
            public int TeacherId { get; set; }
            public int? ClassroomId { get; set; }
        }

        private readonly int _classId;
        private readonly int _dayOfWeek;

        public ObservableCollection<EditableLessonRow> Lessons { get; } = new();
        public ObservableCollection<Subject> Subjects { get; } = new();
        public ObservableCollection<Teacher> Teachers { get; } = new();
        public ObservableCollection<Classroom> Classrooms { get; } = new();

        public ManualScheduleEditWindow(int classId, int dayOfWeek)
        {
            InitializeComponent();
            _classId = classId;
            _dayOfWeek = dayOfWeek;
            DataContext = this;

            LoadDictionaries();
            BindComboColumns();
            LoadLessons();
        }

        private void BindComboColumns()
        {
            SubjectColumn.ItemsSource = Subjects;
            TeacherColumn.ItemsSource = Teachers;
            ClassroomColumn.ItemsSource = Classrooms;
        }

        private void LoadDictionaries()
        {
            using var db = new SchoolDbContext();

            Subjects.Clear();
            foreach (var subject in db.Subjects.OrderBy(x => x.Name).ToList())
            {
                Subjects.Add(subject);
            }

            Teachers.Clear();
            foreach (var teacher in db.Teachers.OrderBy(x => x.FullName).ToList())
            {
                Teachers.Add(teacher);
            }

            Classrooms.Clear();
            foreach (var classroom in db.Classrooms.OrderBy(x => x.Number).ToList())
            {
                Classrooms.Add(classroom);
            }
        }

        private void LoadLessons()
        {
            using var db = new SchoolDbContext();
            var lessons = db.Lessons
                .Where(x => x.AcademicClassId == _classId && x.DayOfWeek == _dayOfWeek)
                .OrderBy(x => x.LessonIndex)
                .ToList();

            Lessons.Clear();
            foreach (var lesson in lessons)
            {
                Lessons.Add(new EditableLessonRow
                {
                    Id = lesson.Id,
                    LessonIndex = lesson.LessonIndex,
                    SubjectId = lesson.SubjectId,
                    TeacherId = lesson.TeacherId,
                    ClassroomId = lesson.ClassroomId
                });
            }
        }

        private void BtnAddLesson_Click(object sender, RoutedEventArgs e)
        {
            var nextIndex = Lessons.Count == 0 ? 1 : Lessons.Max(x => x.LessonIndex) + 1;

            Lessons.Add(new EditableLessonRow
            {
                LessonIndex = nextIndex,
                SubjectId = Subjects.FirstOrDefault()?.Id ?? 0,
                TeacherId = Teachers.FirstOrDefault()?.Id ?? 0,
                ClassroomId = Classrooms.FirstOrDefault()?.Id
            });
        }

        private void BtnDeleteLesson_Click(object sender, RoutedEventArgs e)
        {
            if (LessonsGrid.SelectedItem is not EditableLessonRow row)
            {
                MessageBox.Show("Выберите урок для удаления.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Lessons.Remove(row);
        }

        private void BtnAddSubject_Click(object sender, RoutedEventArgs e)
        {
            var subjectName = (TbNewSubject.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(subjectName))
            {
                MessageBox.Show("Введите название предмета.", "Предмет", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            using var db = new SchoolDbContext();
            if (db.Subjects.Any(s => s.Name.ToLower() == subjectName.ToLower()))
            {
                MessageBox.Show("Такой предмет уже существует.", "Предмет", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var subject = new Subject { Name = subjectName };
            db.Subjects.Add(subject);
            db.SaveChanges();

            Subjects.Add(subject);
            BindComboColumns();
            TbNewSubject.Clear();
            MessageBox.Show("Предмет добавлен. Теперь его можно выбрать в таблице.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnAddClassroom_Click(object sender, RoutedEventArgs e)
        {
            var roomNumber = (TbNewRoomNumber.Text ?? string.Empty).Trim();
            var roomType = (TbNewRoomType.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(roomNumber))
            {
                MessageBox.Show("Введите кабинет/номер.", "Кабинет", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse(TbNewRoomCapacity.Text, out var capacity) || capacity <= 0)
            {
                MessageBox.Show("Вместимость должна быть положительным числом.", "Кабинет", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(roomType))
            {
                roomType = "Обычный";
            }

            using var db = new SchoolDbContext();
            if (db.Classrooms.Any(c => c.Number.ToLower() == roomNumber.ToLower()))
            {
                MessageBox.Show("Такой кабинет уже существует.", "Кабинет", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var classroom = new Classroom
            {
                Number = roomNumber,
                Type = roomType,
                Capacity = capacity
            };

            db.Classrooms.Add(classroom);
            db.SaveChanges();

            Classrooms.Add(classroom);
            BindComboColumns();

            TbNewRoomNumber.Clear();
            TbNewRoomType.Clear();
            TbNewRoomCapacity.Clear();

            MessageBox.Show("Кабинет добавлен. Теперь его можно выбрать в таблице.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string? ValidateRows(SchoolDbContext db)
        {
            if (Lessons.Count == 0)
            {
                return "Добавьте хотя бы один урок.";
            }

            if (Lessons.Any(x => x.SubjectId <= 0 || x.TeacherId <= 0 || x.LessonIndex <= 0))
            {
                return "У каждого урока должен быть корректный номер, предмет и учитель.";
            }

            if (Lessons.GroupBy(x => x.LessonIndex).Any(g => g.Count() > 1))
            {
                return "Номер урока должен быть уникальным внутри дня.";
            }

            var shift = db.AcademicClasses.Where(x => x.Id == _classId).Select(x => x.Shift).FirstOrDefault();
            var minLessonIndex = shift == 2 ? 7 : 1;
            var maxLessonIndex = shift == 2 ? 12 : 6;

            if (Lessons.Any(x => x.LessonIndex < minLessonIndex || x.LessonIndex > maxLessonIndex))
            {
                return shift == 2
                    ? "Для 2-й смены можно ставить только уроки 7..12."
                    : "Для 1-й смены можно ставить только уроки 1..6.";
            }

            var subjectsById = db.Subjects.ToDictionary(x => x.Id, x => x);
            var teachersById = db.Teachers.ToDictionary(x => x.Id, x => x);
            var classroomsById = db.Classrooms.ToDictionary(x => x.Id, x => x);

            foreach (var row in Lessons)
            {
                if (!subjectsById.ContainsKey(row.SubjectId))
                {
                    return $"Урок {row.LessonIndex}: выбранный предмет не найден.";
                }

                if (!teachersById.TryGetValue(row.TeacherId, out var teacher))
                {
                    return $"Урок {row.LessonIndex}: выбранный учитель не найден.";
                }

                if (teacher.SubjectId.HasValue && teacher.SubjectId.Value != row.SubjectId)
                {
                    var teacherSubjectName = subjectsById.ContainsKey(teacher.SubjectId.Value)
                        ? subjectsById[teacher.SubjectId.Value].Name
                        : "(не задан)";
                    var lessonSubjectName = subjectsById[row.SubjectId].Name;

                    return $"Урок {row.LessonIndex}: учитель \"{teacher.FullName}\" ведёт \"{teacherSubjectName}\", нельзя назначить \"{lessonSubjectName}\".";
                }

                if (row.ClassroomId.HasValue && !classroomsById.ContainsKey(row.ClassroomId.Value))
                {
                    return $"Урок {row.LessonIndex}: выбранный кабинет не найден.";
                }
            }

            var editedIds = Lessons.Where(x => x.Id > 0).Select(x => x.Id).ToHashSet();
            var conflicts = new StringBuilder();

            foreach (var row in Lessons)
            {
                var teacherConflictExists = db.Lessons.Any(l =>
                    l.Id != row.Id &&
                    !editedIds.Contains(l.Id) &&
                    l.DayOfWeek == _dayOfWeek &&
                    l.LessonIndex == row.LessonIndex &&
                    l.TeacherId == row.TeacherId);

                if (teacherConflictExists)
                {
                    conflicts.AppendLine($"• Урок {row.LessonIndex}: у учителя уже есть занятие в это время.");
                }

                if (row.ClassroomId.HasValue)
                {
                    var roomConflictExists = db.Lessons.Any(l =>
                        l.Id != row.Id &&
                        !editedIds.Contains(l.Id) &&
                        l.DayOfWeek == _dayOfWeek &&
                        l.LessonIndex == row.LessonIndex &&
                        l.ClassroomId == row.ClassroomId.Value);

                    if (roomConflictExists)
                    {
                        conflicts.AppendLine($"• Урок {row.LessonIndex}: кабинет уже занят в это время.");
                    }
                }
            }

            if (conflicts.Length > 0)
            {
                return "Найдены конфликты:\n" + conflicts;
            }

            return null;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var db = new SchoolDbContext();

                var validationError = ValidateRows(db);
                if (validationError != null)
                {
                    MessageBox.Show(validationError, "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var existing = db.Lessons
                    .Where(x => x.AcademicClassId == _classId && x.DayOfWeek == _dayOfWeek)
                    .ToList();

                var incomingIds = Lessons.Where(x => x.Id > 0).Select(x => x.Id).ToHashSet();
                var toDelete = existing.Where(x => !incomingIds.Contains(x.Id)).ToList();
                if (toDelete.Count > 0)
                {
                    db.Lessons.RemoveRange(toDelete);
                }

                foreach (var row in Lessons)
                {
                    Lesson entity;
                    if (row.Id > 0)
                    {
                        entity = existing.First(x => x.Id == row.Id);
                    }
                    else
                    {
                        entity = new Lesson
                        {
                            AcademicClassId = _classId,
                            DayOfWeek = _dayOfWeek
                        };
                        db.Lessons.Add(entity);
                    }

                    entity.LessonIndex = row.LessonIndex;
                    entity.SubjectId = row.SubjectId;
                    entity.TeacherId = row.TeacherId;
                    entity.ClassroomId = row.ClassroomId;
                }

                db.SaveChanges();
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось сохранить изменения.\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
