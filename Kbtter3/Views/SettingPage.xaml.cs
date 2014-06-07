using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Kbtter3.ViewModels;

namespace Kbtter3.Views
{

    /// <summary>
    /// SettingPage.xaml の相互作用ロジック
    /// </summary>
    internal partial class SettingPage : Page
    {
        Dictionary<string, Page> ChildPages = new Dictionary<string, Page>();
        SettingPageViewModel vm;

        public SettingPage()
        {
            InitializeComponent();
            vm = (SettingPageViewModel)DataContext;
            vm.Load();
            ChildPages["View.MainWindow"] = new Setting.View.MainWindowPage(vm.Setting.MainWindow);
            ChildPages["View.StatusPage"] = new Setting.View.StatusPagePage(vm.Setting.StatusPage);
            ChildPages["View.UserProfilePage"] = new Setting.View.UserProfilePagePage(vm.Setting.UserProfilePage);
            ChildPages["Kbtter3.Consumer"] = new Setting.Kbtter3.ConsumerPage(vm.Setting.Consumer);
        }

        private void TreeViewCategories_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var s = ((e.NewValue as TreeViewItem).Tag as string);
            if (!string.IsNullOrEmpty(s) && ChildPages.ContainsKey(s)) FramePaging.Content = ChildPages[s];
        }
    }
}