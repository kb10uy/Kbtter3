using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using System.Windows.Media.Animation;
using System.ComponentModel;
using System.Diagnostics;
using Newtonsoft.Json;
using IOPath = System.IO.Path;
using Kbtter3.ViewModels;
using Kbtter3.Views.Control;
using System.Windows.Interactivity;

using Livet;
using Livet.EventListeners;
using Livet.EventListeners.WeakEvents;


namespace Kbtter3.Views
{
    internal class TwitterLinkedTextBehavior : Behavior<TextBlock>
    {
        public static DependencyProperty HostedMainWindowProperty =
            DependencyProperty.Register(
                "HostedMainWindow",
                typeof(MainWindowViewModel),
                typeof(TwitterLinkedTextBehavior));

        public static DependencyProperty TargetElementsProperty =
            DependencyProperty.Register(
                "TargetElements",
                typeof(IList<StatusViewModel.StatusElement>),
                typeof(TwitterLinkedTextBehavior));

        public MainWindowViewModel HostedMainWindow
        {
            get { return GetValue(HostedMainWindowProperty) as MainWindowViewModel; }
            set { SetValue(HostedMainWindowProperty, value); }
        }

        public IList<StatusViewModel.StatusElement> TargetElements
        {
            get { return GetValue(TargetElementsProperty) as IList<StatusViewModel.StatusElement>; }
            set
            {
                SetValue(TargetElementsProperty, value);
                Refresh();
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            Refresh();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
        }

        private void Refresh()
        {
            AssociatedObject.Inlines.Clear();
            if (TargetElements == null) return;
            foreach (var i in TargetElements)
            {
                switch (i.Type)
                {
                    case "None":
                        AssociatedObject.Inlines.Add(i.Text);
                        break;

                    case "Url":
                    case "Media":
                    case "Mention":
                    case "Hashtag":
                        var hl = new Hyperlink();
                        hl.Inlines.Add(i.Text);
                        hl.Tag = i;
                        var bh = new Kbtter3StatusLinkBehavior();
                        bh.MouseEnteredForeground = Brushes.Red;
                        bh.MouseLeftForeground = Brushes.DodgerBlue;
                        bh.HostedMainWindow = HostedMainWindow;
                        Interaction.GetBehaviors(hl).Add(bh);
                        AssociatedObject.Inlines.Add(hl);
                        break;
                    default:
                        throw new InvalidOperationException("予期しないテキスト要素です");
                }
            }
        }
    }

    internal class Kbtter3StatusLinkBehavior : Behavior<Hyperlink>
    {
        public static DependencyProperty MouseEnteredForegroundProperty =
            DependencyProperty.Register(
                "MouseEnteredForeground",
                typeof(Brush),
                typeof(Kbtter3StatusLinkBehavior),
                new UIPropertyMetadata(null));

        public static DependencyProperty MouseLeftForegroundProperty =
            DependencyProperty.Register(
                "MouseLeftForeground",
                typeof(Brush),
                typeof(Kbtter3StatusLinkBehavior),
                new UIPropertyMetadata(null));

        public static DependencyProperty HostedMainWindowProperty =
            DependencyProperty.Register(
                "HostedMainWindow",
                typeof(MainWindowViewModel),
                typeof(Kbtter3StatusLinkBehavior));

        public Brush MouseEnteredForeground
        {
            get { return GetValue(MouseEnteredForegroundProperty) as Brush; }
            set { SetValue(MouseEnteredForegroundProperty, value); }
        }

        public Brush MouseLeftForeground
        {
            get { return GetValue(MouseLeftForegroundProperty) as Brush; }
            set { SetValue(MouseLeftForegroundProperty, value); }
        }

        public MainWindowViewModel HostedMainWindow
        {
            get { return GetValue(HostedMainWindowProperty) as MainWindowViewModel; }
            set { SetValue(HostedMainWindowProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseEnter += AssociatedObject_MouseEnter;
            AssociatedObject.MouseLeave += AssociatedObject_MouseLeave;
            AssociatedObject.Click += AssociatedObject_Click;
            AssociatedObject.Foreground = MouseLeftForeground;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.MouseEnter -= AssociatedObject_MouseEnter;
            AssociatedObject.MouseLeave -= AssociatedObject_MouseLeave;
            AssociatedObject.Click -= AssociatedObject_Click;
        }

        private void AssociatedObject_MouseEnter(object sender, MouseEventArgs e)
        {
            AssociatedObject.Foreground = MouseEnteredForeground;
        }

        private void AssociatedObject_MouseLeave(object sender, MouseEventArgs e)
        {
            AssociatedObject.Foreground = MouseLeftForeground;
        }

        private void AssociatedObject_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (HostedMainWindow == null) return;
            var t = AssociatedObject.Tag as StatusViewModel.StatusElement;
            if (t == null) return;
            HostedMainWindow.RequestStatusAction(t.Type, t.Infomation);
        }

    }
}
