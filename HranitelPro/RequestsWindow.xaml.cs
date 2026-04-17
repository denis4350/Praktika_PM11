using System;
using System.Collections.Generic;
using System.Windows;
using Npgsql;

namespace HranitelPro
{
    public partial class RequestsWindow : Window
    {
        private string _connectionString = "Host=localhost;Port=5432;Database=hranitelpro;Username=postgres;Password=1;";
        private int _currentUserId;

        public RequestsWindow(int userId)
        {
            InitializeComponent();
            _currentUserId = userId;
            LoadRequests();
        }

        private void LoadRequests()
        {
            try
            {
                var requests = new List<MyRequestItem>();

                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT 
                            id, 
                            purpose, 
                            department, 
                            employee, 
                            start_date, 
                            end_date, 
                            status,
                            last_name, 
                            first_name, 
                            middle_name,
                            created_at,
                            CASE 
                                WHEN group_id IS NOT NULL AND group_id > 0 THEN 'Групповая'
                                ELSE 'Личная'
                            END as request_type
                        FROM requests 
                        WHERE user_id = @userId
                        ORDER BY created_at DESC";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", _currentUserId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var request = new MyRequestItem
                                {
                                    Id = reader.GetInt32(0),
                                    Purpose = reader.GetString(1),
                                    Department = reader.GetString(2),
                                    Employee = reader.GetString(3),
                                    StartDate = reader.GetDateTime(4).ToShortDateString(),
                                    EndDate = reader.GetDateTime(5).ToShortDateString(),
                                    Status = reader.GetString(6),
                                    VisitorName = $"{reader.GetString(7)} {reader.GetString(8)} {reader.GetString(9)}".Trim(),
                                    CreatedAt = reader.GetDateTime(10).ToShortDateString(),
                                    Type = reader.GetString(11)
                                };
                                requests.Add(request);
                            }
                        }
                    }
                }

                RequestsGrid.ItemsSource = requests;

                if (requests.Count == 0)
                {
                    MessageBox.Show("У вас пока нет заявок", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заявок: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Внутренний класс для отображения заявок
    public class MyRequestItem
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Employee { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string VisitorName { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }
}