using System;
using System.Text.RegularExpressions;
using System.Windows;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
using HranitelPro.Models;
using System.Linq;

namespace HranitelPro
{
    public partial class RegisterWindow : Window
    {
        string connectionString = "Host=localhost;Port=5432;Database=hranitelpro;Username=postgres;Password=1;";

        public RegisterWindow()
        {
            InitializeComponent();
        }

        // MD5 хеширование (по требованию ТЗ)
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

        // Полная валидация пароля по ТЗ
        private (bool IsValid, string ErrorMessage) ValidatePassword(string password)
        {
            // 1. Минимум 8 символов
            if (password.Length < 8)
            {
                return (false, "Пароль должен содержать минимум 8 символов");
            }

            // 2. Хотя бы одна заглавная буква
            if (!Regex.IsMatch(password, @"[A-ZА-Я]"))
            {
                return (false, "Пароль должен содержать хотя бы одну заглавную букву (A-Z, А-Я)");
            }

            // 3. Хотя бы одна строчная буква
            if (!Regex.IsMatch(password, @"[a-zа-я]"))
            {
                return (false, "Пароль должен содержать хотя бы одну строчную букву (a-z, а-я)");
            }

            // 4. Хотя бы одна цифра
            if (!Regex.IsMatch(password, @"[0-9]"))
            {
                return (false, "Пароль должен содержать хотя бы одну цифру (0-9)");
            }

            // 5. Хотя бы один спецсимвол
            if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':\\|,.<>\/?~`]"))
            {
                return (false, "Пароль должен содержать хотя бы один спецсимвол (!@#$%^&*()_+ и т.д.)");
            }

            return (true, "");
        }

        // Валидация email
        private (bool IsValid, string ErrorMessage) ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return (false, "Email обязателен для заполнения");
            }

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                if (addr.Address != email)
                {
                    return (false, "Введите корректный email (пример: user@domain.ru)");
                }
                return (true, "");
            }
            catch
            {
                return (false, "Введите корректный email (пример: user@domain.ru)");
            }
        }

        // Проверка существования пользователя
        private bool IsEmailExists(string email)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM users WHERE email = @email";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@email", email);
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        private void UpdateStatus(bool success, string message)
        {
            StatusText.Text = message;
            StatusText.Foreground = success ?
                System.Windows.Media.Brushes.Green :
                System.Windows.Media.Brushes.Red;
        }

        // Общая валидация формы
        private bool ValidateForm(out string email, out string password, out string confirmPassword)
        {
            email = EmailBox.Text.Trim();
            password = PasswordBox.Password;
            confirmPassword = ConfirmPasswordBox.Password;

            // Валидация email
            var emailValidation = ValidateEmail(email);
            if (!emailValidation.IsValid)
            {
                MessageBox.Show(emailValidation.ErrorMessage);
                return false;
            }

            // Валидация пароля
            var passwordValidation = ValidatePassword(password);
            if (!passwordValidation.IsValid)
            {
                MessageBox.Show(passwordValidation.ErrorMessage);
                return false;
            }

            // Проверка совпадения паролей
            if (password != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают");
                return false;
            }

            return true;
        }

        // СПОСОБ 1: Прямой SQL запрос
        private void RegisterSQL_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm(out string email, out string password, out _)) return;

            // Проверка существования пользователя
            if (IsEmailExists(email))
            {
                UpdateStatus(false, "Пользователь с таким email уже существует");
                MessageBox.Show("Пользователь с таким email уже существует");
                return;
            }

            string hashedPassword = GetMD5(password); // MD5 хеширование по ТЗ

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string query = "INSERT INTO users (email, password, created_at) VALUES (@email, @password, @createdAt)";
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.Parameters.AddWithValue("@password", hashedPassword);
                        cmd.Parameters.AddWithValue("@createdAt", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }
                }

                UpdateStatus(true, "✓ Регистрация (SQL) успешна!");
                MessageBox.Show("Регистрация успешна! Теперь вы можете войти.");
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                UpdateStatus(false, $"✗ Ошибка: {ex.Message}");
            }
        }

        // СПОСОБ 2: Хранимая процедура
        private void RegisterProcedure_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm(out string email, out string password, out _)) return;

            string hashedPassword = GetMD5(password); // MD5 хеширование по ТЗ

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    // Создаём функцию для регистрации с проверкой пароля
                    string createFunction = @"
                        CREATE OR REPLACE FUNCTION register_user(
                            p_email TEXT, 
                            p_password TEXT,
                            OUT success BOOLEAN,
                            OUT message TEXT
                        ) AS $$
                        DECLARE
                            user_exists INTEGER;
                        BEGIN
                            -- Проверка существования пользователя
                            SELECT COUNT(*) INTO user_exists FROM users WHERE email = p_email;
                            
                            IF user_exists > 0 THEN
                                success := FALSE;
                                message := 'Пользователь с таким email уже существует';
                                RETURN;
                            END IF;
                            
                            -- Создание пользователя
                            INSERT INTO users (email, password, created_at) 
                            VALUES (p_email, p_password, CURRENT_TIMESTAMP);
                            
                            success := TRUE;
                            message := 'Регистрация успешна';
                        END;
                        $$ LANGUAGE plpgsql;";

                    using (var createCmd = new NpgsqlCommand(createFunction, conn))
                    {
                        createCmd.ExecuteNonQuery();
                    }

                    // Вызываем функцию
                    using (var cmd = new NpgsqlCommand("SELECT * FROM register_user(@email, @password)", conn))
                    {
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.Parameters.AddWithValue("@password", hashedPassword);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                bool success = reader.GetBoolean(0);
                                string message = reader.GetString(1);

                                if (success)
                                {
                                    UpdateStatus(true, "✓ Регистрация (Процедура) успешна!");
                                    MessageBox.Show("Регистрация успешна! Теперь вы можете войти.");
                                    this.DialogResult = true;
                                    this.Close();
                                }
                                else
                                {
                                    UpdateStatus(false, $"✗ {message}");
                                    MessageBox.Show(message);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus(false, $"✗ Ошибка процедуры: {ex.Message}");
            }
        }

        // СПОСОБ 3: ORM (Entity Framework)
        private void RegisterORM_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm(out string email, out string password, out _)) return;

            string hashedPassword = GetMD5(password); // MD5 хеширование по ТЗ

            try
            {
                using (var context = new AppDbContext())
                {
                    // Проверка существования
                    var existingUser = context.Users.FirstOrDefault(u => u.Email == email);
                    if (existingUser != null)
                    {
                        UpdateStatus(false, "Пользователь с таким email уже существует");
                        MessageBox.Show("Пользователь с таким email уже существует");
                        return;
                    }

                    // Создание пользователя
                    var newUser = new User
                    {
                        Email = email,
                        Password = hashedPassword,
                        CreatedAt = DateTime.Now
                    };

                    context.Users.Add(newUser);
                    context.SaveChanges();
                }

                UpdateStatus(true, "✓ Регистрация (ORM) успешна!");
                MessageBox.Show("Регистрация успешна! Теперь вы можете войти.");
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                UpdateStatus(false, $"✗ Ошибка ORM: {ex.Message}");
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}