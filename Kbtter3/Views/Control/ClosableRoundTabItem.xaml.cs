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
using Kbtter3.Views;
using Livet;

namespace Kbtter3.Views.Control
{
    /// <summary>
    /// ClosableRoundTabItem.xaml の相互作用ロジック
    /// </summary>
    internal partial class ClosableRoundTabItem : TabItem
    {
        public ClosableRoundTabItem()
        {
            InitializeComponent();
        }

        static ClosableRoundTabItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ClosableRoundTabItem),
                new FrameworkPropertyMetadata(typeof(ClosableRoundTabItem)));
        }

        public static readonly RoutedEvent TabItemClosedEvent = EventManager.RegisterRoutedEvent(
            "Closed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ClosableRoundTabItem));

        public event RoutedEventHandler Closed
        {
            add { AddHandler(TabItemClosedEvent, value); }
            remove { RemoveHandler(TabItemClosedEvent, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var bt=GetTemplateChild("ButtonClose") as Button;
            if (bt != null) bt.Click += new RoutedEventHandler(Button_Click);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(TabItemClosedEvent));
        }
    }
}
