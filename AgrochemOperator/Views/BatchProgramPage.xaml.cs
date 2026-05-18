using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using AgrochemOperator.Models;
using AgrochemOperator.Services;

namespace AgrochemOperator.Views
{
    public partial class BatchProgramPage : Window
    {
        private readonly ApiService _apiService;
        private readonly ProductionBatch _batch;
        private readonly int _userId;
        private ObservableCollection<TechStep> _steps;
        private TechStep _currentStep;
        private BatchStepExecution _currentExecution;
        private DispatcherTimer _telemetryTimer;
        private Random _random = new Random();

        public ObservableCollection<TechStep> Steps
        {
            get => _steps;
            set => _steps = value;
        }

        public BatchProgramPage(ApiService apiService, ProductionBatch batch, int userId)
        {
            InitializeComponent();
            _apiService = apiService;
            _batch = batch;
            _userId = userId;
            Steps = new ObservableCollection<TechStep>();
            listSteps.ItemsSource = Steps;
            lblBatchInfo.Text = $"Партия: {batch.BatchNumber}  |  Продукт: {batch.ProductName ?? "—"}";

            // Инициализация таймера телеметрии
            _telemetryTimer = new DispatcherTimer();
            _telemetryTimer.Interval = TimeSpan.FromSeconds(2);
            _telemetryTimer.Tick += TelemetryTimer_Tick;

            LoadProgram();
        }

