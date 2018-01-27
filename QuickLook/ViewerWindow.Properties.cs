// Copyright © 2017 Paddy Xu
// 
// This file is part of QuickLook program.
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using QuickLook.Common.Annotations;
using QuickLook.Common.Plugin;

namespace QuickLook
{
    public partial class ViewerWindow : INotifyPropertyChanged
    {
        private readonly ResourceDictionary _darkDict = new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/QuickLook;component/Styles/MainWindowStyles.Dark.xaml")
        };
        private bool _canOldPluginResize;
        private bool _pinned;
        internal bool Pinned
        {
            get => _pinned;
            set
            {
                _pinned = value;
                buttonPin.Tag = "Pin";
                OnPropertyChanged();
            }
        }
        public IViewer Plugin { get; private set; }
        public ContextObject ContextObject { get; private set; }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private void ContextObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ContextObject.UseDarkTheme):
                    SwitchTheme(ContextObject.UseDarkTheme);
                    break;
                default:
                    break;
            }
        }

        public void SwitchTheme(bool dark)
        {
            if (dark)
            {
                if (!Resources.MergedDictionaries.Contains(_darkDict))
                    Resources.MergedDictionaries.Add(_darkDict);
            }
            else
            {
                if (Resources.MergedDictionaries.Contains(_darkDict))
                    Resources.MergedDictionaries.Remove(_darkDict);
            }
        }
    }
}