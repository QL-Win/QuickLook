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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Navigation;
using SHDocVw;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using WebBrowser = System.Windows.Controls.WebBrowser;

namespace QuickLook.Plugin.HtmlViewer
{
    /// <summary>
    ///     Class wraps a Browser (which itself is a bad designed WPF control) and presents itself as
    ///     a better designed WPF control. For example provides a bindable source property or commands.
    /// </summary>
    public class WpfWebBrowserWrapper : ContentControl, IDisposable
    {
        private static readonly Guid SidSWebBrowserApp = new Guid("0002DF05-0000-0000-C000-000000000046");

        private WebBrowser _innerBrowser;
        private bool _loaded;
        private int _zoom;

        public WpfWebBrowserWrapper()
        {
            _innerBrowser = new WebBrowser
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            Content = _innerBrowser;
            _innerBrowser.Navigated += InnerBrowserNavigated;
            _innerBrowser.Navigating += InnerBrowserNavigating;
            _innerBrowser.LoadCompleted += InnerBrowserLoadCompleted;
            _innerBrowser.Loaded += InnerBrowserLoaded;
            _innerBrowser.SizeChanged += InnerBrowserSizeChanged;
        }

        public string Url { get; private set; }

        public int Zoom
        {
            get => _zoom;
            set
            {
                _zoom = value;
                ApplyZoom();
            }
        }


        // gets the browser control's underlying activeXcontrol. Ready only from within Loaded-event but before loaded Document!
        // do not use prior loaded event.
        public InternetExplorer ActiveXControl
        {
            get
            {
                // this is a brilliant way to access the WebBrowserObject prior to displaying the actual document (eg. Document property)
                var fiComWebBrowser =
                    typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fiComWebBrowser == null) return null;
                var objComWebBrowser = fiComWebBrowser.GetValue(_innerBrowser);
                return objComWebBrowser as InternetExplorer;
            }
        }

        public void Dispose()
        {
            _innerBrowser.Source = null;
            _innerBrowser.Dispose();
            _innerBrowser = null;
            Content = null;
        }

        private void InnerBrowserSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyZoom();
        }

        private void InnerBrowserLoaded(object sender, EventArgs e)
        {
            var ie = ActiveXControl;
            ie.Silent = true;
        }

        // called when the loading of a web page is done
        private void InnerBrowserLoadCompleted(object sender, NavigationEventArgs e)
        {
            ApplyZoom(); // apply later and not only at changed event, since only works if browser is rendered.
        }

        // called when the browser started to load and retrieve data.
        private void InnerBrowserNavigating(object sender, NavigatingCancelEventArgs e)
        {
            if (_loaded)
                if (_innerBrowser.Source != null)
                    if (_innerBrowser.Source.Scheme != e.Uri.Scheme ||
                        _innerBrowser.Source.AbsolutePath != e.Uri.AbsolutePath) // allow in-page navigation
                        e.Cancel = true;
            _loaded = true;
        }

        // re query the commands once done navigating.
        private void InnerBrowserNavigated(object sender, NavigationEventArgs e)
        {
            RegisterWindowErrorHanlder_();

            var alertBlocker =
                "window.print = window.alert = window.open = null;document.oncontextmenu=function(){return false;}";
            _innerBrowser.InvokeScript("execScript", alertBlocker, "JavaScript");
        }

        public void Navigate(string uri)
        {
            Url = uri;

            if (_innerBrowser == null)
                return;

            if (!string.IsNullOrWhiteSpace(uri) && Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                try
                {
                    _innerBrowser.Source = new Uri(uri);
                }
                catch (UriFormatException)
                {
                    // just don't crash because of a malformed url
                }
            else
                _innerBrowser.Source = null;
        }

        public void Navigate(Stream stream)
        {
            if (_innerBrowser == null)
                return;

            try
            {
                _innerBrowser.NavigateToStream(stream);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        // register script errors handler on DOM - document.window
        private void RegisterWindowErrorHanlder_()
        {
            object parwin = ((dynamic)_innerBrowser.Document).parentWindow;
            var cookie = new AxHost.ConnectionPointCookie(parwin, new HtmlWindowEvents2Impl(this),
                typeof(IIntHTMLWindowEvents2));
            // MemoryLEAK? No: cookie has a Finalize() to Disconnect istelf. We'll rely on that. If disconnected too early, 
            // though (eg. in LoadCompleted-event) scripts continue to run and can cause error messages to appear again.
            // --> forget cookie and be happy.
        }

        private void ApplyZoom()
        {
            if (_innerBrowser == null || !_innerBrowser.IsLoaded)
                return;

            // grab a handle to the underlying ActiveX object
            IServiceProvider serviceProvider = null;
            if (_innerBrowser.Document != null)
                serviceProvider = (IServiceProvider)_innerBrowser.Document;
            if (serviceProvider == null)
                return;

            var serviceGuid = SidSWebBrowserApp;
            var iid = typeof(IWebBrowser2).GUID;
            var browserInst =
                (IWebBrowser2)serviceProvider.QueryService(ref serviceGuid, ref iid);

            try
            {
                object zoomPercObj = _zoom;
                // send the zoom command to the ActiveX object
                browserInst.ExecWB(OLECMDID.OLECMDID_OPTICAL_ZOOM,
                    OLECMDEXECOPT.OLECMDEXECOPT_DONTPROMPTUSER,
                    ref zoomPercObj,
                    IntPtr.Zero);
            }
            catch (Exception)
            {
                // ignore this dynamic call if it fails.
            }
        }

        // needed to implement the Event for script errors
        [Guid("3050f625-98b5-11cf-bb82-00aa00bdce0b")]
        [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
        [TypeLibType(TypeLibTypeFlags.FHidden)]
        [ComImport]
        private interface IIntHTMLWindowEvents2
        {
            [DispId(1002)]
            bool onerror(string description, string url, int line);
        }

        // needed to implement the Event for script errors
        private class HtmlWindowEvents2Impl : IIntHTMLWindowEvents2
        {
            private readonly WpfWebBrowserWrapper _control;

            public HtmlWindowEvents2Impl(WpfWebBrowserWrapper control)
            {
                _control = control;
            }

            // implementation of the onerror Javascript error. Return true to indicate a "Handled" state.
            public bool onerror(string description, string urlString, int line)
            {
                Debug.WriteLine(description + "@" + urlString + ": " + line);
                // Handled:
                return true;
            }
        }

        // Needed to expose the WebBrowser's underlying ActiveX control for zoom functionality
        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("6d5140c1-7436-11ce-8034-00aa006009fa")]
        internal interface IServiceProvider
        {
            [return: MarshalAs(UnmanagedType.IUnknown)]
            object QueryService(ref Guid guidService, ref Guid riid);
        }
    }
}