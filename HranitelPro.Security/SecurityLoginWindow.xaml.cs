using System;
using System.Windows;
using Npgsql;

namespace HranitelPro.Security
{
    public partial class SecurityLoginWindow : Window
    {
        private string connectionString = "Host=localhost;Port=5432;Database=hranitelpro;Username=postgres;Password=1;";

        public SecurityLoginWindow()
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
                    string query = "SELECT id, full_name FROM employees WHERE auth_code = @authCode AND section = 'Охрана'";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@authCode", authCode);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int empId = reader.GetInt32(0);
                                string empName = reader.GetString(1);

                                var mainWindow = new SecurityWindow(empId, empName);
                                mainWindow.Show();
                                this.Close();
                            }
                            else
                            {
                                StatusText.Text = "Неверный код или нет доступа";
                                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = ex.Message;
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
        }
    }
}