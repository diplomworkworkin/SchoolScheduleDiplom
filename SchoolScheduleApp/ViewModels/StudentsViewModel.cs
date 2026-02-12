using Microsoft.EntityFrameworkCore;
using SchoolSchedule.Context;
using SchoolSchedule.Entites;
using SchoolScheduleApp.Core;
using SchoolScheduleApp.Views.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SchoolScheduleApp.ViewModels
{
    public class StudentsViewModel : ViewModelBase
    {
        private ObservableCollection<AcademicClass> _classesList = new();
        public ObservableCollection<AcademicClass> ClassesList
        {
            get => _classesList;
            set { _classesList = value; OnPropertyChanged(); }
        }

        private AcademicClass? _selectedClass;
        public AcademicClass? SelectedClass
        {
            get => _selectedClass;
            set { _selectedClass = value; OnPropertyChanged(); }
        }

        public RelayCommand AddClassCommand { get; }
        public RelayCommand EditClassCommand { get; }
        public RelayCommand DeleteClassCommand { get; }
        public RelayCommand RefreshClassesCommand { get; }

        public StudentsViewModel()
        {
            AddClassCommand = new RelayCommand(_ => ExecuteAddClass());
            EditClassCommand = new RelayCommand(ExecuteEditClass, CanEditOrDelete);
            DeleteClassCommand = new RelayCommand(ExecuteDeleteClass, CanEditOrDelete);
            RefreshClassesCommand = new RelayCommand(_ => LoadData());

            LoadData();
        }

        private bool CanEditOrDelete(object? obj)
        {
            var ac = obj as AcademicClass ?? SelectedClass;
            return ac != null;
        }

        private void LoadData()
        {
            using var db = new SchoolDbContext();

            var classes = db.AcademicClasses
                .Include(c => c.CuratorTeacher)
                .OrderBy(c => c.Name)
                .ToList();

            ClassesList = new ObservableCollection<AcademicClass>(classes);
        }

        private void ExecuteAddClass()
        {
            var wnd = new ClassEditWindow(null)
            {
                Owner = Application.Current?.Windows.OfType<Window>()
                    .FirstOrDefault(w => w.IsActive && w is not ClassEditWindow)
                    ?? Application.Current?.MainWindow
            };

            if (wnd.ShowDialog() != true)
                return;

            var newClass = wnd.AcademicClass;

            using var db = new SchoolDbContext();

            // 1) Уникальность имени класса
            if (db.AcademicClasses.Any(c => c.Name == newClass.Name))
            {
                ToastService.Show("Класс с таким названием уже существует.", "Ошибка", true);
                return;
            }

            // 2) Проверка куратора (не может быть куратором двух классов)
            if (newClass.CuratorTeacherId != null)
            {
                bool busy = db.AcademicClasses.Any(c => c.CuratorTeacherId == newClass.CuratorTeacherId);
                if (busy)
                {
                    ToastService.Show("Этот учитель уже назначен куратором другого класса.", "Ошибка", true);
                    return;
                }
            }

            db.AcademicClasses.Add(newClass);
            db.SaveChanges();

            // обновляем красиво и сразу с куратором
            LoadData();
            SelectedClass = ClassesList.FirstOrDefault(x => x.Id == newClass.Id);
        }

        private void ExecuteEditClass(object? obj)
        {
            var ac = obj as AcademicClass ?? SelectedClass;
            if (ac == null) return;

            // делаем копию, чтобы "Отмена" не меняла таблицу
            var editable = new AcademicClass
            {
                Id = ac.Id,
                Name = ac.Name,
                StudentCount = ac.StudentCount,
                Shift = ac.Shift,
                CuratorTeacherId = ac.CuratorTeacherId
            };

            var wnd = new ClassEditWindow(editable)
            {
                Owner = Application.Current?.Windows.OfType<Window>()
                    .FirstOrDefault(w => w.IsActive && w is not ClassEditWindow)
                    ?? Application.Current?.MainWindow
            };

            if (wnd.ShowDialog() != true)
                return;

            var updated = wnd.AcademicClass;

            using var db = new SchoolDbContext();
            var fromDb = db.AcademicClasses.FirstOrDefault(x => x.Id == updated.Id);
            if (fromDb == null) return;

            // 1) Уникальность имени (кроме самого себя)
            bool nameExists = db.AcademicClasses.Any(c => c.Name == updated.Name && c.Id != updated.Id);
            if (nameExists)
            {
                ToastService.Show("Класс с таким названием уже существует.", "Ошибка", true);
                return;
            }

            // 2) Проверка куратора (кроме самого себя)
            if (updated.CuratorTeacherId != null)
            {
                bool busy = db.AcademicClasses.Any(c =>
                    c.CuratorTeacherId == updated.CuratorTeacherId &&
                    c.Id != updated.Id);

                if (busy)
                {
                    ToastService.Show("Этот учитель уже назначен куратором другого класса.", "Ошибка", true);
                    return;
                }
            }

            fromDb.Name = updated.Name;
            fromDb.StudentCount = updated.StudentCount;
            fromDb.Shift = updated.Shift;
            fromDb.CuratorTeacherId = updated.CuratorTeacherId;

            db.SaveChanges();

            LoadData();
            SelectedClass = ClassesList.FirstOrDefault(x => x.Id == updated.Id);
        }

        private void ExecuteDeleteClass(object? obj)
        {
            var ac = obj as AcademicClass ?? SelectedClass;
            if (ac == null) return;

            using var db = new SchoolDbContext();

            // Важное ограничение: нельзя удалить класс, если на него есть нагрузки
            bool hasWorkloads = db.Workloads.Any(w => w.AcademicClassId == ac.Id);
            if (hasWorkloads)
            {
                ToastService.Show("Нельзя удалить класс: для него уже задана нагрузка (Workload). Сначала удалите/измените нагрузку.", "Ошибка", true);
                return;
            }

            var result = MessageBox.Show(
                $"Удалить класс \"{ac.Name}\"?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            var fromDb = db.AcademicClasses.FirstOrDefault(x => x.Id == ac.Id);
            if (fromDb == null) return;

            db.AcademicClasses.Remove(fromDb);
            db.SaveChanges();

            LoadData();
            SelectedClass = null;
        }
    }
}
