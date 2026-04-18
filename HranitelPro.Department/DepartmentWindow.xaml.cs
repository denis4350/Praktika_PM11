using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Npgsql;

namespace HranitelPro.Department
{
    public partial class DepartmentWindow : Window
    {
        private string connectionString = "Host=localhost;Port=5432;Database=hranitelpro;Username=postgres;Password=1;";
        private int employeeId;
        private string employeeName;
        private string department;

        public DepartmentWindow(int empId, string empName, string dept)
        {
            InitializeComponent();
            employeeId = empId;
            employeeName = empName;
            department = dept;
            EmployeeInfo.Text = $"Сотрудник: {empName} | Подразделение: {dept}";

            LoadRequests();
        }

        private void LoadRequests()
        {
            try
            {
                DateTime? filterDate = DateFilter?.SelectedDate;

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
                            r.entry_time,
                            r.exit_time,
                            CASE 
                                WHEN r.group_id IS NOT NULL AND r.group_id > 0 THEN 'Групповая'
                                ELSE 'Личная'
                            END as request_type
                        FROM requests r
                        WHERE r.status = 'Одобрена' 
                        AND r.department = @department
                        ORDER BY r.start_date DESC";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@department", department);

                        using (var reader = cmd.ExecuteReader())
                        {
                            var requests = new List<DepartmentRequestItem>();
                            while (reader.Read())
                            {
                                var item = new DepartmentRequestItem
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
                                    EntryTime = reader.IsDBNull(12) ? "" : Convert.ToDateTime(reader[12]).ToString("HH:mm"),
                                    ExitTime = reader.IsDBNull(13) ? "" : Convert.ToDateTime(reader[13]).ToString("HH:mm"),
                                    RequestType = reader.GetString(14)
                                };

                                if (filterDate.HasValue)
                                {
                                    if (!item.StartDate.Contains(filterDate.Value.ToString("dd.MM.yyyy")))
                                        continue;
                                }

                                requests.Add(item);
                            }
                            RequestsGrid.ItemsSource = requests;
                            StatusBar.Text = $"Найдено заявок: {requests.Count}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заявок: {ex.Message}");
            }
        }

        private void DateFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            LoadRequests();
        }

        private void ResetFilter_Click(object sender, RoutedEventArgs e)
        {
            DateFilter.SelectedDate = null;
            LoadRequests();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadRequests();
        }

        private void OnRequestDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RequestsGrid.SelectedItem is DepartmentRequestItem selectedRequest)
            {
                var visitWindow = new VisitManagementWindow(selectedRequest, employeeId, employeeName, department);
                visitWindow.Owner = this;
                visitWindow.ShowDialog();
                LoadRequests();
            }
        }
    }

    public class DepartmentRequestItem
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
        public string EntryTime { get; set; } = string.Empty;
        public string ExitTime { get; set; } = string.Empty;

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