using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AgrochemApp.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value as string ?? "";

            if (status == "active" || status == "approved")
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"));
            }
            else if (status == "draft")
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
            }
            else if (status == "archived")
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));
            }
            else if (status == "rejected")
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
            }
            else
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}