using System;
using System.Media;
using System.Windows;
using Npgsql;

namespace HranitelPro.Security
{
    public partial class SecurityAccessWindow : Window
    {
        private string connectionString = "Host=localhost;Port=5432;Database=hranitelpro;Username=postgres;Password=1;";
        private SecurityRequestItem request;
        private int employeeId;
        private string employeeName;

        public SecurityAccessWindow(SecurityRequestItem selectedRequest, int empId, string empName)
        {
            InitializeComponent();
            request = selectedRequest;
            employeeId = empId;
            employeeName = empName;

            LoadRequestData();
            LoadExistingTimes();
        }

        private void LoadRequestData()
        {
            VisitorNameText.Text = request.VisitorName;
            PassportText.Text = request.Passport;
            DepartmentText.Text = request.Department;
            EmployeeText.Text = request.Employee;
            StatusText.Text = request.Status;

            // Цвет статуса
            if (request.Status == "Одобрена")
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
        }

        private void LoadExistingTimes()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT entry_time, exit_time FROM requests WHERE id = @id";
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", request.RequestId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                if (!reader.IsDBNull(0))
                                {
                                    DateTime entryTime = reader.GetDateTime(0);
                                    EntryTimeBox.Text = entryTime.ToString("HH:mm");
                                    EntryButton.IsEnabled = false;
                                    EntryButton.Content = "✓ ВХОД ЗАФИКСИРОВАН";
                                }
                                if (!reader.IsDBNull(1))
                                {
                                    DateTime exitTime = reader.GetDateTime(1);
                                    ExitTimeBox.Text = exitTime.ToString("HH:mm");
                                    ExitButton.IsEnabled = false;
                                    ExitButton.Content = "✓ ВЫХОД ЗАФИКСИРОВАН";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки времени: {ex.Message}");
            }
        }

        private void UpdateEntryTime(string time)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE requests SET entry_time = @time, guard_confirmed = TRUE WHERE id = @id";
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@time", DateTime.Parse(time));
                        cmd.Parameters.AddWithValue("@id", request.RequestId);
                        cmd.ExecuteNonQuery();
                    }
                }
                EntryButton.IsEnabled = false;
                EntryButton.Content = "✓ ВХОД ЗАФИКСИРОВАН";
                MessageBox.Show("Время входа зафиксировано!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void UpdateExitTime(string time)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE requests SET exit_time = @time WHERE id = @id";
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@time", DateTime.Parse(time));
                        cmd.Parameters.AddWithValue("@id", request.RequestId);
                        cmd.ExecuteNonQuery();
                    }
                }
                ExitButton.IsEnabled = false;
                ExitButton.Content = "✓ ВЫХОД ЗАФИКСИРОВАН";
                MessageBox.Show("Время выхода зафиксировано!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void EntryButton_Click(object sender, RoutedEventArgs e)
        {
            if (EntryTimeBox.Text == "HH:MM")
            {
                MessageBox.Show("Введите корректное время");
                return;
            }
            UpdateEntryTime(EntryTimeBox.Text);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            if (ExitTimeBox.Text == "HH:MM")
            {
                MessageBox.Show("Введите корректное время");
                return;
            }
            UpdateExitTime(ExitTimeBox.Text);
        }

        private void OpenTurnstile_Click(object sender, RoutedEventArgs e)
        {
            // Системный звук
            SystemSounds.Beep.Play();

            // Отправка сообщения на сервер
            MessageBox.Show($"🚪 ТУРНИКЕТ ОТКРЫТ!\n\nПосетитель: {request.VisitorName}\nСотрудник охраны: {employeeName}\nВремя: {DateTime.Now:HH:mm:ss}",
                "Доступ разрешён", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}