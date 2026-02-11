using Microsoft.EntityFrameworkCore;
using SchoolSchedule.Context;
using SchoolSchedule.Entites;
using SchoolScheduleApp.Core;
using SchoolScheduleApp.Views;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SchoolScheduleApp.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private string _username;
        private string _errorMessage;

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); } // Сообщение об ошибке (красным)
        }

        // Команда для кнопки
        public RelayCommand LoginCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(ExecuteLogin);
        }

        private void ExecuteLogin(object parameter)
        {
            // Передача пароля из PasswordBox (MVVM хак для безопасности)
            var passwordBox = parameter as PasswordBox;
            var password = passwordBox?.Password;

            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(password))
            {
                ErrorMessage = "Введите логин и пароль!";
                return;
            }

            try
            {
                using var db = new SchoolDbContext();
                var user = db.Users
                    .Include(u => u.Teacher)
                    .Include(u => u.AcademicClass)
                    .FirstOrDefault(u => u.Username == Username && u.Password == password);

                if (user == null)
                {
                    ErrorMessage = "Неверный логин или пароль";
                    return;
                }

                ErrorMessage = "";
                UserSession.SetUser(user);
                AppLogger.LogInfo($"Вход в систему: {user.Username} ({user.Role})");

                Window? nextWindow = user.Role switch
                {
                    UserRole.Admin => new AdminWindow(),
                    UserRole.Teacher => new TeacherWindow(),
                    UserRole.Student => new StudentWindow(),
                    _ => null
                };

                if (nextWindow == null)
                {
                    ErrorMessage = "Роль пользователя не поддерживается.";
                    return;
                }

                Application.Current.MainWindow = nextWindow;
                ToastService.Show($"Добро пожаловать, {user.FullName}!", "Успех");
                nextWindow.Show();

                foreach (Window window in Application.Current.Windows)
                {
                    if (window.DataContext == this)
                        window.Close();
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Ошибка авторизации.", ex);
                ErrorMessage = "Произошла ошибка входа. Проверьте подключение к базе.";
            }
        }
    }
}
