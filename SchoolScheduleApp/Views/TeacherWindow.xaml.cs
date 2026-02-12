using SchoolScheduleApp.Core;
using SchoolScheduleApp.Views.Pages;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace SchoolScheduleApp.Views
{
    public partial class TeacherWindow : Window
    {
        private readonly DispatcherTimer _messagesTimer;

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

            Activated += (_, _) => UpdateMessagesBadge();
            _messagesTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            _messagesTimer.Tick += (_, _) => UpdateMessagesBadge();
            _messagesTimer.Start();
            Closed += (_, _) => _messagesTimer.Stop();

            UpdateMessagesBadge();
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
            UpdateMessagesBadge();
        }

        private void BtnSchedule_Click(object sender, RoutedEventArgs e)
            => NavigateTo(new TeacherSchedulePage(), "Моё расписание");

        private void BtnMessages_Click(object sender, RoutedEventArgs e)
            => NavigateTo(new MessagesPage(), "Сообщения");

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
            => NavigateTo(new SettingsPage(), "Настройки");

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

        private void UpdateMessagesBadge()
        {
            var teacherId = UserSession.CurrentUser?.TeacherId;
            var unread = teacherId.HasValue
                ? MessageRequestService.GetUnreadTeacherCount(teacherId.Value)
                : 0;

            BtnMessages.Content = unread > 0
                ? $"✉️   Сообщения ({unread})"
                : "✉️   Сообщения";
        }
    }
}
