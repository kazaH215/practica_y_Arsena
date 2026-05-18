using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using AgrochemLaboratory.Models;
using AgrochemLaboratory.Services;
using AgrochemLaboratory.Views;

namespace AgrochemLaboratory.ViewModels
{
    public class ProductionBatchesViewModel : INotifyPropertyChanged
    {
        private readonly ApiService _apiService;
        private readonly int _currentUserId;
        private ObservableCollection<ProductionBatchModel> _batches;
        private ProductionBatchModel _selectedBatch;
        private bool _isLoading;
        private string _searchText;

        public ObservableCollection<ProductionBatchModel> Batches
        {
            get => _batches;
            set { _batches = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasNoData)); }
        }

        public ProductionBatchModel SelectedBatch
        {
            get => _selectedBatch;
            set { _selectedBatch = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasNoData)); }
        }

        public bool HasNoData => !IsLoading && (Batches == null || Batches.Count == 0);

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); Task.Run(() => LoadBatches()); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand StartTestCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ProductionBatchesViewModel(string token, int userId)
        {
            _apiService = new ApiService();
            _apiService.SetToken(token);
            _currentUserId = userId;
            Batches = new ObservableCollection<ProductionBatchModel>();

            RefreshCommand = new RelayCommand(async _ => await LoadBatches());
            StartTestCommand = new RelayCommand(async _ => await StartTest());

            Task.Run(() => LoadBatches());
        }

        private async Task LoadBatches()
        {
            IsLoading = true;
            try
            {
                var batches = await _apiService.GetPendingBatches();
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    Batches.Clear();
                    var filtered = string.IsNullOrEmpty(SearchText)
                        ? batches
                        : batches.FindAll(b =>
                            (b.Name?.ToLower().Contains(SearchText.ToLower()) ?? false) ||
                            (b.Number?.ToLower().Contains(SearchText.ToLower()) ?? false));

                    foreach (var b in filtered)
                        Batches.Add(b);
                });
            }
            catch (Exception ex)
            {
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    System.Windows.MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task StartTest()
        {
            if (SelectedBatch == null) return;

            try
            {
                // Если уже есть активное испытание – открываем его
                if (SelectedBatch.HasActiveTest && SelectedBatch.ActiveTestId.HasValue)
                {
                    var testPage = new LabTestPage(_apiService, SelectedBatch.ActiveTestId.Value, _currentUserId, "production_batch", SelectedBatch.Id);
                    testPage.Owner = App.Current.MainWindow;
                    testPage.ShowDialog();
                    await LoadBatches();
                    return;
                }

                // Иначе создаём новое
                var testId = await _apiService.CreateLabTest(new CreateLabTestDto
                {
                    ObjectType = "production_batch",
                    ObjectId = SelectedBatch.Id,
                    TestType = "Контроль качества продукции",
                    CreatedBy = _currentUserId,
                    Comment = $"Испытание партии продукции {SelectedBatch.Number}"
                });

                if (testId > 0)
                {
                    var testPage = new LabTestPage(_apiService, testId, _currentUserId, "production_batch", SelectedBatch.Id);
                    testPage.Owner = App.Current.MainWindow;
                    testPage.ShowDialog();
                    await LoadBatches();
                }
            }
            catch (Exception ex)
            {
                // Если API вернул ошибку "уже есть незавершённое испытание", пробуем найти активное испытание
                if (ex.Message.Contains("уже есть незавершённое испытание") && SelectedBatch != null)
                {
                    var refreshedBatches = await _apiService.GetPendingBatches();
                    var fresh = refreshedBatches.Find(b => b.Id == SelectedBatch.Id);
                    if (fresh != null && fresh.HasActiveTest && fresh.ActiveTestId.HasValue)
                    {
                        var testPage = new LabTestPage(_apiService, fresh.ActiveTestId.Value, _currentUserId, "production_batch", SelectedBatch.Id);
                        testPage.Owner = App.Current.MainWindow;
                        testPage.ShowDialog();
                        await LoadBatches();
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Не удалось найти активное испытание", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}