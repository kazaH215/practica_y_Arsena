using System.Windows;
using System.Windows.Controls;
using AgrochemLaboratory.Models;
using AgrochemLaboratory.Views;

namespace AgrochemLaboratory
{
    public partial class MainWindow : Window
    {
        private UserModel _user;

        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(UserModel user) : this()
        {
            _user = user;
            lblUser.Text = $"{user.FullName} ({user.Role})";

            // Загружаем дашборд
            MainFrame.Navigate(new DashboardPage(user.Token, user.Id));
        }

        private void SetActiveButton(Button activeButton)
        {
            var buttons = new Button[] { btnDashboard, btnRawMaterials, btnBatches, btnHistory };
            foreach (var btn in buttons)
            {
                btn.Style = btn == activeButton
                    ? (Style)FindResource("ActiveSideMenuButton")
                    : (Style)FindResource("SideMenuButton");
            }
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(btnDashboard);
            MainFrame.Navigate(new DashboardPage(_user.Token, _user.Id));
        }

        private void RawMaterials_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(btnRawMaterials);
            MainFrame.Navigate(new RawMaterialsPage(_user.Token, _user.Id));
        }

        private void Batches_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(btnBatches);
            MainFrame.Navigate(new ProductionBatchesPage(_user.Token, _user.Id));
        }

        private void History_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(btnHistory);
            MainFrame.Navigate(new TestHistoryPage(_user.Token));
        }

        private void Reports_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(btnReports);
            MainFrame.Navigate(new ReportsPage(_user.Token));
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