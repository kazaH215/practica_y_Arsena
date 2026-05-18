using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using AgrochemOperator.Models;
using AgrochemOperator.Services;
using AgrochemOperator.Views;

namespace AgrochemOperator.ViewModels
{
    public class ActiveBatchesViewModel : INotifyPropertyChanged
    {
        private readonly ApiService _apiService;
        private readonly int _userId;
        private ObservableCollection<ProductionBatch> _batches;
        private ProductionBatch _selectedBatch;
        private bool _isLoading;
        private string _searchText;

        public ObservableCollection<ProductionBatch> Batches
        {
            get => _batches;
            set { _batches = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasNoData)); }
        }

        public ProductionBatch SelectedBatch
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
        public ICommand SelectBatchCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ActiveBatchesViewModel(string token, int userId)
        {
            _apiService = new ApiService();
            _apiService.SetToken(token);
            _userId = userId;
            Batches = new ObservableCollection<ProductionBatch>();

            RefreshCommand = new RelayCommand(async _ => await LoadBatches());
            SelectBatchCommand = new RelayCommand(_ => SelectBatch());

            Task.Run(() => LoadBatches());
        }

        private async Task LoadBatches()
        {
            IsLoading = true;
            try
            {
                var batches = await _apiService.GetActiveBatches();
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    Batches.Clear();
                    var filtered = string.IsNullOrEmpty(SearchText)
                        ? batches
                        : batches.FindAll(b => b.BatchNumber.ToLower().Contains(SearchText.ToLower()) ||
                                               (b.ProductName?.ToLower().Contains(SearchText.ToLower()) ?? false));
                    foreach (var b in filtered)
                        Batches.Add(b);
                });
            }
            catch (Exception ex)
            {
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    System.Windows.MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void SelectBatch()
        {
            if (SelectedBatch == null) return;
            var programWindow = new BatchProgramPage(_apiService, SelectedBatch, _userId);
            programWindow.Owner = App.Current.MainWindow;
            programWindow.ShowDialog();
            // После закрытия программы партии обновим список
            Task.Run(() => LoadBatches());
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}