        private async void LoadProgram()
        {
            try
            {
                var allSteps = await _apiService.GetBatchSteps(_batch.Id);
                if (allSteps == null || !allSteps.Any())
                {
                    MessageBox.Show("Для этой партии не найдена технологическая карта.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                var executions = await _apiService.GetBatchStepExecutions(_batch.Id);

                Steps.Clear();
                foreach (var step in allSteps.OrderBy(s => s.OrderNum))
                {
                    var exec = executions?.FirstOrDefault(e => e.StepId == step.Id);
                    step.Status = exec?.Status ?? "pending";
                    Steps.Add(step);
                }

                _currentStep = Steps.FirstOrDefault(s => s.Status != "completed");
                if (_currentStep != null)
                {
                    _currentExecution = executions?.FirstOrDefault(e => e.StepId == _currentStep.Id);
                    UpdateCurrentStepUI();
                }
                else
                {
                    MessageBox.Show("Все шаги выполнены! Партия готова.", "Завершение", MessageBoxButton.OK, MessageBoxImage.Information);
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки программы: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void UpdateCurrentStepUI()
        {
            lblCurrentStep.Text = $"{_currentStep.OrderNum}. {_currentStep.StepType}";
            lblStepInstruction.Text = $"Инструкция: {_currentStep.Instructions ?? "—"}";
            lblPlannedParams.Text = $"Плановые параметры: {_currentStep.PlannedParams ?? "не заданы"}";
            txtActualParams.Text = _currentExecution?.ActualParams ?? "";
            btnStartStep.IsEnabled = (_currentExecution == null || _currentExecution.Status != "in_progress");
            btnCompleteStep.IsEnabled = (_currentExecution != null && _currentExecution.Status == "in_progress");

            // Управление панелью телеметрии: показываем только для шага "Экструзия" в процессе выполнения
            if (_currentStep.StepType.ToLower().Contains("экструз") && _currentExecution?.Status == "in_progress")
            {
                TelemetryPanel.Visibility = Visibility.Visible;
                if (!_telemetryTimer.IsEnabled)
                    _telemetryTimer.Start();
            }
            else
            {
                TelemetryPanel.Visibility = Visibility.Collapsed;
                if (_telemetryTimer.IsEnabled)
                    _telemetryTimer.Stop();
            }
        }

        private void TelemetryTimer_Tick(object sender, EventArgs e)
        {
            // Имитация данных экструдера
            double temp = 180 + _random.NextDouble() * 15; // 180-195
            double pressure = 2.5 + _random.NextDouble() * 0.8; // 2.5-3.3
            int speed = 120 + _random.Next(0, 31); // 120-150

            lblTemperature.Text = temp.ToString("F1");
            lblPressure.Text = pressure.ToString("F1");
            lblSpeed.Text = speed.ToString();

            // Проверка отклонений от нормы
            bool tempOk = (temp >= 180 && temp <= 190);
            bool pressOk = (pressure >= 2.5 && pressure <= 3.0);
            bool speedOk = (speed >= 120 && speed <= 150);

            lblTemperature.Foreground = tempOk ? (System.Windows.Media.Brush)FindResource("SuccessColor") : (System.Windows.Media.Brush)FindResource("DangerColor");
            lblPressure.Foreground = pressOk ? (System.Windows.Media.Brush)FindResource("SuccessColor") : (System.Windows.Media.Brush)FindResource("DangerColor");
            lblSpeed.Foreground = speedOk ? (System.Windows.Media.Brush)FindResource("SuccessColor") : (System.Windows.Media.Brush)FindResource("DangerColor");

            string status = tempOk && pressOk && speedOk ? "✅ Все параметры в норме" : "⚠️ Есть отклонения от нормы!";
            lblTelemetryStatus.Text = status;
            lblTelemetryStatus.Foreground = (tempOk && pressOk && speedOk) ? (System.Windows.Media.Brush)FindResource("SuccessColor") : (System.Windows.Media.Brush)FindResource("WarningColor");
        }

        private async void BtnStartStep_Click(object sender, RoutedEventArgs args)
        {
            if (_currentStep == null) return;

            try
            {
                var success = await _apiService.StartStep(_batch.Id, _currentStep.Id, _userId);
                if (success)
                {
                    _currentStep.Status = "in_progress";
                    if (_currentExecution == null)
                    {
                        _currentExecution = new BatchStepExecution
                        {
                            BatchId = _batch.Id,
                            StepId = _currentStep.Id,
                            Status = "in_progress",
                            StartTime = DateTime.Now
                        };
                    }
                    else
                    {
                        _currentExecution.Status = "in_progress";
                        _currentExecution.StartTime = DateTime.Now;
                    }
                    UpdateCurrentStepUI();
                    RefreshStepsList();
                }
                else
                {
                    MessageBox.Show("Не удалось начать шаг. Попробуйте ещё раз.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnCompleteStep_Click(object sender, RoutedEventArgs args)
        {
            MessageBox.Show("Завершение шага вызвано");


            if (_currentExecution == null || _currentExecution.Status != "in_progress")
            {
                MessageBox.Show("Нет активного шага для завершения.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_currentStep.IsMandatory && string.IsNullOrWhiteSpace(txtActualParams.Text))
            {
                MessageBox.Show("Для обязательного шага необходимо ввести фактические параметры.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var success = await _apiService.CompleteStep(_currentExecution.Id, _userId, txtActualParams.Text, null);
                if (success)
                {
                    _currentStep.Status = "completed";
                    _currentExecution.Status = "completed";
                    _currentExecution.EndTime = DateTime.Now;
                    _currentExecution.ActualParams = txtActualParams.Text;

                    var nextStep = Steps.FirstOrDefault(s => s.Status != "completed");
                    if (nextStep != null)
                    {
                        _currentStep = nextStep;
                        var executions = await _apiService.GetBatchStepExecutions(_batch.Id);
                        _currentExecution = executions?.FirstOrDefault(e => e.StepId == _currentStep.Id);
                        UpdateCurrentStepUI();
                    }
                    else
                    {
                        MessageBox.Show("Все шаги выполнены! Партия готова.", "Завершение", MessageBoxButton.OK, MessageBoxImage.Information);
                        Close();
                    }
                    RefreshStepsList();
                }
                else
                {
                    MessageBox.Show("Ошибка при завершении шага.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnDeviation_Click(object sender, RoutedEventArgs args)
        {
            if (_currentExecution == null)
            {
                MessageBox.Show("Сначала начните шаг.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string comment = Microsoft.VisualBasic.Interaction.InputBox("Опишите отклонение:", "Фиксация отклонения", "", -1, -1);
            if (string.IsNullOrWhiteSpace(comment))
                return;

            var deviation = new Deviation
            {
                BatchId = _batch.Id,
                StepId = _currentStep.Id,
                ExecutionId = _currentExecution.Id,
                Parameter = "Общее отклонение",
                PlannedValue = _currentStep.PlannedParams,
                ActualValue = txtActualParams.Text,
                Severity = "warning",
                Comment = comment,
                UserId = _userId
            };

            try
            {
                var success = await _apiService.ReportDeviation(deviation);
                if (success)
                    MessageBox.Show("Отклонение зафиксировано.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show("Не удалось зафиксировать отклонение.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshStepsList()
        {
            listSteps.Items.Refresh();
        }
    }
}