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
    public partial class TeacherWindow : Window
    {
        public TeacherWindow()
        {
            InitializeComponent();

            MainFrame.Navigated += MainFrame_Navigated;
            NavigateTo(new TeacherSchedulePage(), "Моё расписание");

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

        private void NavigateTo(Page page, string title)
        {
            MainFrame.Navigate(page);
            PageTitle.Text = title;
        }

        private void BtnSchedule_Click(object sender, RoutedEventArgs e)
            => NavigateTo(new TeacherSchedulePage(), "Моё расписание");

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
            => NavigateTo(new SettingsPage(), "Настройки");

        private void BtnClose_Click(object sender, RoutedEventArgs e)
            => Application.Current.Shutdown();

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            UserSession.Clear();
            new MainWindow().Show();
            Close();
        }
    }
}
