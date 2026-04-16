using System;
using System.Collections.ObjectModel;
using System.Windows;
using Npgsql;

namespace HranitelPro
{
    public partial class GroupRequestWindow : Window
    {
        private int currentUserId;
        private string connectionString = "Host=localhost;Port=5432;Database=hranitelpro;Username=postgres;Password=1;";
        private ObservableCollection<Visitor> visitors = new ObservableCollection<Visitor>();

        public GroupRequestWindow(int userId)
        {
            InitializeComponent();
            currentUserId = userId;
            VisitorsGrid.ItemsSource = visitors;
        }


        private void AddVisitor_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VisitorDialog();
            if (dialog.ShowDialog() == true && dialog.Visitor != null) 
            {
                visitors.Add(dialog.Visitor);
            }
        }

        private void RemoveVisitor_Click(object sender, RoutedEventArgs e)
        {
            if (VisitorsGrid.SelectedItem is Visitor selected)
            {
                visitors.Remove(selected);
            }
        }

        private void SubmitRequest_Click(object sender, RoutedEventArgs e)
        {
            if (visitors.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы одного посетителя");
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
                             passport_series, passport_number, user_id, created_at) 
                            VALUES 
                            (@purpose, @department, @employee, @start, @end,
                             @lastName, @firstName, @middleName, @phone, @email,
                             @passportSeries, @passportNumber, @userId, @createdAt)";

                        using (var cmd = new NpgsqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@purpose", PurposeBox.Text);
                            cmd.Parameters.AddWithValue("@department", DepartmentBox.Text);
                            cmd.Parameters.AddWithValue("@employee", EmployeeBox.Text);
                            cmd.Parameters.AddWithValue("@start", StartDate.SelectedDate ?? DateTime.Now);
                            cmd.Parameters.AddWithValue("@end", EndDate.SelectedDate ?? DateTime.Now);
                            cmd.Parameters.AddWithValue("@lastName", visitor.LastName);
                            cmd.Parameters.AddWithValue("@firstName", visitor.FirstName);
                            cmd.Parameters.AddWithValue("@middleName", visitor.MiddleName ?? "");
                            cmd.Parameters.AddWithValue("@phone", visitor.Phone);
                            cmd.Parameters.AddWithValue("@email", visitor.Email);
                            cmd.Parameters.AddWithValue("@passportSeries", visitor.PassportSeries);
                            cmd.Parameters.AddWithValue("@passportNumber", visitor.PassportNumber);
                            cmd.Parameters.AddWithValue("@userId", currentUserId);
                            cmd.Parameters.AddWithValue("@createdAt", DateTime.Now);

                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                MessageBox.Show($"Заявка для {visitors.Count} посетителей успешно отправлена!");
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