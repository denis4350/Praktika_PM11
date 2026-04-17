using System;
using System.Windows;
using Npgsql;

namespace HranitelPro
{
    public partial class EmployeeLoginWindow : Window
    {
        private string connectionString = "Host=localhost;Port=5432;Database=hranitelpro;Username=postgres;Password=1;";
        public int EmployeeId { get; private set; } = -1;
        public string EmployeeName { get; private set; } = string.Empty;

        public EmployeeLoginWindow()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string authCode = AuthCodeBox.Password;

            if (string.IsNullOrWhiteSpace(authCode))
            {
                StatusText.Text = "Введите код сотрудника";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT id, full_name FROM employees 
                        WHERE auth_code = @authCode";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@authCode", authCode);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                EmployeeId = reader.GetInt32(0);
                                EmployeeName = reader.GetString(1);

                                StatusText.Text = $"✓ Добро пожаловать, {EmployeeName}!";
                                StatusText.Foreground = System.Windows.Media.Brushes.Green;

                                this.DialogResult = true;
                                this.Close();
                            }
                            else
                            {
                                StatusText.Text = "✗ Неверный код сотрудника";
                                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"✗ Ошибка: {ex.Message}";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
        }
    }
}