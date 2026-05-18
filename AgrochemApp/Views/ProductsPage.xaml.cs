using System.Windows.Controls;
using AgrochemApp.ViewModels;

namespace AgrochemApp.Views
{
    public partial class ProductsPage : Page
    {
        public ProductsPage(string token)
        {
            InitializeComponent();
            DataContext = new ProductsViewModel(token);
        }
    }
}