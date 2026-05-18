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
    public class RecipesViewModel : INotifyPropertyChanged
    {
        private readonly ApiService _apiService;
        private readonly int _currentUserId;
        private ObservableCollection<RecipeModel> _recipes;
        private RecipeModel _selectedRecipe;
        private bool _isLoading;
        private string _searchText;

        public ObservableCollection<RecipeModel> Recipes
        {
            get => _recipes;
            set { _recipes = value; OnPropertyChanged(); }
        }

        public RecipeModel SelectedRecipe
        {
            get => _selectedRecipe;
            set { _selectedRecipe = value; OnPropertyChanged(); }
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
                Task.Run(() => LoadRecipes());
            }
        }

        public ICommand LoadRecipesCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ApproveCommand { get; }
        public ICommand ShowDetailsCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public RecipesViewModel(string token, int userId)
        {
            _apiService = new ApiService();
            _apiService.SetToken(token);  // ← БЕЗ ЭТОГО ТОКЕН НЕ УСТАНОВИТСЯ!
            _currentUserId = userId;
            Recipes = new ObservableCollection<RecipeModel>();

            LoadRecipesCommand = new RelayCommand(async _ => await LoadRecipes());
            RefreshCommand = new RelayCommand(async _ => await LoadRecipes());
            ApproveCommand = new RelayCommand(async _ => await ApproveRecipe());
            ShowDetailsCommand = new RelayCommand(_ => ShowDetails());

            Task.Run(() => LoadRecipes());
        }

        private async Task LoadRecipes()
        {
            IsLoading = true;
            try
            {
                System.Diagnostics.Debug.WriteLine("=== LoadRecipes START ===");
                var recipes = await _apiService.GetRecipes();
                System.Diagnostics.Debug.WriteLine($"Recipes count: {recipes?.Count ?? 0}");

                if (recipes == null || recipes.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("Recipes is NULL or EMPTY!");
                }

                App.Current.Dispatcher.Invoke(() =>
                {
                    Recipes.Clear();
                    foreach (var recipe in recipes)
                    {
                        System.Diagnostics.Debug.WriteLine($"Adding: {recipe.Id} - {recipe.ProductName}");
                        Recipes.Add(recipe);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ApproveRecipe()
        {
            if (SelectedRecipe == null)
            {
                System.Windows.MessageBox.Show("Выберите рецептуру", "Предупреждение",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (SelectedRecipe.Status == "approved")
            {
                System.Windows.MessageBox.Show("Рецептура уже утверждена", "Информация",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            var result = await _apiService.ApproveRecipe(SelectedRecipe.Id, _currentUserId);
            if (result)
            {
                System.Windows.MessageBox.Show("Рецептура утверждена", "Успех",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                await LoadRecipes();
            }
            else
            {
                System.Windows.MessageBox.Show("Ошибка утверждения. Проверьте сумму компонентов (должна быть 100%)",
                    "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ShowDetails()
        {
            if (SelectedRecipe != null)
            {
                System.Windows.MessageBox.Show(
                    $"Рецептура: {SelectedRecipe.ProductName} v{SelectedRecipe.Version}\n" +
                    $"Статус: {SelectedRecipe.Status}\n" +
                    $"Сумма компонентов: {SelectedRecipe.TotalPercent}%\n" +
                    $"Компонентов: {SelectedRecipe.Components?.Count ?? 0}",
                    "Детали рецептуры",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }




        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}