using System.Windows.Controls;
using AgrochemOperator.ViewModels;

namespace AgrochemOperator.Views
{
    public partial class ActiveBatchesPage : Page
    {
        public ActiveBatchesPage(string token, int userId)
        {
            InitializeComponent();
            DataContext = new ActiveBatchesViewModel(token, userId);
        }
    }
}