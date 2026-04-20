using Npgsql;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace HranitelPro
{
    public partial class GroupRequestWindow : Window
    {
        private int currentUserId;
        private string connectionString = "Host=localhost;Port=5432;Database=hranitelpro;Username=postgres;Password=1;";
        private ObservableCollection<Visitor> visitors = new ObservableCollection<Visitor>();
        private int groupId;

        public GroupRequestWindow(int userId)
        {
            InitializeComponent();
            currentUserId = userId;
            groupId = new Random().Next(10000, 99999);
            VisitorsGrid.ItemsSource = visitors;
            LoadDepartments();
            SetDefaultDates();
        }

        private void SetDefaultDates()
        {
            StartDate.SelectedDate = DateTime.Now.AddDays(1);
            EndDate.SelectedDate = DateTime.Now.AddDays(8);
        }

        private void LoadDepartments()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT name FROM departments ORDER BY name";
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            var departments = new ObservableCollection<string>();
                            while (reader.Read())
                            {
                                departments.Add(reader.GetString(0));
                            }
                            DepartmentCombo.ItemsSource = departments;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки подразделений: {ex.Message}");
            }
        }

        private void DepartmentCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DepartmentCombo.SelectedItem != null)
            {
                string? department = DepartmentCombo.SelectedItem.ToString();
                if (!string.IsNullOrEmpty(department))
                {
                    LoadEmployees(department);
                }
            }
        }

        private void LoadEmployees(string department)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT full_name FROM employees 
                        WHERE section = @deptName OR department = @deptName";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@deptName", department ?? "");
                        using (var reader = cmd.ExecuteReader())
                        {
                            var employees = new ObservableCollection<string>();
                            while (reader.Read())
                            {
                                employees.Add(reader.GetString(0));
                            }
                            EmployeeCombo.ItemsSource = employees;
                            EmployeeCombo.IsEnabled = employees.Count > 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сотрудников: {ex.Message}");
            }
        }

        private void AddVisitor_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VisitorDialog();
            if (dialog.ShowDialog() == true && dialog.Visitor != null)
            {
                visitors.Add(dialog.Visitor);
                UpdateVisitorsCount();
            }
        }

        private void RemoveVisitor_Click(object sender, RoutedEventArgs e)
        {
            if (VisitorsGrid.SelectedItem is Visitor selected)
            {
                visitors.Remove(selected);
                UpdateVisitorsCount();
            }
        }

        private void UpdateVisitorsCount()
        {
            if (VisitorsCountText != null)
                VisitorsCountText.Text = visitors.Count.ToString();
        }

        private void SubmitRequest_Click(object sender, RoutedEventArgs e)
        {
            if (visitors.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы одного посетителя");
                return;
            }

            if (string.IsNullOrWhiteSpace(PurposeBox?.Text))
            {
                MessageBox.Show("Введите цель посещения");
                return;
            }

            if (DepartmentCombo?.SelectedItem == null)
            {
                MessageBox.Show("Выберите подразделение");
                return;
            }

            if (EmployeeCombo?.SelectedItem == null)
            {
                MessageBox.Show("Выберите сотрудника");
                return;
            }

            if (StartDate?.SelectedDate == null || EndDate?.SelectedDate == null)
            {
                MessageBox.Show("Выберите даты посещения");
                return;
            }

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    foreach (var visitor in visitors)
                    {
                        string query = @"
                            INSERT INTO requests 
                            (purpose, department, employee, start_date, end_date, 
                             last_name, first_name, middle_name, phone, email, 
                             passport_series, passport_number, user_id, created_at,
                             group_id, status) 
                            VALUES 
                            (@purpose, @department, @employee, @start, @end,
                             @lastName, @firstName, @middleName, @phone, @email,
                             @passportSeries, @passportNumber, @userId, @createdAt,
                             @groupId, @status)";

                        using (var cmd = new NpgsqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@purpose", PurposeBox.Text ?? "");
                            cmd.Parameters.AddWithValue("@department", DepartmentCombo.SelectedItem.ToString() ?? "");
                            cmd.Parameters.AddWithValue("@employee", EmployeeCombo.SelectedItem.ToString() ?? "");
                            cmd.Parameters.AddWithValue("@start", StartDate.SelectedDate.Value);
                            cmd.Parameters.AddWithValue("@end", EndDate.SelectedDate.Value);
                            cmd.Parameters.AddWithValue("@lastName", visitor.LastName ?? "");
                            cmd.Parameters.AddWithValue("@firstName", visitor.FirstName ?? "");
                            cmd.Parameters.AddWithValue("@middleName", visitor.MiddleName ?? "");
                            cmd.Parameters.AddWithValue("@phone", visitor.Phone ?? "");
                            cmd.Parameters.AddWithValue("@email", visitor.Email ?? "");
                            cmd.Parameters.AddWithValue("@passportSeries", visitor.PassportSeries ?? "");
                            cmd.Parameters.AddWithValue("@passportNumber", visitor.PassportNumber ?? "");
                            cmd.Parameters.AddWithValue("@userId", currentUserId);
                            cmd.Parameters.AddWithValue("@createdAt", DateTime.Now);
                            cmd.Parameters.AddWithValue("@groupId", groupId);
                            cmd.Parameters.AddWithValue("@status", "На проверке");

                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                MessageBox.Show($"Групповая заявка успешно отправлена!\nГруппа ID: {groupId}\nКоличество посетителей: {visitors.Count}");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class Visitor
    {
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PassportSeries { get; set; } = string.Empty;
        public string PassportNumber { get; set; } = string.Empty;
    }
}