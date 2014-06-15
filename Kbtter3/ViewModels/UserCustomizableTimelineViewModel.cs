using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;

using Kbtter3.Models;
using Kbtter3.Query;

namespace Kbtter3.ViewModels
{
    internal class UserCustomizableTimelineViewModel : ViewModel
    {
        Kbtter kbtter = Kbtter.Instance;
        MainWindowViewModel main;
        PropertyChangedEventListener listener;

        public UserCustomizableTimelineViewModel(MainWindowViewModel mv)
        {
            main = mv;
            listener = new PropertyChangedEventListener(kbtter);
            CompositeDisposable.Add(listener);
            listener.Add("Status", OnStatus);
        }

        public UserCustomizableTimelineViewModel()
        {

        }

        ~UserCustomizableTimelineViewModel()
        {
            CompositeDisposable.Dispose();

        }

        public void Initialize()
        {
            Statuses.Clear();
        }

        private void OnStatus(object sender,PropertyChangedEventArgs e)
        {
            var st = kbtter.LatestStatus;
            Query.ClearVariables();
            Query.SetVariable("Status", st.Status);
            var ret = Query.Execute();
            if (ret)
            {
                main.NotifyInformation("合致");
                Statuses.Insert(0, StatusViewModelExtension.CreateStatusViewModel(main, st.Status));
            }
        }

        #region Query変更通知プロパティ
        private Kbtter3Query _Query = new Kbtter3Query("true");

        public Kbtter3Query Query
        {
            get
            { return _Query; }
            set
            {
                if (_Query == value)
                    return;
                _Query = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region Statuses変更通知プロパティ
        private ObservableSynchronizedCollection<StatusViewModel> _Statuses = new ObservableSynchronizedCollection<StatusViewModel>();

        public ObservableSynchronizedCollection<StatusViewModel> Statuses
        {
            get
            { return _Statuses; }
            set
            {
                if (_Statuses == value)
                    return;
                _Statuses = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region IsInverted変更通知プロパティ
        private bool _IsInverted;

        public bool IsInverted
        {
            get
            { return _IsInverted; }
            set
            {
                if (_IsInverted == value)
                    return;
                _IsInverted = value;
                RaisePropertyChanged();
            }
        }
        #endregion

    }
}
