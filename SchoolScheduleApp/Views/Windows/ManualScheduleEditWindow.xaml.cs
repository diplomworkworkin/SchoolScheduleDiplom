using SchoolSchedule.Context;
using SchoolSchedule.Entites;
using System.Collections.ObjectModel;
using System.Linq;
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
            LoadLessons();
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

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (Lessons.Any(x => x.SubjectId <= 0 || x.TeacherId <= 0 || x.LessonIndex <= 0))
            {
                MessageBox.Show("У каждого урока должен быть номер, предмет и учитель.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Lessons.GroupBy(x => x.LessonIndex).Any(g => g.Count() > 1))
            {
                MessageBox.Show("Номер урока должен быть уникальным внутри дня.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new SchoolDbContext();

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

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
