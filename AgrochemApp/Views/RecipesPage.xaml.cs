using System.Windows.Controls;
using AgrochemApp.ViewModels;

namespace AgrochemApp.Views
{
    public partial class RecipesPage : Page
    {
        public RecipesPage(string token, int userId)
        {
            InitializeComponent();
            DataContext = new RecipesViewModel(token, userId);
        }
    }
}