using System.Windows.Controls;
using AgrochemLaboratory.ViewModels;

namespace AgrochemLaboratory.Views
{
    public partial class TestHistoryPage : Page
    {
        public TestHistoryPage(string token)
        {
            InitializeComponent();
            DataContext = new TestHistoryViewModel(token);
        }
    }
}