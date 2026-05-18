using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AgrochemApp.Models;
using AgrochemApp.Views;

namespace AgrochemApp
{
    public partial class MainWindow : Window
    {
        private UserModel _user;
        private Button[] _menuButtons;

        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(UserModel user) : this()
        {
            _user = user;
            lblUser.Text = $"{user.FullName} ({user.Role})";

            // Загружаем дашборд
            MainFrame.Navigate(new DashboardPage(user.Token));

            // Инициализация кнопок для подсветки
            _menuButtons = new Button[]
            {
                btnDashboard, btnProducts, btnRecipes, btnTechCards,
                btnOrders, btnBatches, btnExtruder, btnDeviations, btnReports
            };
        }



        private void SetActiveButton(Button activeButton)
        {
            foreach (var btn in _menuButtons)
            {
                if (btn == activeButton)
                {
                    btn.Style = (Style)FindResource("ActiveSideMenuButton");
                }
                else
                {
                    btn.Style = (Style)FindResource("SideMenuButton");
                }
            }
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(btnDashboard);
            MainFrame.Navigate(new DashboardPage(_user.Token));
        }

        private void Products_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(btnProducts);
            MainFrame.Navigate(new ProductsPage(_user.Token));
        }

        private void Recipes_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(btnRecipes);
            MainFrame.Navigate(new RecipesPage(_user.Token, _user.Id));
        }

        private void TechCards_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(btnTechCards);
            MainFrame.Navigate(new TechCardsPage(_user.Token, _user.Id));
        }

        private void Orders_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(btnOrders);
            MainFrame.Navigate(new ProductionOrdersPage(_user.Token));
        }

        private void Batches_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(btnBatches);
            MessageBox.Show("Производственные партии - в разработке", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Extruder_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(btnExtruder);
            MessageBox.Show("Программы экструдера - в разработке", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Deviations_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(btnDeviations);
            MessageBox.Show("Отклонения и события - в разработке", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Reports_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(btnReports);
            MessageBox.Show("Отчёты - в разработке", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Выход",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }
    }
}