using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using AgrochemApp.Services;
using Newtonsoft.Json.Linq;

namespace AgrochemApp.Views
{
    public partial class DashboardPage : Page
    {
        private readonly ApiService _apiService;

        public DashboardPage(string token)
        {
            InitializeComponent();
            _apiService = new ApiService();
            _apiService.SetToken(token);
            LoadDashboard();
        }

        private async void LoadDashboard()
        {
            try
            {
                LoadingText.Visibility = Visibility.Visible;
                LoadingText.Text = "⏳ Загрузка данных...";

                var stats = await _apiService.GetAsync<dynamic>("/api/Reference/dashboard");

                if (stats != null && stats.success == true)
                {
                    var data = stats.data;
                    StatsPanel.Children.Clear();

                    AddStatCard("📦 Продукты", data?.products?.ToString() ?? "0", "#3B82F6");
                    AddStatCard("🧪 Сырьё", data?.rawMaterials?.ToString() ?? "0", "#10B981");
                    AddStatCard("⚙️ Оборудование", data?.equipment?.ToString() ?? "0", "#8B5CF6");
                    AddStatCard("▶️ Запущенные партии", data?.batches?.running?.ToString() ?? "0", "#F59E0B");
                    AddStatCard("⏳ Плановые партии", data?.batches?.planned?.ToString() ?? "0", "#6B7280");
                    AddStatCard("✅ Завершённые партии", data?.batches?.completed?.ToString() ?? "0", "#22C55E");
                }
                else
                {
                    LoadingText.Text = "❌ Ошибка загрузки статистики";
                    LoadingText.Foreground = System.Windows.Media.Brushes.Red;
                }

                // События пока отключим, чтобы не было ошибок
                EventsList.Items.Clear();
                EventsList.Items.Add("События будут доступны в следующей версии");
            }
            catch (Exception ex)
            {
                LoadingText.Text = $"❌ Ошибка: {ex.Message}";
                LoadingText.Foreground = System.Windows.Media.Brushes.Red;
            }
            finally
            {
                LoadingText.Visibility = Visibility.Collapsed;
            }
        }

        private void AddStatCard(string title, string value, string colorHex)
        {
            var brush = (Brush)new BrushConverter().ConvertFromString(colorHex);

            var border = new Border
            {
                Width = 220,
                Height = 110,
                Margin = new Thickness(0, 0, 20, 20),
                CornerRadius = new CornerRadius(16),
                Background = Brushes.White,
                Effect = new DropShadowEffect { BlurRadius = 12, ShadowDepth = 1, Opacity = 0.06 }
            };

            var stack = new StackPanel { Margin = new Thickness(18) };
            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 13,
                Foreground = Brushes.Gray
            });

            var valBlock = new TextBlock
            {
                Text = value,
                FontSize = 32,
                FontWeight = FontWeights.Bold,
                Foreground = brush,
                Margin = new Thickness(0, 10, 0, 0)
            };
            stack.Children.Add(valBlock);

            border.Child = stack;
            StatsPanel.Children.Add(border);
        }
    }
}