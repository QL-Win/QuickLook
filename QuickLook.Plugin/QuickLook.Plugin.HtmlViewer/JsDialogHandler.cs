// Copyright © 2010-2017 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using CefSharp;

namespace QuickLook.Plugin.HtmlViewer
{
    public class JsDialogHandler : IJsDialogHandler
    {
        public bool OnJSDialog(IWebBrowser browserControl, IBrowser browser, string originUrl,
            CefJsDialogType dialogType, string messageText, string defaultPromptText, IJsDialogCallback callback,
            ref bool suppressMessage)
        {
            return true;
        }

        public bool OnJSBeforeUnload(IWebBrowser browserControl, IBrowser browser, string message, bool isReload,
            IJsDialogCallback callback)
        {
            //NOTE: No need to execute the callback if you return false
            // callback.Continue(true);

            //NOTE: Returning false will trigger the default behaviour, you need to return true to handle yourself.
            return true;
        }

        public void OnResetDialogState(IWebBrowser browserControl, IBrowser browser)
        {
        }

        public void OnDialogClosed(IWebBrowser browserControl, IBrowser browser)
        {
        }
    }
}