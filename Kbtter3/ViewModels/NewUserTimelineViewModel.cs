using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

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
    internal class NewUserTimelineViewModel : ViewModel
    {
        MainWindowViewModel main;
        public NewUserTimelineViewModel(MainWindowViewModel vm)
        {
            main = vm;
        }

        public void Initialize()
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            main = null;
            CompositeDisposable.Dispose();
        }

        #region ExtractTrueStatus変更通知プロパティ
        private bool _ExtractTrueStatus = true;

        public bool ExtractTrueStatus
        {
            get
            { return _ExtractTrueStatus; }
            set
            {
                if (_ExtractTrueStatus == value)
                    return;
                _ExtractTrueStatus = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region ExtractFalseStatus変更通知プロパティ
        private bool _ExtractFalseStatus;

        public bool ExtractFalseStatus
        {
            get
            { return _ExtractFalseStatus; }
            set
            {
                if (_ExtractFalseStatus == value)
                    return;
                _ExtractFalseStatus = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region QueryText変更通知プロパティ
        private string _QueryText;

        public string QueryText
        {
            get
            { return _QueryText; }
            set
            {
                if (_QueryText == value)
                    return;
                _QueryText = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region CreateCommand
        private ViewModelCommand _CreateCommand;

        public ViewModelCommand CreateCommand
        {
            get
            {
                if (_CreateCommand == null)
                {
                    _CreateCommand = new ViewModelCommand(Create);
                }
                return _CreateCommand;
            }
        }

        public void Create()
        {
            try
            {
                var q = new Kbtter3Query(QueryText);
                var vm = new UserCustomizableTimelineViewModel(main);
                vm.IsInverted = ExtractFalseStatus;
                vm.Query = q;
                main.RequestUserTimeline(vm);
                Messenger.Raise(new WindowActionMessage(WindowAction.Close, "Close"));
            }
            catch
            {
                Messenger.Raise(new InformationMessage("クエリ構文が間違っています", "構文エラー", "InformationNUT"));
                return;
            }

        }
        #endregion


    }
}
