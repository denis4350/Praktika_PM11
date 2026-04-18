using System;
using System.Windows;
using System.Windows.Controls;
using Npgsql;

namespace HranitelPro.Department
{
    public partial class VisitManagementWindow : Window
    {
        private string connectionString = "Host=localhost;Port=5432;Database=hranitelpro;Username=postgres;Password=1;";
        private DepartmentRequestItem request;
        private int employeeId;
        private string employeeName;
        private string department;

        public VisitManagementWindow(DepartmentRequestItem selectedRequest, int empId, string empName, string dept)
        {
            InitializeComponent();
            request = selectedRequest;
            employeeId = empId;
            employeeName = empName;
            department = dept;

            LoadRequestData();
            LoadExistingTimes();
            SetupContextMenu();
        }

        private void LoadRequestData()
        {
            VisitorNameText.Text = request.VisitorName;
            PassportText.Text = request.Passport;
            DepartmentText.Text = request.Department;
            EmployeeText.Text = request.Employee;
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

        private void SetupContextMenu()
        {
            var contextMenu = new ContextMenu();
            var menuItem = new MenuItem { Header = "🚫 Добавить в чёрный список..." };
            menuItem.Click += AddToBlacklist_Click;
            contextMenu.Items.Add(menuItem);

            // Привязываем контекстное меню к тексту ФИО
            VisitorNameText.ContextMenu = contextMenu;
        }

        private void AddToBlacklist_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new BlacklistReasonWindow(request.LastName, request.FirstName, request.MiddleName,
                                                   request.PassportSeries, request.PassportNumber);
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        private void UpdateEntryTime(string time)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE requests SET entry_time = @time, department_confirmed = TRUE WHERE id = @id";
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

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}