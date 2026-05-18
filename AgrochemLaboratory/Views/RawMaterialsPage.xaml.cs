using System.Windows.Controls;
using AgrochemLaboratory.ViewModels;

namespace AgrochemLaboratory.Views
{
    public partial class RawMaterialsPage : Page
    {
        public RawMaterialsPage(string token, int userId)
        {
            InitializeComponent();
            DataContext = new RawMaterialsViewModel(token, userId);
        }
    }
}