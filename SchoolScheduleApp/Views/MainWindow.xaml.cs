using System.Windows;
using System.Windows.Input;
using SchoolScheduleApp.ViewModels;

namespace SchoolScheduleApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            if (DataContext is LoginViewModel vm && !string.IsNullOrWhiteSpace(vm.RememberedPassword))
            {
                UserPasswordBox.Password = vm.RememberedPassword;
                PasswordPlaceholder.Visibility = Visibility.Collapsed;
            }

            MouseDown += (_, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    DragMove();
                }
            };
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordPlaceholder.Visibility = UserPasswordBox.Password.Length > 0
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }
}
