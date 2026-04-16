using Npgsql;
using System;
using System.Windows;
using System.Windows.Controls;

namespace HranitelPro
{
    public partial class MainWindow : Window
    {
        private string connectionString = "Host=localhost;Port=5432;Database=hranitelpro;Username=postgres;Password=1;";
        private int currentUserId = -1;
        private bool isGroupMode = false;

        public MainWindow(int userId, bool groupMode = false)
        {
            InitializeComponent();
            currentUserId = userId;
            isGroupMode = groupMode;

            // Устанавливаем даты
            StartDate.SelectedDate = DateTime.Now.AddDays(1);
            EndDate.SelectedDate = DateTime.Now.AddDays(8);
            BirthDateBox.SelectedDate = DateTime.Now.AddYears(-16);

            if (isGroupMode)
            {
                var groupWindow = new GroupRequestWindow(currentUserId);
                groupWindow.Show();
                this.Close();
            }
            else
            {
                LoadDepartments();
            }
        }

        private void LoadDepartments()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT id, name FROM departments ORDER BY name";
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            var departments = new System.Collections.Generic.List<Department>();
                            while (reader.Read())
                            {
                                departments.Add(new Department
                                {
                                    Id = reader.GetInt32(0),
                                    Name = reader.GetString(1)
                                });
                            }
                            DepartmentCombo.ItemsSource = departments;
                            DepartmentCombo.DisplayMemberPath = "Name";
                            DepartmentCombo.SelectedValuePath = "Id";
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
                var selectedDept = (Department)DepartmentCombo.SelectedItem;
                LoadEmployees(selectedDept.Id);
            }
        }

        private void LoadEmployees(int departmentId)
        {
            try
            {
                MessageBox.Show($"Загрузка сотрудников для departmentId = {departmentId}");

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                SELECT id, full_name FROM employees 
                WHERE department_id = @deptId
                ORDER BY full_name";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@deptId", departmentId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            var employees = new System.Collections.Generic.List<Employee>();
                            while (reader.Read())
                            {
                                employees.Add(new Employee
                                {
                                    Id = reader.GetInt32(0),
                                    FullName = reader.GetString(1)
                                });
                            }

                            MessageBox.Show($"Найдено сотрудников: {employees.Count}");

                            EmployeeCombo.ItemsSource = employees;
                            EmployeeCombo.DisplayMemberPath = "FullName";
                            EmployeeCombo.SelectedValuePath = "Id";
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

        private bool ValidateForm()
        {
            // Проверка дат
            if (StartDate.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату начала");
                return false;
            }
            if (EndDate.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату окончания");
                return false;
            }

            DateTime start = StartDate.SelectedDate.Value;
            DateTime end = EndDate.SelectedDate.Value;
            DateTime today = DateTime.Now.Date;

            if (start < today.AddDays(1))
            {
                MessageBox.Show("Дата начала должна быть не раньше следующего дня");
                return false;
            }
            if (start > today.AddDays(15))
            {
                MessageBox.Show("Дата начала не может быть позже чем через 15 дней");
                return false;
            }
            if (end < start)
            {
                MessageBox.Show("Дата окончания не может быть раньше даты начала");
                return false;
            }
            if (end > start.AddDays(15))
            {
                MessageBox.Show("Дата окончания не может быть позже чем через 15 дней от даты начала");
                return false;
            }

            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(PurposeBox.Text))
            {
                MessageBox.Show("Введите цель посещения");
                return false;
            }
            if (DepartmentCombo.SelectedItem == null)
            {
                MessageBox.Show("Выберите подразделение");
                return false;
            }
            if (EmployeeCombo.SelectedItem == null)
            {
                MessageBox.Show("Выберите сотрудника");
                return false;
            }
            if (string.IsNullOrWhiteSpace(LastNameBox.Text))
            {
                MessageBox.Show("Введите фамилию");
                return false;
            }
            if (string.IsNullOrWhiteSpace(FirstNameBox.Text))
            {
                MessageBox.Show("Введите имя");
                return false;
            }
            if (BirthDateBox.SelectedDate == null)
            {
                MessageBox.Show("Введите дату рождения");
                return false;
            }

            // Проверка возраста
            DateTime birthDate = BirthDateBox.SelectedDate.Value;
            int age = DateTime.Now.Year - birthDate.Year;
            if (birthDate > DateTime.Now.AddYears(-age)) age--;
            if (age < 16)
            {
                MessageBox.Show("Возраст посетителя должен быть не младше 16 лет");
                return false;
            }

            // Проверка email
            if (string.IsNullOrWhiteSpace(EmailBox.Text) || !EmailBox.Text.Contains("@"))
            {
                MessageBox.Show("Введите корректный email");
                return false;
            }

            // Проверка паспорта
            if (PassportSeriesBox.Text.Length != 4 || !int.TryParse(PassportSeriesBox.Text, out _))
            {
                MessageBox.Show("Серия паспорта должна содержать 4 цифры");
                return false;
            }
            if (PassportNumberBox.Text.Length != 6 || !int.TryParse(PassportNumberBox.Text, out _))
            {
                MessageBox.Show("Номер паспорта должен содержать 6 цифр");
                return false;
            }

            return true;
        }

        private void CreateRequest_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                        INSERT INTO requests 
                        (purpose, department, employee, start_date, end_date, 
                         last_name, first_name, middle_name, phone, email, 
                         passport_series, passport_number, user_id, created_at, 
                         organization, note, birth_date, status) 
                        VALUES 
                        (@purpose, @department, @employee, @start, @end,
                         @lastName, @firstName, @middleName, @phone, @email,
                         @passportSeries, @passportNumber, @userId, @createdAt,
                         @organization, @note, @birthDate, @status)";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@purpose", PurposeBox.Text);
                        cmd.Parameters.AddWithValue("@department", ((Department)DepartmentCombo.SelectedItem).Name);
                        cmd.Parameters.AddWithValue("@employee", ((Employee)EmployeeCombo.SelectedItem).FullName);
                        cmd.Parameters.AddWithValue("@start", StartDate.SelectedDate.Value);
                        cmd.Parameters.AddWithValue("@end", EndDate.SelectedDate.Value);
                        cmd.Parameters.AddWithValue("@lastName", LastNameBox.Text);
                        cmd.Parameters.AddWithValue("@firstName", FirstNameBox.Text);
                        cmd.Parameters.AddWithValue("@middleName", string.IsNullOrEmpty(MiddleNameBox.Text) ? "" : MiddleNameBox.Text);
                        cmd.Parameters.AddWithValue("@phone", string.IsNullOrEmpty(PhoneBox.Text) ? "" : PhoneBox.Text);
                        cmd.Parameters.AddWithValue("@email", EmailBox.Text);
                        cmd.Parameters.AddWithValue("@passportSeries", PassportSeriesBox.Text);
                        cmd.Parameters.AddWithValue("@passportNumber", PassportNumberBox.Text);
                        cmd.Parameters.AddWithValue("@userId", currentUserId);
                        cmd.Parameters.AddWithValue("@createdAt", DateTime.Now);
                        cmd.Parameters.AddWithValue("@organization", string.IsNullOrEmpty(OrganizationBox.Text) ? "" : OrganizationBox.Text);
                        cmd.Parameters.AddWithValue("@note", string.IsNullOrEmpty(NoteBox.Text) ? "" : NoteBox.Text);
                        cmd.Parameters.AddWithValue("@birthDate", BirthDateBox.SelectedDate.Value);
                        cmd.Parameters.AddWithValue("@status", "На проверке");

                        cmd.ExecuteNonQuery();
                    }
                }

                StatusText.Text = "✓ Заявка успешно отправлена!";
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
                MessageBox.Show("Заявка успешно отправлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"✗ Ошибка: {ex.Message}";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                MessageBox.Show($"Ошибка при отправке: {ex.Message}");
            }
        }

        private void ClearForm()
        {
            PurposeBox.Text = "";
            LastNameBox.Text = "";
            FirstNameBox.Text = "";
            MiddleNameBox.Text = "";
            PhoneBox.Text = "";
            EmailBox.Text = "";
            PassportSeriesBox.Text = "";
            PassportNumberBox.Text = "";
            OrganizationBox.Text = "";
            NoteBox.Text = "";
            DepartmentCombo.SelectedItem = null;
            EmployeeCombo.SelectedItem = null;
            EmployeeCombo.IsEnabled = false;
            StartDate.SelectedDate = DateTime.Now.AddDays(1);
            EndDate.SelectedDate = DateTime.Now.AddDays(8);
            BirthDateBox.SelectedDate = DateTime.Now.AddYears(-16);
        }

        private void ViewRequests_Click(object sender, RoutedEventArgs e)
        {
            var requestsWindow = new RequestsWindow(currentUserId);
            requestsWindow.ShowDialog();
        }
    }

    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class Employee
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
    }
}