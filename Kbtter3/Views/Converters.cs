using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;

namespace Kbtter3.Views
{
    internal sealed class UriToBitmapImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return new BitmapImage();
            return new BitmapImage(value as Uri);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class InvertedVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var v = (Visibility)value;
            switch (v)
            {
                case Visibility.Collapsed:
                    return Visibility.Visible;
                case Visibility.Hidden:
                    return Visibility.Visible;
                case Visibility.Visible:
                    return Visibility.Collapsed;
                default:
                    return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class DoubleTwitterBannerHeightConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((double)value) / 3.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((double)value) * 3.0;
        }
    }

    internal sealed class DoubleTwitterUserImageHeightConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((double)value) / 6.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class Int32ToShortenNumberStringConverter : IValueConverter
    {
        //エクサまで対応

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var t = (long)value;
            if (t < 1000)
            {
                return t.ToString();
            }
            else if (t < 1000000)
            {
                var dn = ((double)t) / 1000.0;
                return dn >= 100 ? dn.ToString("#") + "K" : dn.ToString("#.#") + "K";
            }
            else if (t < 1000000000)
            {
                var dn = ((double)t) / 1000000.0;
                return dn >= 100 ? dn.ToString("#") + "M" : dn.ToString("#.#") + "M";
            }
            else if (t < 1000000000000)
            {
                var dn = ((double)t) / 1000000000.0;
                return dn >= 100 ? dn.ToString("#") + "G" : dn.ToString("#.#") + "G";
            }
            else if (t < 1000000000000000)
            {
                var dn = ((double)t) / 1000000000000.0;
                return dn >= 100 ? dn.ToString("#") + "T" : dn.ToString("#.#") + "T";
            }
            else if (t < 1000000000000000000)
            {
                var dn = ((double)t) / 1000000000000000.0;
                return dn >= 100 ? dn.ToString("#") + "P" : dn.ToString("#.#") + "P";
            }
            else
            {
                var dn = ((double)t) / 1000000000000000.0;
                return dn >= 100 ? dn.ToString("#") + "E" : dn.ToString("#.#") + "E";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
