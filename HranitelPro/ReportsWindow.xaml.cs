using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Npgsql;

namespace HranitelPro
{
    public partial class ReportsWindow : Window
    {
        private string connectionString = "Host=localhost;Port=5432;Database=hranitelpro;Username=postgres;Password=1;";

        public ReportsWindow()
        {
            InitializeComponent();
            ReportDatePicker.SelectedDate = DateTime.Now;
        }

        // ========== ОТЧЁТ ЗА ВЫБРАННЫЙ ПЕРИОД (1.1) ==========
        private void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string period = (PeriodCombo.SelectedItem as ComboBoxItem)?.Content.ToString();
                DateTime? selectedDate = ReportDatePicker.SelectedDate;
                string groupBy = (GroupByCombo.SelectedItem as ComboBoxItem)?.Content.ToString();

                if (!selectedDate.HasValue)
                {
                    MessageBox.Show("Выберите дату для формирования отчёта", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                List<ReportItem> reportData = new List<ReportItem>();
                int totalCount = 0;

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string dateCondition = "";
                    if (period == "День")
                    {
                        dateCondition = $"DATE(r.created_at) = '{selectedDate.Value:yyyy-MM-dd}'";
                    }
                    else if (period == "Месяц")
                    {
                        dateCondition = $"EXTRACT(YEAR FROM r.created_at) = {selectedDate.Value.Year} AND EXTRACT(MONTH FROM r.created_at) = {selectedDate.Value.Month}";
                    }
                    else // Год
                    {
                        dateCondition = $"EXTRACT(YEAR FROM r.created_at) = {selectedDate.Value.Year}";
                    }

                    string groupField = "";
                    if (groupBy == "По подразделениям")
                    {
                        groupField = "COALESCE(r.department, 'Не указано')";
                    }
                    else if (groupBy == "По статусам")
                    {
                        groupField = "r.status";
                    }
                    else // По типам заявок
                    {
                        groupField = "CASE WHEN r.group_id > 0 THEN 'Групповая' ELSE 'Личная' END";
                    }

                    string query = $@"
                        SELECT 
                            {groupField} as group_name,
                            COUNT(*) as count
                        FROM requests r
                        WHERE {dateCondition}
                        GROUP BY {groupField}
                        ORDER BY count DESC";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string groupName = reader.GetString(0);
                                int count = reader.GetInt32(1);
                                reportData.Add(new ReportItem
                                {
                                    GroupName = groupName,
                                    Count = count,
                                    Percent = 0
                                });
                                totalCount += count;
                            }
                        }
                    }
                }

                foreach (var item in reportData)
                {
                    if (totalCount > 0)
                        item.Percent = Math.Round((double)item.Count / totalCount * 100, 2);
                }

                ReportGrid.ItemsSource = reportData;
                TotalText.Text = $"Всего посещений: {totalCount}";
                ExportBtn.IsEnabled = reportData.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка формирования отчёта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ========== ЭКСПОРТ В HTML (ВМЕСТО PDF) ==========
        private void ExportToPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var reportData = ReportGrid.ItemsSource as List<ReportItem>;
                if (reportData == null || reportData.Count == 0)
                {
                    MessageBox.Show("Нет данных для экспорта. Сначала сформируйте отчёт.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Формируем путь к папке
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string reportsFolder = Path.Combine(documentsPath, "Отчеты ТБ");
                if (!Directory.Exists(reportsFolder))
                    Directory.CreateDirectory(reportsFolder);

                string todayFolder = Path.Combine(reportsFolder, DateTime.Now.ToString("dd_MM_yyyy"));
                if (!Directory.Exists(todayFolder))
                    Directory.CreateDirectory(todayFolder);

                // Генерируем HTML
                string html = GenerateExportHtml(reportData);
                string fileName = $"report_{DateTime.Now:HH_mm_ss}.html";
                string filePath = Path.Combine(todayFolder, fileName);

                File.WriteAllText(filePath, html, Encoding.UTF8);

                MessageBox.Show($"Отчёт сохранён в формате HTML:\n{filePath}\n\nОткройте файл в браузере и сохраните как PDF", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateExportHtml(List<ReportItem> data)
        {
            StringBuilder html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='UTF-8'>");
            html.AppendLine("<title>Отчёт о посещениях</title>");
            html.AppendLine(@"
                <style>
                    body { font-family: 'Segoe UI', Arial, sans-serif; margin: 40px; padding: 20px; }
                    h1 { color: #FF9800; border-bottom: 3px solid #FF9800; padding-bottom: 10px; text-align: center; }
                    .header { text-align: center; margin-bottom: 30px; }
                    .period { font-size: 14px; color: #555; margin: 5px 0; text-align: center; }
                    table { width: 100%; border-collapse: collapse; margin-top: 25px; }
                    th, td { border: 1px solid #ddd; padding: 12px; text-align: left; }
                    th { background-color: #FF9800; color: white; font-weight: bold; }
                    tr:nth-child(even) { background-color: #f9f9f9; }
                    .total { font-weight: bold; background-color: #e0e0e0; }
                    .footer { margin-top: 30px; text-align: center; font-size: 10px; color: #999; border-top: 1px solid #ddd; padding-top: 15px; }
                </style>
            ");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            html.AppendLine($"<h1>📊 Отчёт о количестве посетителей</h1>");
            html.AppendLine($"<div class='period'><strong>Дата формирования:</strong> {DateTime.Now:dd.MM.yyyy HH:mm:ss}</div>");

            html.AppendLine("<table>");
            html.AppendLine(" <thead><tr><th>Группа</th><th>Количество посещений</th><th>Доля (%)</th></thead> ");
            html.AppendLine("<tbody>");

            int total = 0;
            foreach (var item in data)
            {
                html.AppendLine($"<tr><td>{item.GroupName}</td><td style='text-align: center;'>{item.Count}</td><td style='text-align: center;'>{item.Percent}%</td></tr>");
                total += item.Count;
            }

            html.AppendLine($"<tr class='total'><td><strong>ИТОГО:</strong></td><td style='text-align: center;'><strong>{total}</strong></td><td style='text-align: center;'><strong>100%</strong></td></tr>");
            html.AppendLine("</tbody>");
            html.AppendLine("</table>");

            html.AppendLine($"<div class='footer'>Сформировано системой &quot;ХранительПРО&quot;<br/>");
            html.AppendLine($"Файл сгенерирован: {DateTime.Now:dd.MM.yyyy HH:mm:ss}</div>");
            html.AppendLine("</body></html>");

            return html.ToString();
        }

        // ========== ОТЧЁТ ЗА 3 ЧАСА (ЗАДАНИЕ 2) - СОХРАНЕНИЕ В HTML ==========
        private void GenerateThreeHourReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime now = DateTime.Now;
                int hourBlock = (now.Hour / 3) * 3;
                DateTime startTime = new DateTime(now.Year, now.Month, now.Day, hourBlock, 0, 0);
                DateTime endTime = startTime.AddHours(3);

                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string reportsFolder = Path.Combine(documentsPath, "Отчеты ТБ");
                if (!Directory.Exists(reportsFolder))
                    Directory.CreateDirectory(reportsFolder);

                string todayFolder = Path.Combine(reportsFolder, DateTime.Now.ToString("dd_MM_yyyy"));
                if (!Directory.Exists(todayFolder))
                    Directory.CreateDirectory(todayFolder);

                var reportData = new List<ReportDepartmentItem>();

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT 
                            COALESCE(r.department, 'Не указано') as department,
                            COUNT(*) as visit_count
                        FROM requests r
                        WHERE r.status = 'Одобрена'
                        AND r.entry_time BETWEEN @startTime AND @endTime
                        GROUP BY r.department
                        ORDER BY visit_count DESC";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@startTime", startTime);
                        cmd.Parameters.AddWithValue("@endTime", endTime);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                reportData.Add(new ReportDepartmentItem
                                {
                                    Department = reader.GetString(0),
                                    Count = reader.GetInt32(1)
                                });
                            }
                        }
                    }
                }

                string html = GenerateThreeHourReportHtml(reportData, startTime, endTime);
                string fileName = $"report_3hours_{startTime:HH_mm}_{endTime:HH_mm}.html";
                string filePath = Path.Combine(todayFolder, fileName);

                File.WriteAllText(filePath, html, Encoding.UTF8);

                MessageBox.Show($"Отчёт за 3 часа успешно сохранён!\n\n" +
                    $"Период: {startTime:HH:mm} - {endTime:HH:mm}\n" +
                    $"Путь: {filePath}\n\n" +
                    $"Откройте файл в браузере и сохраните как PDF", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка формирования отчёта за 3 часа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateThreeHourReportHtml(List<ReportDepartmentItem> data, DateTime startTime, DateTime endTime)
        {
            StringBuilder html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='UTF-8'>");
            html.AppendLine("<title>Отчёт о посещениях за 3 часа</title>");
            html.AppendLine(@"
                <style>
                    body { font-family: 'Segoe UI', Arial, sans-serif; margin: 40px; padding: 20px; }
                    h1 { color: #FF9800; border-bottom: 3px solid #FF9800; padding-bottom: 10px; text-align: center; }
                    .header { text-align: center; margin-bottom: 30px; }
                    .period { font-size: 14px; color: #555; margin: 5px 0; text-align: center; }
                    table { width: 100%; border-collapse: collapse; margin-top: 25px; }
                    th, td { border: 1px solid #ddd; padding: 12px; text-align: left; }
                    th { background-color: #FF9800; color: white; font-weight: bold; }
                    tr:nth-child(even) { background-color: #f9f9f9; }
                    .total { font-weight: bold; background-color: #e0e0e0; }
                    .footer { margin-top: 30px; text-align: center; font-size: 10px; color: #999; border-top: 1px solid #ddd; padding-top: 15px; }
                </style>
            ");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            html.AppendLine($"<h1>📊 Отчёт о количестве посетителей за 3 часа</h1>");
            html.AppendLine($"<div class='period'><strong>Период:</strong> {startTime:dd.MM.yyyy HH:mm} - {endTime:HH:mm}</div>");
            html.AppendLine($"<div class='period'><strong>Дата формирования:</strong> {DateTime.Now:dd.MM.yyyy HH:mm:ss}</div>");

            html.AppendLine("</table>");
            html.AppendLine(" <thead><tr><th>Подразделение</th><th>Количество посетителей</th></thead> ");
            html.AppendLine("<tbody>");

            int total = 0;
            foreach (var item in data)
            {
                html.AppendLine($"<tr><td>{item.Department}</td><td style='text-align: center;'>{item.Count}</td></tr>");
                total += item.Count;
            }

            html.AppendLine($"<tr class='total'><td><strong>ИТОГО:</strong></td><td style='text-align: center;'><strong>{total}</strong></td></tr>");
            html.AppendLine("</tbody>");
            html.AppendLine("</table>");

            html.AppendLine($"<div class='footer'>Сформировано автоматически системой &quot;ХранительПРО&quot;<br/>");
            html.AppendLine($"Файл сгенерирован: {DateTime.Now:dd.MM.yyyy HH:mm:ss}</div>");
            html.AppendLine("</body></html>");

            return html.ToString();
        }
    }

    public class ReportItem
    {
        public string GroupName { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percent { get; set; }
    }

    public class ReportDepartmentItem
    {
        public string Department { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}