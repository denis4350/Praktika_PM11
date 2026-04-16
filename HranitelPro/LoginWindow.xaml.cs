using Npgsql;
using System;
using System.Windows;
using System.Security.Cryptography;
using System.Text;
using HranitelPro.Models;
using System.Linq;

namespace HranitelPro
{
    public partial class LoginWindow : Window
    {
        string connectionString = "Host=localhost;Port=5432;Database=hranitelpro;Username=postgres;Password=1;";
        public int LoggedInUserId { get; private set; } = -1;

        public LoginWindow()
        {
            InitializeComponent();
        }

        private string GetMD5(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        // СПОСОБ 1: Прямой SQL запрос
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailBox.Text.Trim();
            string password = PasswordBox.Password;
            string hashedPassword = GetMD5(password);

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT id FROM users WHERE email=@email AND password=@password";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.Parameters.AddWithValue("@password", hashedPassword);

                        object? result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            LoggedInUserId = Convert.ToInt32(result);
                            StatusText.Text = "✓ Вход (SQL) выполнен успешно!";
                            StatusText.Foreground = System.Windows.Media.Brushes.Green;

                            // ========== КОД ВЫБОРА ТИПА ЗАЯВКИ ==========
                            ShowChoiceWindowAndProceed();
                            // ==========================================
                        }
                        else
                        {
                            StatusText.Text = "✗ Ошибка: неверный email или пароль";
                            StatusText.Foreground = System.Windows.Media.Brushes.Red;
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

        // СПОСОБ 2: Хранимая процедура
        private void LoginProcedure_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailBox.Text.Trim();
            string password = PasswordBox.Password;
            string hashedPassword = GetMD5(password);

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string createFunction = @"
                        CREATE OR REPLACE FUNCTION login_user(p_email TEXT, p_password TEXT)
                        RETURNS INTEGER AS $$
                        DECLARE
                            v_user_id INTEGER;
                        BEGIN
                            SELECT id INTO v_user_id
                            FROM users 
                            WHERE email = p_email AND password = p_password;
                            RETURN COALESCE(v_user_id, 0);
                        END;
                        $$ LANGUAGE plpgsql;";

                    using (var createCmd = new NpgsqlCommand(createFunction, conn))
                    {
                        createCmd.ExecuteNonQuery();
                    }

                    using (var cmd = new NpgsqlCommand("SELECT login_user(@email, @password)", conn))
                    {
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.Parameters.AddWithValue("@password", hashedPassword);

                        object? result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            int userId = Convert.ToInt32(result);
                            if (userId > 0)
                            {
                                LoggedInUserId = userId;
                                StatusText.Text = "✓ Вход (Процедура) выполнен успешно!";
                                StatusText.Foreground = System.Windows.Media.Brushes.Green;

                                // ========== КОД ВЫБОРА ТИПА ЗАЯВКИ ==========
                                ShowChoiceWindowAndProceed();
                                // ==========================================
                            }
                            else
                            {
                                StatusText.Text = "✗ Ошибка: неверный email или пароль";
                                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                            }
                        }
                        else
                        {
                            StatusText.Text = "✗ Ошибка: пользователь не найден";
                            StatusText.Foreground = System.Windows.Media.Brushes.Red;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"✗ Ошибка процедуры: {ex.Message}";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        // СПОСОБ 3: ORM (Entity Framework)
        private void LoginORM_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailBox.Text.Trim();
            string password = PasswordBox.Password;
            string hashedPassword = GetMD5(password);

            try
            {
                using (var context = new AppDbContext())
                {
                    var user = context.Users
                        .FirstOrDefault(u => u.Email == email && u.Password == hashedPassword);

                    if (user != null)
                    {
                        LoggedInUserId = user.Id;
                        StatusText.Text = "✓ Вход (ORM) выполнен успешно!";
                        StatusText.Foreground = System.Windows.Media.Brushes.Green;

                        // ========== КОД ВЫБОРА ТИПА ЗАЯВКИ ==========
                        ShowChoiceWindowAndProceed();
                        // ==========================================
                    }
                    else
                    {
                        StatusText.Text = "✗ Ошибка: неверный email или пароль";
                        StatusText.Foreground = System.Windows.Media.Brushes.Red;
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"✗ Ошибка ORM: {ex.Message}";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        // ========== ОБЩИЙ МЕТОД ДЛЯ ВЫБОРА ТИПА ЗАЯВКИ ==========
        private void ShowChoiceWindowAndProceed()
        {
            var choice = new ChoiceWindow();
            if (choice.ShowDialog() == true)
            {
                if (choice.VisitType == "personal")
                {
                    var mainWindow = new MainWindow(LoggedInUserId, false);
                    mainWindow.Show();
                }
                else
                {
                    var groupWindow = new GroupRequestWindow(LoggedInUserId);
                    groupWindow.Show();
                }
                this.Close(); // Закрываем окно авторизации
            }
        }
        // =======================================================

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            RegisterWindow register = new RegisterWindow();
            register.ShowDialog();
        }
    }
}