using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Npgsql;

namespace HranitelPro
{
    public partial class GeneralDepartmentWindow : Window
    {
        private string connectionString = "Host=localhost;Port=5432;Database=hranitelpro;Username=postgres;Password=1;";
        private int employeeId;

        public GeneralDepartmentWindow(int empId, string empName)
        {
            InitializeComponent();
            employeeId = empId;

            if (EmployeeInfo != null)
                EmployeeInfo.Text = $"Сотрудник: {empName}";

            // Ждём полной загрузки окна
            this.Loaded += GeneralDepartmentWindow_Loaded;
        }

        private void GeneralDepartmentWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDepartments();
            LoadRequests();
            LoadCurrentVisitors(); // Добавлен вызов загрузки списка посетителей
        }

        // ========== ЗАГРУЗКА ПОДРАЗДЕЛЕНИЙ ==========
        private void LoadDepartments()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT DISTINCT department FROM requests WHERE department IS NOT NULL AND department != ''";
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            var departments = new List<string> { "Все" };
                            while (reader.Read())
                            {
                                string dept = reader.GetString(0);
                                if (!string.IsNullOrEmpty(dept) && dept != "Все")
                                    departments.Add(dept);
                            }
                            DepartmentFilter.ItemsSource = departments;
                            DepartmentFilter.SelectedIndex = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки подразделений: {ex.Message}");
            }
        }

        // ========== ЗАГРУЗКА ЗАЯВОК ==========
        private void LoadRequests()
        {
            try
            {
                if (TypeFilter == null || DepartmentFilter == null || StatusFilter == null || RequestsGrid == null)
                {
                    return;
                }

                string type = (TypeFilter.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Все типы";
                string department = DepartmentFilter.SelectedItem?.ToString() ?? "Все";
                string status = (StatusFilter.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Все статусы";

                if (department == "Все") department = null;
                if (status == "Все статусы") status = null;

                var requests = new List<EmployeeRequestItem>();

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT 
                            r.id, 
                            r.purpose, 
                            r.department, 
                            r.employee, 
                            r.start_date, 
                            r.end_date, 
                            r.status,
                            r.last_name, 
                            r.first_name, 
                            r.middle_name,
                            r.passport_series,
                            r.passport_number,
                            CASE 
                                WHEN r.group_id IS NOT NULL AND r.group_id > 0 THEN 'Групповая'
                                ELSE 'Личная'
                            END as request_type
                        FROM requests r
                        ORDER BY r.created_at DESC";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var item = new EmployeeRequestItem
                                {
                                    RequestId = reader.GetInt32(0),
                                    Purpose = reader.GetString(1),
                                    Department = reader.GetString(2),
                                    Employee = reader.GetString(3),
                                    StartDate = reader.GetDateTime(4).ToShortDateString(),
                                    EndDate = reader.GetDateTime(5).ToShortDateString(),
                                    Status = reader.GetString(6),
                                    LastName = reader.GetString(7),
                                    FirstName = reader.GetString(8),
                                    MiddleName = reader.IsDBNull(9) ? "" : reader.GetString(9),
                                    PassportSeries = reader.GetString(10),
                                    PassportNumber = reader.GetString(11),
                                    RequestType = reader.GetString(12)
                                };

                                if (type != "Все типы" && item.RequestType != type) continue;
                                if (!string.IsNullOrEmpty(department) && item.Department != department) continue;
                                if (!string.IsNullOrEmpty(status) && item.Status != status) continue;

                                requests.Add(item);
                            }
                        }
                    }
                }

                RequestsGrid.ItemsSource = requests;
                if (StatusBar != null)
                    StatusBar.Text = $"Загружено заявок: {requests.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заявок: {ex.Message}");
            }
        }

        // ========== СПИСОК ЛИЦ НА ТЕРРИТОРИИ ==========
        private void LoadCurrentVisitors()
        {
            try
            {
                var visitors = new List<CurrentVisitorItem>();

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT 
                            r.last_name,
                            r.first_name,
                            r.middle_name,
                            r.department,
                            r.entry_time,
                            CASE WHEN r.group_id > 0 THEN 'Групповая' ELSE 'Личная' END as request_type,
                            r.status
                        FROM requests r
                        WHERE r.status = 'Одобрена' 
                        AND r.entry_time IS NOT NULL 
                        AND r.exit_time IS NULL
                        ORDER BY r.department, r.entry_time";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string lastName = reader.GetString(0);
                                string firstName = reader.GetString(1);
                                string middleName = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                string department = reader.GetString(3);
                                DateTime entryTime = reader.GetDateTime(4);
                                string requestType = reader.GetString(5);
                                string status = reader.GetString(6);

                                visitors.Add(new CurrentVisitorItem
                                {
                                    VisitorName = $"{lastName} {firstName} {middleName}".Trim(),
                                    Department = department,
                                    EntryTime = entryTime.ToString("dd.MM.yyyy HH:mm"),
                                    RequestType = requestType,
                                    Status = status
                                });
                            }
                        }
                    }
                }

                if (CurrentVisitorsGrid != null)
                    CurrentVisitorsGrid.ItemsSource = visitors;
                if (VisitorsCountText != null)
                    VisitorsCountText.Text = $"На территории: {visitors.Count} человек";
                if (StatusBar != null)
                    StatusBar.Text = $"На территории находится: {visitors.Count} человек";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка посетителей: {ex.Message}");
                if (StatusBar != null)
                    StatusBar.Text = $"Ошибка: {ex.Message}";
            }
        }

        // ========== ОБНОВЛЕНИЕ СПИСКА ПОСЕТИТЕЛЕЙ ==========
        private void RefreshVisitorsList_Click(object sender, RoutedEventArgs e)
        {
            LoadCurrentVisitors();
        }

        // ========== ОТКРЫТИЕ ОКНА ОТЧЁТОВ ==========
        private void OpenReports_Click(object sender, RoutedEventArgs e)
        {
            ReportsWindow reportsWindow = new ReportsWindow();
            reportsWindow.Owner = this;
            reportsWindow.ShowDialog();
        }

        // ========== СОБЫТИЯ ФИЛЬТРОВ ==========
        private void ApplyFilters(object sender, SelectionChangedEventArgs e)
        {
            LoadRequests();
        }

        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            TypeFilter.SelectedIndex = 0;
            DepartmentFilter.SelectedIndex = 0;
            StatusFilter.SelectedIndex = 0;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadRequests();
        }

        // ========== ОТКРЫТИЕ ЗАЯВКИ ==========
        private void OnRequestDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RequestsGrid.SelectedItem is EmployeeRequestItem selectedRequest)
            {
                var reviewWindow = new RequestReviewWindow(selectedRequest, employeeId);
                reviewWindow.Owner = this;
                reviewWindow.ShowDialog();
                LoadRequests();
            }
        }

    }

    // ========== ВСПОМОГАТЕЛЬНЫЕ КЛАССЫ ==========
    public class EmployeeRequestItem
    {
        public int RequestId { get; set; }
        public string RequestType { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Employee { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string PassportSeries { get; set; } = string.Empty;
        public string PassportNumber { get; set; } = string.Empty;

        public string VisitorName => $"{LastName} {FirstName} {MiddleName}".Trim();
        public string Passport => $"{PassportSeries} {PassportNumber}".Trim();
    }

    public class CurrentVisitorItem
    {
        public string VisitorName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string EntryTime { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}