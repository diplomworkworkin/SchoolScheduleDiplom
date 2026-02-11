using SchoolScheduleApp.Core;
using SchoolScheduleApp.Views.Pages;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Navigation;

namespace SchoolScheduleApp.Views
{
    public partial class AdminWindow : Window
    {
        public AdminWindow()
        {
            InitializeComponent();

            // Небольшая анимация при переходе между страницами (приятный "вау" эффект)
            MainFrame.Navigated += MainFrame_Navigated;

            NavigateTo(new DashboardPage(), "Обзор системы");

            // Перетаскивание окна (WindowStyle=None)
            MouseDown += (_, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                    DragMove();
            };
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.Content is not Page page) return;

            page.Opacity = 0;
            var anim = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(180),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            page.BeginAnimation(OpacityProperty, anim);
        }

        public void NavigateTo(Page page, string title)
        {
            MainFrame.Navigate(page);
            PageTitle.Text = title;
        }

        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
            => NavigateTo(new DashboardPage(), "Обзор системы");

        private void BtnSchedule_Click(object sender, RoutedEventArgs e)
            => NavigateTo(new SchedulePage(), "Управление расписанием");

        private void BtnTeachers_Click(object sender, RoutedEventArgs e)
            => NavigateTo(new TeachersPage(), "Справочник учителей");

        private void BtnStudents_Click(object sender, RoutedEventArgs e)
            => NavigateTo(new StudentsPage(), "База данных учащихся");

        private void BtnWorkloads_Click(object sender, RoutedEventArgs e)
            => NavigateTo(new WorkloadsPage(), "Учебная нагрузка");

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
            => NavigateTo(new SettingsPage(), "Настройки системы");

        private void BtnClose_Click(object sender, RoutedEventArgs e)
            => Application.Current.Shutdown();

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;


        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            UserSession.Clear();
            var loginWindow = new MainWindow();
            Application.Current.MainWindow = loginWindow;
            loginWindow.Show();
            Close();
        }
    }
}
