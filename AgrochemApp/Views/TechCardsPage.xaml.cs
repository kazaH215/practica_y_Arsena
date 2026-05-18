using System.Windows.Controls;
using AgrochemApp.ViewModels;

namespace AgrochemApp.Views
{
    public partial class TechCardsPage : Page
    {
        public TechCardsPage(string token, int userId)
        {
            InitializeComponent();
            DataContext = new TechCardsViewModel(token, userId);
        }
    }
}