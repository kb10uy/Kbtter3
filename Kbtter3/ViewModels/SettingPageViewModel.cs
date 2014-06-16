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

namespace Kbtter3.ViewModels
{
    internal class SettingPageViewModel : ViewModel
    {
        public Kbtter3Setting Setting;

        public void Load()
        {
            Setting = Kbtter3Extension.LoadJson<Kbtter3Setting>(App.ConfigurationFileName);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            CompositeDisposable.Dispose();
        }

        #region DebugBreakCommand
        private ViewModelCommand _DebugBreakCommand;

        public ViewModelCommand DebugBreakCommand
        {
            get
            {
                if (_DebugBreakCommand == null)
                {
                    _DebugBreakCommand = new ViewModelCommand(DebugBreak);
                }
                return _DebugBreakCommand;
            }
        }

        public void DebugBreak()
        {
            Console.WriteLine("SettingPageViewModel Debugging!");
        }
        #endregion


        #region SaveSettingCommand
        private ViewModelCommand _SaveSettingCommand;

        public ViewModelCommand SaveSettingCommand
        {
            get
            {
                if (_SaveSettingCommand == null)
                {
                    _SaveSettingCommand = new ViewModelCommand(SaveSetting);
                }
                return _SaveSettingCommand;
            }
        }

        public void SaveSetting()
        {
            Setting.SaveJson(App.ConfigurationFileName);
        }
        #endregion


    }
}
