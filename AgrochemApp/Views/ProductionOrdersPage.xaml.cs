using System.Windows.Controls;
using AgrochemApp.ViewModels;

namespace AgrochemApp.Views
{
    public partial class ProductionOrdersPage : Page
    {
        public ProductionOrdersPage(string token)
        {
            InitializeComponent();
            DataContext = new ProductionOrdersViewModel(token);
        }
    }
}