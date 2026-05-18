using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AgrochemApp.Converters
{
    public class PercentToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            decimal percent = 0;

            if (value is decimal dec)
                percent = dec;
            else if (value is int intVal)
                percent = intVal;
            else if (value is double dbl)
                percent = (decimal)dbl;
            else if (value is float fl)
                percent = (decimal)fl;

            if (percent == 100)
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"));
            }
            else
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}