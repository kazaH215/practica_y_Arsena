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
    public class RawMaterialsViewModel : INotifyPropertyChanged
    {
        private readonly ApiService _apiService;
        private readonly int _currentUserId;
        private ObservableCollection<RawMaterialBatchModel> _rawMaterials;
        private RawMaterialBatchModel _selectedMaterial;
        private bool _isLoading;
        private string _searchText;

        public ObservableCollection<RawMaterialBatchModel> RawMaterials
        {
            get => _rawMaterials;
            set { _rawMaterials = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasNoData)); }
        }

        public RawMaterialBatchModel SelectedMaterial
        {
            get => _selectedMaterial;
            set { _selectedMaterial = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasNoData)); }
        }

        public bool HasNoData => !IsLoading && (RawMaterials == null || RawMaterials.Count == 0);

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); Task.Run(() => LoadMaterials()); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand StartTestCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public RawMaterialsViewModel(string token, int userId)
        {
            _apiService = new ApiService();
            _apiService.SetToken(token);
            _currentUserId = userId;
            RawMaterials = new ObservableCollection<RawMaterialBatchModel>();

            RefreshCommand = new RelayCommand(async _ => await LoadMaterials());
            StartTestCommand = new RelayCommand(async _ => await StartTest());

            Task.Run(() => LoadMaterials());
        }

        private async Task LoadMaterials()
        {
            IsLoading = true;
            try
            {
                var materials = await _apiService.GetPendingRawMaterials();
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    RawMaterials.Clear();
                    var filtered = string.IsNullOrEmpty(SearchText)
                        ? materials
                        : materials.FindAll(m =>
                            (m.Name?.ToLower().Contains(SearchText.ToLower()) ?? false) ||
                            (m.Number?.ToLower().Contains(SearchText.ToLower()) ?? false) ||
                            (m.Supplier?.ToLower().Contains(SearchText.ToLower()) ?? false));

                    foreach (var m in filtered)
                        RawMaterials.Add(m);
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
            if (SelectedMaterial == null) return;

            try
            {
                // Если уже есть активное испытание – открываем его
                if (SelectedMaterial.HasActiveTest && SelectedMaterial.ActiveTestId.HasValue)
                {
                    var testPage = new LabTestPage(_apiService, SelectedMaterial.ActiveTestId.Value, _currentUserId, "raw_material", SelectedMaterial.Id);
                    testPage.Owner = App.Current.MainWindow;
                    testPage.ShowDialog();
                    await LoadMaterials();
                    return;
                }

                // Иначе создаём новое
                var testId = await _apiService.CreateLabTest(new CreateLabTestDto
                {
                    ObjectType = "raw_material",
                    ObjectId = SelectedMaterial.Id,
                    TestType = "Входной контроль",
                    CreatedBy = _currentUserId,
                    Comment = $"Испытание партии сырья {SelectedMaterial.Number}"
                });

                if (testId > 0)
                {
                    var testPage = new LabTestPage(_apiService, testId, _currentUserId, "raw_material", SelectedMaterial.Id);
                    testPage.Owner = App.Current.MainWindow;
                    testPage.ShowDialog();
                    await LoadMaterials();
                }
            }
            catch (Exception ex)
            {
                // Если API вернул ошибку "уже есть незавершённое испытание", пробуем найти активное испытание
                if (ex.Message.Contains("уже есть незавершённое испытание") && SelectedMaterial != null)
                {
                    // Обновляем данные этой партии, чтобы получить актуальный ActiveTestId
                    var refreshedMaterials = await _apiService.GetPendingRawMaterials();
                    var fresh = refreshedMaterials.Find(m => m.Id == SelectedMaterial.Id);
                    if (fresh != null && fresh.HasActiveTest && fresh.ActiveTestId.HasValue)
                    {
                        var testPage = new LabTestPage(_apiService, fresh.ActiveTestId.Value, _currentUserId, "raw_material", SelectedMaterial.Id);
                        testPage.Owner = App.Current.MainWindow;
                        testPage.ShowDialog();
                        await LoadMaterials();
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