using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Livet;

namespace Kbtter3.Views.Setting.View
{
    /// <summary>
    /// MainWindowPage.xaml の相互作用ロジック
    /// </summary>
    internal partial class UserProfilePagePage : Page
    {
        public UserProfilePagePage(UserProfilePageSetting st)
        {
            InitializeComponent();
            DataContext = st;
        }
    }
}
