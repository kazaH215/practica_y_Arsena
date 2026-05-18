using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AgrochemLaboratory.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value as string ?? "";

            if (status == "pending" || status == "pending_analysis")
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
            if (status == "approved" || status == "completed")
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"));
            if (status == "rejected" || status == "blocked")
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));

            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}