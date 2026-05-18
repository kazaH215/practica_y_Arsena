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
    public class TechCardsViewModel : INotifyPropertyChanged
    {
        private readonly ApiService _apiService;
        private readonly int _currentUserId;
        private ObservableCollection<TechCardModel> _techCards;
        private TechCardModel _selectedTechCard;
        private bool _isLoading;
        private string _searchText;

        public ObservableCollection<TechCardModel> TechCards
        {
            get => _techCards;
            set { _techCards = value; OnPropertyChanged(); }
        }

        public TechCardModel SelectedTechCard
        {
            get => _selectedTechCard;
            set { _selectedTechCard = value; OnPropertyChanged(); }
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
                Task.Run(() => LoadTechCards());
            }
        }

        public ICommand LoadCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ApproveCommand { get; }
        public ICommand ShowDetailsCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public TechCardsViewModel(string token, int userId)
        {
            _apiService = new ApiService();
            _apiService.SetToken(token);
            _currentUserId = userId;
            TechCards = new ObservableCollection<TechCardModel>();

            LoadCommand = new RelayCommand(async _ => await LoadTechCards());
            RefreshCommand = new RelayCommand(async _ => await LoadTechCards());
            ApproveCommand = new RelayCommand(async _ => await ApproveTechCard());
            ShowDetailsCommand = new RelayCommand(_ => ShowDetails());

            Task.Run(() => LoadTechCards());
        }

        private async Task LoadTechCards()
        {
            IsLoading = true;
            try
            {
                var cards = await _apiService.GetTechCards();

                App.Current.Dispatcher.Invoke(() =>
                {
                    TechCards.Clear();

                    var filtered = string.IsNullOrEmpty(SearchText)
                        ? cards
                        : cards.FindAll(c => c.ProductName.ToLower().Contains(SearchText.ToLower()));

                    foreach (var card in filtered)
                        TechCards.Add(card);
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

        private async Task ApproveTechCard()
        {
            if (SelectedTechCard == null)
            {
                System.Windows.MessageBox.Show("Выберите технологическую карту", "Предупреждение",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var result = await _apiService.ApproveTechCard(SelectedTechCard.Id, _currentUserId);
            if (result)
            {
                System.Windows.MessageBox.Show("Технологическая карта утверждена", "Успех",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                await LoadTechCards();
            }
            else
            {
                System.Windows.MessageBox.Show("Ошибка утверждения", "Ошибка",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ShowDetails()
        {
            if (SelectedTechCard != null)
            {
                var steps = SelectedTechCard.Steps != null ? string.Join("\n", SelectedTechCard.Steps) : "нет";
                System.Windows.MessageBox.Show(
                    $"Техкарта: {SelectedTechCard.ProductName} v{SelectedTechCard.Version}\n" +
                    $"Статус: {SelectedTechCard.Status}\n" +
                    $"Шагов: {SelectedTechCard.Steps?.Count ?? 0}",
                    "Детали техкарты",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}