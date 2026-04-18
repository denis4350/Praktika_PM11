using System;
using System.Windows;
using Npgsql;

namespace HranitelPro.Department
{
    public partial class BlacklistReasonWindow : Window
    {
        private string connectionString = "Host=localhost;Port=5432;Database=hranitelpro;Username=postgres;Password=1;";
        private string lastName, firstName, middleName, passportSeries, passportNumber;

        public BlacklistReasonWindow(string last, string first, string middle, string passSer, string passNum)
        {
            InitializeComponent();
            lastName = last;
            firstName = first;
            middleName = middle;
            passportSeries = passSer;
            passportNumber = passNum;

            VisitorNameText.Text = $"{lastName} {firstName} {middleName}".Trim();
            PassportText.Text = $"{passportSeries} {passportNumber}";
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            string reason = ReasonBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show("Введите причину добавления в чёрный список");
                return;
            }

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        INSERT INTO blacklist (last_name, first_name, middle_name, passport_series, passport_number, reason, created_at)
                        VALUES (@lastName, @firstName, @middleName, @passportSeries, @passportNumber, @reason, @createdAt)";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@lastName", lastName);
                        cmd.Parameters.AddWithValue("@firstName", firstName);
                        cmd.Parameters.AddWithValue("@middleName", string.IsNullOrEmpty(middleName) ? "" : middleName);
                        cmd.Parameters.AddWithValue("@passportSeries", passportSeries);
                        cmd.Parameters.AddWithValue("@passportNumber", passportNumber);
                        cmd.Parameters.AddWithValue("@reason", reason);
                        cmd.Parameters.AddWithValue("@createdAt", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Посетитель добавлен в чёрный список!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}