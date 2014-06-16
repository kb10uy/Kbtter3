using System;
using System.Collections.Specialized;
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
using System.Collections.ObjectModel;

using Livet;
using Livet.EventListeners;
using Livet.EventListeners.WeakEvents;


namespace Kbtter3.Views
{
    internal class HyperlinkMouseOverColorChangeBehavior : Behavior<Hyperlink>
    {
        public static DependencyProperty MouseEnteredForegroundProperty =
            DependencyProperty.Register(
                "MouseEnteredForeground",
                typeof(Brush),
                typeof(HyperlinkMouseOverColorChangeBehavior),
                new UIPropertyMetadata(null));

        public static DependencyProperty MouseLeftForegroundProperty =
            DependencyProperty.Register(
                "MouseLeftForeground",
                typeof(Brush),
                typeof(HyperlinkMouseOverColorChangeBehavior),
                new UIPropertyMetadata(null));

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

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseEnter += AssociatedObject_MouseEnter;
            AssociatedObject.MouseLeave += AssociatedObject_MouseLeave;
            AssociatedObject.Foreground = MouseLeftForeground;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.MouseEnter -= AssociatedObject_MouseEnter;
            AssociatedObject.MouseLeave -= AssociatedObject_MouseLeave;
        }

        private void AssociatedObject_MouseEnter(object sender, MouseEventArgs e)
        {
            AssociatedObject.Foreground = MouseEnteredForeground;
        }

        private void AssociatedObject_MouseLeave(object sender, MouseEventArgs e)
        {
            AssociatedObject.Foreground = MouseLeftForeground;
        }

    }

    internal class Kbtter3StatusBindingBehavior : Behavior<ListView>
    {
        public static DependencyProperty StatusesSourceProperty =
            DependencyProperty.Register(
                "StatusesSource",
                typeof(ObservableSynchronizedCollection<StatusViewModel>),
                typeof(Kbtter3StatusBindingBehavior));

        public ObservableSynchronizedCollection<StatusViewModel> StatusesSource
        {
            get { return GetValue(StatusesSourceProperty) as ObservableSynchronizedCollection<StatusViewModel>; }
            set
            {
                SetValue(StatusesSourceProperty, value);
                IsSourceEnable = value != null;
            }
        }

        private bool IsSourceEnable = false;

        protected override void OnAttached()
        {
            if (StatusesSource != null)
            {
                IsSourceEnable = true;
                StatusesSource.CollectionChanged += StatusesSource_CollectionChanged;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            StatusesSource.CollectionChanged -= StatusesSource_CollectionChanged;
        }

        private void StatusesSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            DispatcherHelper.UIDispatcher.BeginInvoke((Action)(() =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        var f = new Frame();
                        f.Content = new StatusPage(StatusesSource[e.NewStartingIndex]);
                        f.NavigationUIVisibility = NavigationUIVisibility.Hidden;
                        AssociatedObject.Items.Insert(e.NewStartingIndex, f);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        var c = (AssociatedObject.Items[e.OldStartingIndex] as Frame).Content;
                        if (c != null)
                        {
                            var idis = (c as StatusPage).DataContext as IDisposable;
                            if (idis != null) idis.Dispose();
                            (c as StatusPage).DataContext = null;
                        }
                        AssociatedObject.Items.RemoveAt(e.OldStartingIndex);
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        for (int i = AssociatedObject.Items.Count - 1; i >= 0; i--)
                        {
                            var c2 = (AssociatedObject.Items[i] as Frame).Content;
                            if (c2 != null)
                            {
                                var idis2 = (c2 as StatusPage).DataContext as IDisposable;
                                if (idis2 != null) idis2.Dispose();
                            }
                            AssociatedObject.Items.RemoveAt(i);
                        }
                        break;
                }
            }));
        }
    }

    internal class Kbtter3NotificationBindingBehavior : Behavior<ListView>
    {
        public static DependencyProperty NotificationsSourceProperty =
            DependencyProperty.Register(
                "NotificationsSource",
                typeof(ObservableSynchronizedCollection<NotificationViewModel>),
                typeof(Kbtter3NotificationBindingBehavior));

        public ObservableSynchronizedCollection<NotificationViewModel> NotificationsSource
        {
            get { return GetValue(NotificationsSourceProperty) as ObservableSynchronizedCollection<NotificationViewModel>; }
            set
            {
                SetValue(NotificationsSourceProperty, value);
                IsSourceEnable = value != null;
            }
        }

        private bool IsSourceEnable = false;

        protected override void OnAttached()
        {
            if (NotificationsSource != null)
            {
                IsSourceEnable = true;
                NotificationsSource.CollectionChanged += StatusesSource_CollectionChanged;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            NotificationsSource.CollectionChanged -= StatusesSource_CollectionChanged;
        }

        private void StatusesSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            DispatcherHelper.UIDispatcher.BeginInvoke((Action)(() =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        var f = new Frame();
                        f.Content = new NotificationPage(NotificationsSource[e.NewStartingIndex]);
                        f.NavigationUIVisibility = NavigationUIVisibility.Hidden;
                        AssociatedObject.Items.Insert(e.NewStartingIndex, f);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        var c = (AssociatedObject.Items[e.OldStartingIndex] as Frame).Content;
                        if (c != null)
                        {
                            var idis = (c as NotificationPage).DataContext as IDisposable;
                            if (idis != null) idis.Dispose();
                            (c as NotificationPage).DataContext = null;
                        }
                        AssociatedObject.Items.RemoveAt(e.OldStartingIndex);
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        for (int i = AssociatedObject.Items.Count - 1; i >= 0; i--)
                        {
                            var c2 = (AssociatedObject.Items[i] as Frame).Content;
                            if (c2 != null)
                            {
                                var idis2 = (c2 as NotificationPage).DataContext as IDisposable;
                                if (idis2 != null) idis2.Dispose();
                            }
                            AssociatedObject.Items.RemoveAt(i);
                        }
                        break;
                }
            }));
        }
    }
}
