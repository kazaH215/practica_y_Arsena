using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using AgrochemLaboratory.Models;
using AgrochemLaboratory.Services;
using Microsoft.Win32;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace AgrochemLaboratory.ViewModels
{
    public class ReportsViewModel : INotifyPropertyChanged
    {
        private readonly ApiService _apiService;
        private DateTime _startDate;
        private DateTime _endDate;
        private string _selectedReportType;
        private bool _isLoading;

        public DateTime StartDate
        {
            get => _startDate;
            set { _startDate = value; OnPropertyChanged(); }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set { _endDate = value; OnPropertyChanged(); }
        }

        public string SelectedReportType
        {
            get => _selectedReportType;
            set { _selectedReportType = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> ReportTypes { get; } = new ObservableCollection<string>
        {
            "Отчёт по партиям за период",
            "Отчёт по отклонениям",
            "Отчёт по использованию рецептур",
            "Отчёт по лабораторным блокировкам"
        };

        public ICommand GenerateExcelCommand { get; }
        public ICommand GeneratePdfCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ReportsViewModel(string token)
        {
            _apiService = new ApiService();
            _apiService.SetToken(token);
            StartDate = DateTime.Now.AddMonths(-1);
            EndDate = DateTime.Now;
            SelectedReportType = ReportTypes[0];

            GenerateExcelCommand = new RelayCommand(async _ => await GenerateExcel());
            GeneratePdfCommand = new RelayCommand(async _ => await GeneratePdf());
        }

        private async Task GenerateExcel()
        {
            if (string.IsNullOrEmpty(SelectedReportType))
            {
                System.Windows.MessageBox.Show("Выберите тип отчёта", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;
            try
            {
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Отчёт");
                    worksheet.Cells["A1"].Value = SelectedReportType;
                    worksheet.Cells["A1"].Style.Font.Size = 16;
                    worksheet.Cells["A1"].Style.Font.Bold = true;
                    worksheet.Cells["A2"].Value = $"Период: {StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy}";
                    worksheet.Cells["A2"].Style.Font.Size = 12;

                    int currentRow = 4;

                    switch (SelectedReportType)
                    {
                        case "Отчёт по партиям за период":
                            currentRow = await FillBatchesReport(worksheet, currentRow);
                            break;
                        case "Отчёт по отклонениям":
                            currentRow = await FillDeviationsReport(worksheet, currentRow);
                            break;
                        case "Отчёт по использованию рецептур":
                            currentRow = await FillRecipesUsageReport(worksheet, currentRow);
                            break;
                        case "Отчёт по лабораторным блокировкам":
                            currentRow = await FillLabBlockingsReport(worksheet, currentRow);
                            break;
                    }

                    worksheet.Cells.AutoFitColumns();

                    var saveDialog = new SaveFileDialog
                    {
                        Filter = "Excel files (*.xlsx)|*.xlsx",
                        FileName = $"{SelectedReportType}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                    };
                    if (saveDialog.ShowDialog() == true)
                    {
                        // Синхронная запись файла (для совместимости с .NET Framework)
                        File.WriteAllBytes(saveDialog.FileName, package.GetAsByteArray());
                        System.Windows.MessageBox.Show("Отчёт сохранён!", "Успех", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task GeneratePdf()
        {
            await Task.Run(() =>
            {
                System.Windows.MessageBox.Show("PDF-отчёты будут добавлены в следующей версии", "Информация", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            });
        }

        private async Task<int> FillBatchesReport(ExcelWorksheet ws, int startRow)
        {
            int row = startRow;
            // Заголовки
            ws.Cells[row, 1].Value = "ID";
            ws.Cells[row, 2].Value = "Номер партии";
            ws.Cells[row, 3].Value = "Продукт";
            ws.Cells[row, 4].Value = "Статус";
            ws.Cells[row, 5].Value = "Дата начала";
            ws.Cells[row, 6].Value = "Дата окончания";
            ws.Cells[row, 7].Value = "Факт. кол-во, кг";
            using (var range = ws.Cells[row, 1, row, 7])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                // Убираем цвет фона, чтобы избежать ошибки с System.Drawing
                // range.Style.Fill.BackgroundColor.SetColor(211, 211, 211);
            }
            row++;

            var batches = new System.Collections.Generic.List<ProductionBatchModel>();
            try
            {
                var result = await _apiService.GetAsync<ApiResponse<List<ProductionBatchModel>>>("/api/ProductionBatches");
                if (result?.success == true && result.data != null)
                    batches = result.data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Не удалось загрузить партии: {ex.Message}");
                ws.Cells[row, 1].Value = "Данные не загружены. Требуется доработка API.";
                row++;
                return row;
            }

            var filtered = batches.Where(b => b.Date.HasValue && b.Date.Value.Date >= StartDate.Date && b.Date.Value.Date <= EndDate.Date).ToList();

            if (!filtered.Any())
            {
                ws.Cells[row, 1].Value = "Нет данных за выбранный период";
                row++;
                return row;
            }

            foreach (var b in filtered)
            {
                ws.Cells[row, 1].Value = b.Id;
                ws.Cells[row, 2].Value = b.Number;
                ws.Cells[row, 3].Value = b.Name;
                ws.Cells[row, 4].Value = b.CurrentStatus;
                ws.Cells[row, 5].Value = b.Date?.ToString("dd.MM.yyyy HH:mm");
                ws.Cells[row, 6].Value = b.Date?.ToString("dd.MM.yyyy HH:mm");
                ws.Cells[row, 7].Value = b.Quantity;
                row++;
            }
            return row;
        }

        private async Task<int> FillDeviationsReport(ExcelWorksheet ws, int startRow)
        {
            int row = startRow;
            ws.Cells[row, 1].Value = "ID";
            ws.Cells[row, 2].Value = "Партия";
            ws.Cells[row, 3].Value = "Параметр";
            ws.Cells[row, 4].Value = "План";
            ws.Cells[row, 5].Value = "Факт";
            ws.Cells[row, 6].Value = "Уровень";
            ws.Cells[row, 7].Value = "Комментарий";
            ws.Cells[row, 8].Value = "Дата";
            using (var range = ws.Cells[row, 1, row, 8])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            }
            row++;

            var deviations = new System.Collections.Generic.List<DeviationModel>();
            try
            {
                var result = await _apiService.GetAsync<ApiResponse<List<DeviationModel>>>("/api/Deviations");
                if (result?.success == true && result.data != null)
                    deviations = result.data;
            }
            catch { }

            var filtered = deviations.Where(d => d.CreatedAt.HasValue && d.CreatedAt.Value.Date >= StartDate.Date && d.CreatedAt.Value.Date <= EndDate.Date).ToList();

            if (!filtered.Any())
            {
                ws.Cells[row, 1].Value = "Нет данных за выбранный период";
                row++;
                return row;
            }

            foreach (var d in filtered)
            {
                ws.Cells[row, 1].Value = d.Id;
                ws.Cells[row, 2].Value = d.BatchNumber;
                ws.Cells[row, 3].Value = d.Parameter;
                ws.Cells[row, 4].Value = d.PlannedValue;
                ws.Cells[row, 5].Value = d.ActualValue;
                ws.Cells[row, 6].Value = d.Severity;
                ws.Cells[row, 7].Value = d.Comment;
                ws.Cells[row, 8].Value = d.CreatedAt?.ToString("dd.MM.yyyy HH:mm");
                row++;
            }
            return row;
        }

        private async Task<int> FillRecipesUsageReport(ExcelWorksheet ws, int startRow)
        {
            int row = startRow;
            ws.Cells[row, 1].Value = "Рецептура ID";
            ws.Cells[row, 2].Value = "Продукт";
            ws.Cells[row, 3].Value = "Версия";
            ws.Cells[row, 4].Value = "Статус";
            ws.Cells[row, 5].Value = "Кол-во партий";
            ws.Cells[row, 6].Value = "Общий объём, кг";
            using (var range = ws.Cells[row, 1, row, 6])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            }
            row++;

            ws.Cells[row, 1].Value = "Для формирования отчёта требуется доработка API (GET /api/Recipes/usage)";
            row++;
            return row;
        }

        private async Task<int> FillLabBlockingsReport(ExcelWorksheet ws, int startRow)
        {
            int row = startRow;
            ws.Cells[row, 1].Value = "Партия";
            ws.Cells[row, 2].Value = "Продукт";
            ws.Cells[row, 3].Value = "Дата блокировки";
            ws.Cells[row, 4].Value = "Причина";
            ws.Cells[row, 5].Value = "Лаборант";
            using (var range = ws.Cells[row, 1, row, 5])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            }
            row++;

            var tests = new System.Collections.Generic.List<LabTestModel>();
            try
            {
                var result = await _apiService.GetAsync<ApiResponse<List<LabTestModel>>>("/api/LabTests");
                if (result?.success == true && result.data != null)
                    tests = result.data;
            }
            catch { }

            var blocked = tests.Where(t => t.Result == "rejected" && DateTime.TryParse(t.CreatedAt, out DateTime dt) && dt.Date >= StartDate.Date && dt.Date <= EndDate.Date).ToList();

            if (!blocked.Any())
            {
                ws.Cells[row, 1].Value = "Нет блокировок за выбранный период";
                row++;
                return row;
            }

            foreach (var t in blocked)
            {
                ws.Cells[row, 1].Value = t.ObjectNumber;
                ws.Cells[row, 2].Value = t.ObjectName;
                ws.Cells[row, 3].Value = t.CreatedAt;
                ws.Cells[row, 4].Value = t.Comment;
                ws.Cells[row, 5].Value = t.AuthorName;
                row++;
            }
            return row;
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}