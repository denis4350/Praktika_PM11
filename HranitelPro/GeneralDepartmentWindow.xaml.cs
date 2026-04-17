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
        private int _employeeId;

        public GeneralDepartmentWindow(int empId, string empName)
        {
            InitializeComponent();
            _employeeId = empId;
            EmployeeInfoText.Text = $"Сотрудник: {empName}";

            LoadDepartments();
            LoadRequests();
        }

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
                            DepartmentCombo.ItemsSource = departments;
                            DepartmentCombo.SelectedIndex = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки подразделений: {ex.Message}");
            }
        }

        private void LoadRequests()
        {
            try
            {
                // Безопасное получение значений фильтров
                string type = "Все типы";
                string department = "Все";
                string status = "Все статусы";

                if (TypeCombo != null && TypeCombo.SelectedItem != null)
                {
                    var selectedType = TypeCombo.SelectedItem as ComboBoxItem;
                    if (selectedType != null) type = selectedType.Content.ToString();
                }

                if (DepartmentCombo != null && DepartmentCombo.SelectedItem != null)
                {
                    department = DepartmentCombo.SelectedItem.ToString();
                }

                if (StatusCombo != null && StatusCombo.SelectedItem != null)
                {
                    var selectedStatus = StatusCombo.SelectedItem as ComboBoxItem;
                    if (selectedStatus != null) status = selectedStatus.Content.ToString();
                }

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

                                // Применяем фильтры
                                if (type != "Все типы" && item.RequestType != type) continue;
                                if (!string.IsNullOrEmpty(department) && item.Department != department) continue;
                                if (!string.IsNullOrEmpty(status) && item.Status != status) continue;

                                requests.Add(item);
                            }
                        }
                    }
                }

                if (RequestsDataGrid != null)
                    RequestsDataGrid.ItemsSource = requests;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заявок: {ex.Message}");
            }
        }

        private void OnFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadRequests();
        }

        private void OnResetFilters(object sender, RoutedEventArgs e)
        {
            if (TypeCombo != null) TypeCombo.SelectedIndex = 0;
            if (DepartmentCombo != null) DepartmentCombo.SelectedIndex = 0;
            if (StatusCombo != null) StatusCombo.SelectedIndex = 0;
        }

        private void OnRefresh(object sender, RoutedEventArgs e)
        {
            LoadRequests();
        }

        private void OnRequestDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RequestsDataGrid != null && RequestsDataGrid.SelectedItem is EmployeeRequestItem selectedRequest)
            {
                var reviewWindow = new RequestReviewWindow(selectedRequest, _employeeId);
                reviewWindow.Owner = this;
                reviewWindow.ShowDialog();
                LoadRequests();
            }
        }
    }

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

        public System.Windows.Media.Brush StatusColor
        {
            get
            {
                return Status switch
                {
                    "Одобрена" => System.Windows.Media.Brushes.Green,
                    "Отклонена" => System.Windows.Media.Brushes.Red,
                    _ => System.Windows.Media.Brushes.Orange
                };
            }
        }
    }
}