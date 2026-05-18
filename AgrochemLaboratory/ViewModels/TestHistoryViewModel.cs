using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using AgrochemLaboratory.Models;
using AgrochemLaboratory.Services;

namespace AgrochemLaboratory.ViewModels
{
    public class TestHistoryViewModel : INotifyPropertyChanged
    {
        private readonly ApiService _apiService;
        private ObservableCollection<LabTestModel> _tests;
        private LabTestModel _selectedTest;
        private bool _isLoading;
        private string _searchText;

        public ObservableCollection<LabTestModel> Tests
        {
            get => _tests;
            set { _tests = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasNoData)); }
        }

        public LabTestModel SelectedTest
        {
            get => _selectedTest;
            set { _selectedTest = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasNoData)); }
        }

        public bool HasNoData => !IsLoading && (Tests == null || Tests.Count == 0);

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); Task.Run(() => LoadTests()); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand ViewDetailsCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public TestHistoryViewModel(string token)
        {
            _apiService = new ApiService();
            _apiService.SetToken(token);
            Tests = new ObservableCollection<LabTestModel>();

            RefreshCommand = new RelayCommand(async _ => await LoadTests());
            ViewDetailsCommand = new RelayCommand(_ => ShowDetails());

            Task.Run(() => LoadTests());
        }

        private async Task LoadTests()
        {
            IsLoading = true;
            try
            {
                var result = await _apiService.GetAsync<ApiResponse<List<LabTestModel>>>("/api/LabTests");
                var testsList = result?.data ?? new System.Collections.Generic.List<LabTestModel>();
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    Tests.Clear();
                    var filtered = string.IsNullOrEmpty(SearchText)
                        ? testsList
                        : testsList.FindAll(t =>
                            (t.ObjectName?.ToLower().Contains(SearchText.ToLower()) ?? false) ||
                            (t.ObjectNumber?.ToLower().Contains(SearchText.ToLower()) ?? false));

                    foreach (var t in filtered)
                        Tests.Add(t);
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

        private void ShowDetails()
        {
            if (SelectedTest != null)
            {
                System.Windows.MessageBox.Show(
                    $"Испытание #{SelectedTest.Id}\n" +
                    $"Объект: {SelectedTest.ObjectName}\n" +
                    $"Партия: {SelectedTest.ObjectNumber}\n" +
                    $"Тип: {SelectedTest.TestType}\n" +
                    $"Статус: {SelectedTest.Status}\n" +
                    $"Результат: {SelectedTest.Result ?? "не принят"}\n" +
                    $"Дата: {SelectedTest.CreatedAt}",
                    "Детали испытания",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show("Выберите испытание из списка", "Информация",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}