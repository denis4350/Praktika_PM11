using System.Windows;

namespace HranitelPro
{
    public partial class ModeSelectionWindow : Window
    {
        public ModeSelectionWindow()
        {
            InitializeComponent();
        }

        // Режим посетителя
        private void VisitorMode_Click(object sender, RoutedEventArgs e)
        {
            // Открываем окно авторизации посетителя
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        // Режим сотрудника общего отдела
        private void EmployeeMode_Click(object sender, RoutedEventArgs e)
        {
            // Открываем окно авторизации сотрудника
            var employeeLogin = new EmployeeLoginWindow();
            if (employeeLogin.ShowDialog() == true)
            {
                var departmentWindow = new GeneralDepartmentWindow(
                    employeeLogin.EmployeeId,
                    employeeLogin.EmployeeName
                );
                departmentWindow.Show();
                this.Close();
            }
        }
    }
}