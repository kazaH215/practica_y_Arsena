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
    public class ProductionOrdersViewModel : INotifyPropertyChanged
    {
        private readonly ApiService _apiService;
        private ObservableCollection<ProductionOrderModel> _orders;
        private ProductionOrderModel _selectedOrder;
        private bool _isLoading;
        private string _searchText;

        public ObservableCollection<ProductionOrderModel> Orders
        {
            get => _orders;
            set
            {
                _orders = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasNoData));
            }
        }

        public ProductionOrderModel SelectedOrder
        {
            get => _selectedOrder;
            set { _selectedOrder = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasNoData));
            }
        }

        public bool HasNoData => !IsLoading && (Orders == null || Orders.Count == 0);

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                Task.Run(() => LoadOrders());
            }
        }

        public ICommand LoadCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand CreateCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ProductionOrdersViewModel(string token)
        {
            _apiService = new ApiService();
            _apiService.SetToken(token);
            Orders = new ObservableCollection<ProductionOrderModel>();

            LoadCommand = new RelayCommand(async _ => await LoadOrders());
            RefreshCommand = new RelayCommand(async _ => await LoadOrders());
            CancelCommand = new RelayCommand(async _ => await CancelOrder());
            CreateCommand = new RelayCommand(async _ => await CreateOrder());

            Task.Run(() => LoadOrders());
        }

        private async Task LoadOrders()
        {
            IsLoading = true;
            try
            {
                var orders = await _apiService.GetOrders();

                App.Current.Dispatcher.Invoke(() =>
                {
                    Orders.Clear();

                    var filtered = string.IsNullOrEmpty(SearchText)
                        ? orders
                        : orders.FindAll(o => o.ProductName != null && o.ProductName.ToLower().Contains(SearchText.ToLower()));

                    foreach (var order in filtered)
                        Orders.Add(order);
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

        private async Task CancelOrder()
        {
            if (SelectedOrder == null)
            {
                System.Windows.MessageBox.Show("Выберите заказ для отмены", "Предупреждение",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (SelectedOrder.Status == "cancelled")
            {
                System.Windows.MessageBox.Show("Заказ уже отменён", "Информация",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            var result = await _apiService.CancelOrder(SelectedOrder.Id);
            if (result)
            {
                await LoadOrders();
                System.Windows.MessageBox.Show("Заказ отменён", "Успех",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        private async Task CreateOrder()
        {
            var dto = new CreateOrderDto
            {
                ProductId = 1,
                PlannedQty = 1000
            };

            var result = await _apiService.CreateOrder(dto);
            if (result)
            {
                await LoadOrders();
                System.Windows.MessageBox.Show("Тестовый заказ создан", "Успех",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show("Ошибка создания заказа", "Ошибка",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}