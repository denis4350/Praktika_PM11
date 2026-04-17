using Npgsql;
using System;
using System.Windows;
using System.Windows.Controls;

namespace HranitelPro
{
    public partial class RequestReviewWindow : Window
    {
        private string connectionString = "Host=localhost;Port=5432;Database=hranitelpro;Username=postgres;Password=1;";
        private EmployeeRequestItem request;
        private int employeeId;
        private bool isInBlacklist = false;

        public RequestReviewWindow(EmployeeRequestItem selectedRequest, int empId)
        {
            InitializeComponent();
            request = selectedRequest;
            employeeId = empId;

            LoadRequestData();
            CheckBlacklist();
        }

        private void LoadRequestData()
        {
            // Информация о заявителе (с проверкой на null)
            FullNameText.Text = request.VisitorName ?? "Не указано";
            BirthDateText.Text = "Не указана";
            PhoneText.Text = "Не указан";
            EmailText.Text = "Не указан";
            PassportText.Text = request.Passport ?? "Не указан";
            OrganizationText.Text = "Не указана";
            NoteText.Text = "Нет";

            // Информация о посещении (с проверкой на null)
            RequestTypeText.Text = request.RequestType ?? "Не указан";
            PurposeText.Text = request.Purpose ?? "Не указана";
            DepartmentText.Text = request.Department ?? "Не указано";
            EmployeeText.Text = request.Employee ?? "Не указан";

            // Устанавливаем дату посещения
            if (!string.IsNullOrEmpty(request.StartDate) && DateTime.TryParse(request.StartDate, out DateTime startDate))
            {
                VisitDatePicker.SelectedDate = startDate;
            }

            // Устанавливаем текущий статус
            if (!string.IsNullOrEmpty(request.Status))
            {
                foreach (ComboBoxItem item in StatusCombo.Items)
                {
                    if (item.Content.ToString() == request.Status)
                    {
                        StatusCombo.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void CheckBlacklist()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT COUNT(*) FROM blacklist 
                        WHERE (last_name = @lastName AND first_name = @firstName)
                           OR (passport_series = @passportSeries AND passport_number = @passportNumber)";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@lastName", request.LastName ?? "");
                        cmd.Parameters.AddWithValue("@firstName", request.FirstName ?? "");
                        cmd.Parameters.AddWithValue("@passportSeries", request.PassportSeries ?? "");
                        cmd.Parameters.AddWithValue("@passportNumber", request.PassportNumber ?? "");

                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        isInBlacklist = count > 0;

                        if (isInBlacklist)
                        {
                            BlacklistWarning.Visibility = Visibility.Visible;
                            StatusCombo.IsEnabled = false;
                            SaveButton.IsEnabled = false;

                            // Автоматически отклоняем заявку
                            UpdateRequestStatus("Отклонена", "Посетитель находится в чёрном списке (нарушение ФЗ №187-ФЗ)");
                            StatusCombo.SelectedIndex = 2; // Отклонена
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка проверки чёрного списка: {ex.Message}");
            }
        }

        private void UpdateRequestStatus(string status, string comment)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        UPDATE requests 
                        SET status = @status, 
                            review_comment = @comment,
                            reviewed_by = @employeeId,
                            reviewed_at = @reviewedAt
                        WHERE id = @requestId";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@status", status ?? "На проверке");
                        cmd.Parameters.AddWithValue("@comment", comment ?? "");
                        cmd.Parameters.AddWithValue("@employeeId", employeeId);
                        cmd.Parameters.AddWithValue("@reviewedAt", DateTime.Now);
                        cmd.Parameters.AddWithValue("@requestId", request.RequestId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления статуса: {ex.Message}");
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // ОТЛАДКА: проверяем что пришло
            MessageBox.Show($"DEBUG: RequestId = {request?.RequestId}, EmployeeId = {employeeId}, CurrentStatus = {request?.Status}");

            if (isInBlacklist)
            {
                MessageBox.Show("Невозможно изменить статус: посетитель находится в чёрном списке",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string newStatus = (StatusCombo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "На проверке";

            // ОТЛАДКА: какой статус выбран
            MessageBox.Show($"DEBUG: Selected newStatus = {newStatus}");

            string comment = "";

            if (newStatus == "Отклонена")
            {
                string reason = Microsoft.VisualBasic.Interaction.InputBox(
                    "Введите причину отклонения заявки:",
                    "Причина отклонения",
                    "Недостоверные данные");
                comment = $"Заявка отклонена. Причина: {reason}";
            }
            else if (newStatus == "Одобрена")
            {
                string visitDate = VisitDatePicker.SelectedDate?.ToString("dd.MM.yyyy") ?? "не указана";
                string visitTime = VisitTimeBox.Text;
                comment = $"Заявка одобрена. Дата посещения: {visitDate}, время: {visitTime}";
            }
            else
            {
                comment = "Заявка оставлена на проверке";
            }

            UpdateRequestStatus(newStatus, comment);

            MessageBox.Show($"Статус заявки #{request.RequestId} изменён на '{newStatus}'",
                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            this.Close();
        }
       

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}