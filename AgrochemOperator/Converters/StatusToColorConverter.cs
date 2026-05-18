using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AgrochemOperator.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value as string ?? "";
            switch (status)
            {
                case "completed":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"));
                case "in_progress":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0EA5E9"));
                case "pending":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                case "skipped":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                default:
                    return new SolidColorBrush(Colors.Gray);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}