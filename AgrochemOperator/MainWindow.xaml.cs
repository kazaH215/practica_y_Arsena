using System.Windows;
using AgrochemOperator.Models;
using AgrochemOperator.Views;

namespace AgrochemOperator
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
            MainFrame.Navigate(new ActiveBatchesPage(user.Token, user.Id));
        }

        private void ActiveBatches_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ActiveBatchesPage(_user.Token, _user.Id));
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}