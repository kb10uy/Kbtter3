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
using CoreTweet;

namespace Kbtter3.ViewModels
{
    internal class DirectMessageViewModel : ViewModel
    {
        MainWindowViewModel mainw;
        public DirectMessageViewModel()
        {

        }

        public DirectMessageViewModel(MainWindowViewModel mwvm, string mysn)
        {
            mainw = mwvm;
            MyScreenName = mysn;
            Initialize();
        }


        #region MyScreenName変更通知プロパティ
        private string _MyScreenName;

        public string MyScreenName
        {
            get
            { return _MyScreenName; }
            set
            {
                if (_MyScreenName == value)
                    return;
                _MyScreenName = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region TargetUserName変更通知プロパティ
        private string _TargetUserName = "Name";

        public string TargetUserName
        {
            get
            { return _TargetUserName; }
            set
            {
                if (_TargetUserName == value)
                    return;
                _TargetUserName = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region TargetUserScreenName変更通知プロパティ
        private string _TargetUserScreenName = "ScreenName";

        public string TargetUserScreenName
        {
            get
            { return _TargetUserScreenName; }
            set
            {
                if (_TargetUserScreenName == value)
                    return;
                _TargetUserScreenName = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region TargetUserImageUri変更通知プロパティ
        private Uri _TargetUserImageUri;

        public Uri TargetUserImageUri
        {
            get
            { return _TargetUserImageUri; }
            set
            {
                if (_TargetUserImageUri == value)
                    return;
                _TargetUserImageUri = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region MyMessage変更通知プロパティ
        private ObservableSynchronizedCollection<DirectMessageItemViewModel> _MyMessage = new ObservableSynchronizedCollection<DirectMessageItemViewModel>();

        public ObservableSynchronizedCollection<DirectMessageItemViewModel> MyMessage
        {
            get
            { return _MyMessage; }
            set
            {
                if (_MyMessage == value)
                    return;
                _MyMessage = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region ReceiviedMessage変更通知プロパティ
        private ObservableSynchronizedCollection<DirectMessageItemViewModel> _ReceiviedMessage = new ObservableSynchronizedCollection<DirectMessageItemViewModel>();

        public ObservableSynchronizedCollection<DirectMessageItemViewModel> ReceiviedMessage
        {
            get
            { return _ReceiviedMessage; }
            set
            {
                if (_ReceiviedMessage == value)
                    return;
                _ReceiviedMessage = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region MergedMessage変更通知プロパティ
        private ObservableSynchronizedCollection<DirectMessageItemViewModel> _MergedMessage = new ObservableSynchronizedCollection<DirectMessageItemViewModel>();

        public ObservableSynchronizedCollection<DirectMessageItemViewModel> MergedMessage
        {
            get
            { return _MergedMessage; }
            set
            {
                if (_MergedMessage == value)
                    return;
                _MergedMessage = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        public void Initialize()
        {
        }

        public void AddMessage(DirectMessage dm)
        {
            var divm = new DirectMessageItemViewModel(dm);
            MergedMessage.Add(divm);
            if (dm.Recipient.ScreenName == TargetUserScreenName)
            {
                divm.IsSent = true;
                MyMessage.Add(divm);
            }
            else
            {
                divm.IsReceived = true;
                ReceiviedMessage.Add(divm);
            }

        }

        public bool CheckUserPair(string u1, string u2)
        {
            return (u1 == TargetUserScreenName && u2 == MyScreenName) || (u1 == MyScreenName && u2 == TargetUserScreenName);
        }
    }
}
