using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Net;
using Livet;
using System.Globalization;

namespace Kbtter3.Views
{
    internal sealed class UriToBitmapImageAsyncConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var u = value as Uri;

            var t = Task<BitmapImage>.Factory.StartNew(() =>
            {
                var b = new BitmapImage();
                if (u == null || u.ToString() == "")
                {
                    b.Freeze();
                    return b;
                }
                else
                {
                    using (var wc = new WebClient())
                    {
                        try
                        {
                            var bs = wc.DownloadData(u);
                            using (var ms = new MemoryStream(bs))
                            {
                                b.BeginInit();
                                b.CacheOption = BitmapCacheOption.OnLoad;
                                b.CreateOptions = BitmapCreateOptions.None;
                                b.StreamSource = ms;
                                b.EndInit();
                                if (b.CanFreeze) b.Freeze();
                            }
                        }
                        catch
                        {
                            b.Freeze();
                            return b;
                        }
                        return b;
                    }
                }

            });

            return new AsyncNotifier<BitmapImage>(t);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class UriToBitmapImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var u = value as Uri;
            if (u == null || u.ToString() == "")
            {
                var b = new BitmapImage();
                return b;
            }
            else
            {
                var b = new BitmapImage(u);
                return b;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class UriToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return "";
            return (value as Uri).ToString();
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

    internal sealed class BooleanToHorizontalAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var b = (bool)value;
            if (b)
            {
                return HorizontalAlignment.Right;
            }
            else
            {
                return HorizontalAlignment.Left;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class AsyncNotifier<T> : NotificationObject
    {

        #region Task変更通知プロパティ
        private Task<T> _Task;

        public Task<T> Task
        {
            get
            { return _Task; }
            set
            {
                if (_Task == value)
                    return;
                _Task = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region Result変更通知プロパティ
        private T _Result;

        public T Result
        {
            get
            { return _Result; }
            set
            {
                if (_Result != null && _Result.Equals(value))
                    return;
                _Result = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region IsCompleted変更通知プロパティ
        private bool _IsCompleted;

        public bool IsCompleted
        {
            get
            { return _IsCompleted; }
            set
            {
                if (_IsCompleted == value)
                    return;
                _IsCompleted = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region IsFailed変更通知プロパティ
        private bool _IsFailed;

        public bool IsFailed
        {
            get
            { return _IsFailed; }
            set
            {
                if (_IsFailed == value)
                    return;
                _IsFailed = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        public AsyncNotifier(Task<T> task)
        {
            Task = task;
            if (task.IsCompleted)
            {
                IsCompleted = true;
                Result = Task.Result;
                return;
            }
            Task.ContinueWith((r) =>
            {
                Result = r.Result;
                IsCompleted = r.IsCompleted;
                IsFailed = r.IsFaulted;
            });
        }
    }
}
