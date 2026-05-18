using System.Windows.Controls;
using AgrochemLaboratory.ViewModels;

namespace AgrochemLaboratory.Views
{
    public partial class ReportsPage : Page
    {
        public ReportsPage(string token)
        {
            InitializeComponent();
            DataContext = new ReportsViewModel(token);
        }
    }
}