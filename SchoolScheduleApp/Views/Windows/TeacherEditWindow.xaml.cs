using SchoolSchedule.Context;
using SchoolSchedule.Entites;
using SchoolScheduleApp.Core;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace SchoolScheduleApp.Views.Windows
{
    public partial class TeacherEditWindow : Window
    {
        public ObservableCollection<Subject> Subjects { get; private set; } = new();
        public ObservableCollection<ClassroomOption> Classrooms { get; private set; } = new();
        public Teacher Teacher { get; }

        public TeacherEditWindow(Teacher teacher)
        {
            InitializeComponent();

            Teacher = teacher ?? new Teacher();
            LoadSubjects();
            LoadClassrooms();

            DataContext = this;
        }

        private void LoadSubjects()
        {
            using var db = new SchoolDbContext();
            Subjects = new ObservableCollection<Subject>(
                db.Subjects.OrderBy(s => s.Name).ToList()
            );
        }

        private void RefreshBinding()
        {
            DataContext = null;
            DataContext = this;
        }

        private void LoadClassrooms()
        {
            using var db = new SchoolDbContext();

            var rooms = db.Classrooms
                .OrderBy(c => c.Number)
                .Select(c => new ClassroomOption
                {
                    Id = c.Id,
                    DisplayName = string.IsNullOrWhiteSpace(c.Type)
                        ? c.Number
                        : $"{c.Number} ({c.Type})"
                })
                .ToList();

            Classrooms = new ObservableCollection<ClassroomOption>(rooms);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Teacher.FullName))
            {
                ToastService.Show("Введите ФИО преподавателя.", "Проверка", true);
                return;
            }

            if (Teacher.SubjectId == null)
            {
                ToastService.Show("Выберите предмет для учителя.", "Проверка", true);
                return;
            }

            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void BtnAddSubject_Click(object sender, RoutedEventArgs e)
        {
            var name = (TbNewSubject.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ToastService.Show("Введите название предмета.", "Проверка", true);
                return;
            }

            using var db = new SchoolDbContext();
            if (db.Subjects.Any(s => s.Name.ToLower() == name.ToLower()))
            {
                ToastService.Show("Такой предмет уже существует.", "Информация");
                return;
            }

            var subject = new Subject { Name = name };
            db.Subjects.Add(subject);
            db.SaveChanges();

            LoadSubjects();
            Teacher.SubjectId = subject.Id;
            TbNewSubject.Clear();
            RefreshBinding();

            ToastService.Show("Предмет успешно добавлен.");
        }

        private void BtnAddClassroom_Click(object sender, RoutedEventArgs e)
        {
            var number = (TbNewRoomNumber.Text ?? string.Empty).Trim();
            var type = (TbNewRoomType.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(number))
            {
                ToastService.Show("Введите номер/название кабинета.", "Проверка", true);
                return;
            }

            if (!int.TryParse(TbNewRoomCapacity.Text, out var capacity) || capacity <= 0)
            {
                ToastService.Show("Вместимость должна быть положительным числом.", "Проверка", true);
                return;
            }

            using var db = new SchoolDbContext();
            if (db.Classrooms.Any(c => c.Number.ToLower() == number.ToLower()))
            {
                ToastService.Show("Такой кабинет уже существует.", "Информация");
                return;
            }

            var classroom = new Classroom
            {
                Number = number,
                Type = string.IsNullOrWhiteSpace(type) ? null : type,
                Capacity = capacity
            };

            db.Classrooms.Add(classroom);
            db.SaveChanges();

            Teacher.ClassroomId = classroom.Id;
            LoadClassrooms();
            RefreshBinding();

            TbNewRoomNumber.Clear();
            TbNewRoomType.Clear();
            TbNewRoomCapacity.Clear();

            ToastService.Show("Кабинет добавлен.");
        }

        public sealed class ClassroomOption
        {
            public int Id { get; set; }
            public string DisplayName { get; set; } = string.Empty;
        }
    }
}
