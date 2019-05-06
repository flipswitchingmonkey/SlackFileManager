using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Windows.Data;

namespace SlackFileManager
{
    public class DateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;

            return ((DateTime)value).ToString("yyyy-MM-dd (HH:mm)");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}