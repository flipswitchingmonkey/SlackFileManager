using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Windows.Data;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace SlackFileManager
{
    public class BitmapImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;

            try
            {
                WebRequest request = WebRequest.Create((string)value);
                WebResponse response = request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                Bitmap bmp = new Bitmap(responseStream);
                return bmp;

            }
            catch (Exception)
            {
                return null;
            }
            //BitmapImage bi = new BitmapImage();

            //bi.BeginInit();
            //bi.UriSource = new Uri((string)value, UriKind.RelativeOrAbsolute);
            //bi.EndInit();

            //return bi;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}