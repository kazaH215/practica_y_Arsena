using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using AgrochemLaboratory.Models;
using AgrochemLaboratory.Services;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.Win32;

namespace AgrochemLaboratory.Views
{
    public partial class LabTestPage : Window
    {
        private readonly ApiService _apiService;
        private readonly int _testId;
        private readonly int _userId;
        private readonly string _objectType;
        private readonly int _objectId;
        private LabTestModel _test;

        public LabTestPage(ApiService apiService, int testId, int userId, string objectType, int objectId)
        {
            InitializeComponent();
            _apiService = apiService;
            _testId = testId;
            _userId = userId;
            _objectType = objectType;
            _objectId = objectId;

            LoadTest();
        }

        private async void LoadTest()
        {
            try
            {
                _test = await _apiService.GetLabTest(_testId);
                if (_test == null)
                {
                    MessageBox.Show("Испытание не найдено", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                string objectTitle = "";
                if (_objectType == "raw_material")
                {
                    var materials = await _apiService.GetPendingRawMaterials();
                    var material = materials.FirstOrDefault(m => m.Id == _objectId);
                    if (material != null)
                        objectTitle = $"{material.Name}, партия {material.Number}";
                }
                else
                {
                    var batches = await _apiService.GetPendingBatches();
                    var batch = batches.FirstOrDefault(b => b.Id == _objectId);
                    if (batch != null)
                        objectTitle = $"{batch.Name}, партия {batch.Number}";
                }

                lblObjectInfo.Text = $"📦 {objectTitle}";
                lblTestStatus.Text = $"Статус: {_test.Status}";

                if (_test.Status == "completed" || !string.IsNullOrEmpty(_test.Result))
                {
                    btnSaveResults.IsEnabled = false;
                    btnApprove.IsEnabled = false;
                    btnReject.IsEnabled = false;
                }

                // Если есть параметры, заполняем поля (опционально)
                if (_test.Parameters != null && _test.Parameters.Count >= 3)
                {
                    // Можно заполнить txtParam1, txtParam2, txtParam3, но пока пропускаем
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtParam_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateParameterResult(txtParam2, lblResult2, 6.5m, 7.5m);
            UpdateParameterResult(txtParam3, lblResult3, 1.05m, 1.15m);
            if (!string.IsNullOrEmpty(txtParam1.Text))
            {
                bool passed = txtParam1.Text.Trim().ToLower() == "прозрачный";
                lblResult1.Text = passed ? "✅" : "❌";
                lblResult1.Foreground = passed ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
            }
            else
            {
                lblResult1.Text = "❓";
            }
        }

        private void UpdateParameterResult(System.Windows.Controls.TextBox textBox, System.Windows.Controls.TextBlock resultLabel, decimal min, decimal max)
        {
            if (decimal.TryParse(textBox.Text, out decimal val))
            {
                bool passed = val >= min && val <= max;
                resultLabel.Text = passed ? "✅" : "❌";
                resultLabel.Foreground = passed ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
            }
            else
            {
                resultLabel.Text = "❓";
                resultLabel.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private async void BtnSaveResults_Click(object sender, RoutedEventArgs e)
        {
            var parameters = new List<LabTestParameterDto>();

            if (decimal.TryParse(txtParam2.Text, out decimal ph))
            {
                parameters.Add(new LabTestParameterDto
                {
                    Id = 2,
                    ParameterName = "pH",
                    NormMin = 6.5m,
                    NormMax = 7.5m,
                    ActualValue = ph,
                    IsPassed = lblResult2.Text == "✅"
                });
            }

            if (decimal.TryParse(txtParam3.Text, out decimal density))
            {
                parameters.Add(new LabTestParameterDto
                {
                    Id = 3,
                    ParameterName = "Плотность",
                    NormMin = 1.05m,
                    NormMax = 1.15m,
                    ActualValue = density,
                    IsPassed = lblResult3.Text == "✅"
                });
            }

            var resultDto = new EnterLabResultDto
            {
                TestId = _testId,
                UserId = _userId,
                Parameters = parameters,
                Comment = $"Внешний вид: {txtParam1.Text}\n" + txtComment.Text
            };

            var success = await _apiService.EnterLabResults(resultDto);
            if (success)
            {
                MessageBox.Show("Результаты сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                btnSaveResults.IsEnabled = false;
                lblTestStatus.Text = "Статус: completed";
            }
            else
            {
                MessageBox.Show("Ошибка сохранения результатов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnApprove_Click(object sender, RoutedEventArgs e)
        {
            if (btnSaveResults.IsEnabled)
            {
                MessageBox.Show("Сначала сохраните результаты", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("Подтвердить решение: Одобрить партию?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            var decisionDto = new LabDecisionDto
            {
                TestId = _testId,
                UserId = _userId,
                Result = "approved",
                Comment = txtComment.Text
            };

            var success = await _apiService.MakeDecision(decisionDto);
            if (success)
            {
                MessageBox.Show("Партия одобрена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Ошибка при принятии решения", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnReject_Click(object sender, RoutedEventArgs e)
        {
            if (btnSaveResults.IsEnabled)
            {
                MessageBox.Show("Сначала сохраните результаты", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtComment.Text) || txtComment.Text == "Введите комментарий (при блокировке обязательно)")
            {
                MessageBox.Show("При блокировке партии необходимо указать причину!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("Подтвердить решение: Заблокировать партию?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            var decisionDto = new LabDecisionDto
            {
                TestId = _testId,
                UserId = _userId,
                Result = "rejected",
                Comment = txtComment.Text
            };

            var success = await _apiService.MakeDecision(decisionDto);
            if (success)
            {
                MessageBox.Show("Партия заблокирована!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Ошибка при принятии решения", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Метод генерации отчёта (теперь public, чтобы XAML его видел)
        public async void BtnGenerateReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var test = await _apiService.GetLabTest(_testId);
                if (test == null)
                {
                    MessageBox.Show("Не удалось загрузить данные испытания", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string objectTitle = test.ObjectName ?? "Объект";
                string batchNumber = test.ObjectNumber ?? "Н/Д";

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    FileName = $"Протокол_испытания_{test.Id}_{batchNumber}.pdf",
                    Title = "Сохранить протокол испытания"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (FileStream fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                    {
                        PdfWriter writer = new PdfWriter(fileStream);
                        PdfDocument pdf = new PdfDocument(writer);
                        Document document = new Document(pdf, iText.Kernel.Geom.PageSize.A4);

                        document.Add(new Paragraph("ПРОТОКОЛ ЛАБОРАТОРНОГО ИСПЫТАНИЯ")
                            .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                            .SetFontSize(18)
                            .SetBold());

                        document.Add(new Paragraph($"Номер испытания: {test.Id}").SetMarginTop(10));
                        document.Add(new Paragraph($"Объект контроля: {objectTitle}"));
                        document.Add(new Paragraph($"Партия: {batchNumber}"));
                        document.Add(new Paragraph($"Тип испытания: {test.TestType ?? "Контроль качества"}"));
                        document.Add(new Paragraph($"Дата проведения: {test.CreatedAt}"));
                        document.Add(new Paragraph($"Лаборант: {test.AuthorName ?? "—"}"));
                        document.Add(new Paragraph("\n"));

                        document.Add(new Paragraph("РЕЗУЛЬТАТЫ ИЗМЕРЕНИЙ").SetBold().SetFontSize(14));

                        iText.Layout.Element.Table table = new iText.Layout.Element.Table(4);
                        table.AddHeaderCell("Параметр");
                        table.AddHeaderCell("Норма");
                        table.AddHeaderCell("Факт");
                        table.AddHeaderCell("Результат");

                        if (test.Parameters != null && test.Parameters.Count > 0)
                        {
                            foreach (var p in test.Parameters)
                            {
                                string norm = (p.NormMin.HasValue || p.NormMax.HasValue) ? $"{p.NormMin} – {p.NormMax}" : "—";
                                string fact = p.ActualValue?.ToString() ?? "—";
                                string result = (p.IsPassed == true) ? "✅ Соответствует" : (p.IsPassed == false) ? "❌ Не соответствует" : "—";
                                table.AddCell(p.ParameterName);
                                table.AddCell(norm);
                                table.AddCell(fact);
                                table.AddCell(result);
                            }
                        }
                        else
                        {
                            // Запасной вариант: берём значения из текстовых полей
                            table.AddCell("Внешний вид");
                            table.AddCell("прозрачный");
                            table.AddCell(txtParam1.Text);
                            table.AddCell(lblResult1.Text == "✅" ? "✅ Соответствует" : "❌ Не соответствует");

                            table.AddCell("pH");
                            table.AddCell("6.5 – 7.5");
                            table.AddCell(txtParam2.Text);
                            table.AddCell(lblResult2.Text == "✅" ? "✅ Соответствует" : "❌ Не соответствует");

                            table.AddCell("Плотность, г/см³");
                            table.AddCell("1.05 – 1.15");
                            table.AddCell(txtParam3.Text);
                            table.AddCell(lblResult3.Text == "✅" ? "✅ Соответствует" : "❌ Не соответствует");
                        }

                        document.Add(table);
                        document.Add(new Paragraph("\n"));
                        document.Add(new Paragraph("ЗАКЛЮЧЕНИЕ:").SetBold());
                        string conclusion = test.Result == "approved" ? "Партия соответствует установленным требованиям. Допущена к использованию." :
                                            test.Result == "rejected" ? "Партия не соответствует требованиям. Заблокирована." :
                                            "Решение не принято.";
                        document.Add(new Paragraph(conclusion));

                        if (!string.IsNullOrEmpty(test.Comment))
                        {
                            document.Add(new Paragraph("\n"));
                            document.Add(new Paragraph("КОММЕНТАРИЙ:").SetBold());
                            document.Add(new Paragraph(test.Comment));
                        }

                        document.Close();
                    }

                    MessageBox.Show("Протокол успешно сохранён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании PDF: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}