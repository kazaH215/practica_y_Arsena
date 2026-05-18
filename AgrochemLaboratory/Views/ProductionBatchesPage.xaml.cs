using System.Windows.Controls;
using AgrochemLaboratory.ViewModels;

namespace AgrochemLaboratory.Views
{
    public partial class ProductionBatchesPage : Page
    {
        public ProductionBatchesPage(string token, int userId)
        {
            InitializeComponent();
            DataContext = new ProductionBatchesViewModel(token, userId);
        }
    }
}