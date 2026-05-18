using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using AgrochemApp.Models;
using AgrochemApp.Services;

namespace AgrochemApp.ViewModels
{
    public class ProductsViewModel : INotifyPropertyChanged
    {
        private readonly ApiService _apiService;
        private ObservableCollection<ProductModel> _products;
        private ProductModel _selectedProduct;
        private bool _isLoading;
        private string _searchText;

        public ObservableCollection<ProductModel> Products
        {
            get => _products;
            set { _products = value; OnPropertyChanged(); }
        }

        public ProductModel SelectedProduct
        {
            get => _selectedProduct;
            set { _selectedProduct = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                Task.Run(() => LoadProducts());
            }
        }

        public ICommand LoadProductsCommand { get; }
        public ICommand RefreshCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ProductsViewModel(string token)
        {
            _apiService = new ApiService();
            _apiService.SetToken(token);
            Products = new ObservableCollection<ProductModel>();

            LoadProductsCommand = new RelayCommand(async _ => await LoadProducts());
            RefreshCommand = new RelayCommand(async _ => await LoadProducts());

            Task.Run(() => LoadProducts());
        }

        private async Task LoadProducts()
        {
            IsLoading = true;
            try
            {
                var products = await _apiService.GetProducts();

                App.Current.Dispatcher.Invoke(() =>
                {
                    Products.Clear();

                    var filtered = string.IsNullOrEmpty(SearchText)
                        ? products
                        : products.FindAll(p => p.Name.ToLower().Contains(SearchText.ToLower())
                                             || p.Code.ToLower().Contains(SearchText.ToLower()));

                    foreach (var product in filtered)
                        Products.Add(product);
                });
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}