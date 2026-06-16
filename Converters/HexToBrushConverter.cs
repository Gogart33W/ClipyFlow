using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ClipyFlow.Converters
{
    public class HexToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hexString && !string.IsNullOrEmpty(hexString))
            {
                try
                {
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexString));
                }
                catch
                {
                    return Brushes.Transparent;
                }
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
