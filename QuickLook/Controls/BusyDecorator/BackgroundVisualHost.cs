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
using System.Collections;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace QuickLook.Controls.BusyDecorator;

public delegate Visual CreateContentFunction();

public class BackgroundVisualHost : FrameworkElement
{
    protected override int VisualChildrenCount => _hostVisual != null ? 1 : 0;

    protected override IEnumerator LogicalChildren
    {
        get
        {
            if (_hostVisual != null)
                yield return _hostVisual;
        }
    }

    protected override Visual GetVisualChild(int index)
    {
        if (_hostVisual != null && index == 0)
            return _hostVisual;

        throw new IndexOutOfRangeException("index");
    }

    private void CreateContentHelper()
    {
        ThreadedHelper = new ThreadedVisualHelper(CreateContent, SafeInvalidateMeasure);
        _hostVisual = ThreadedHelper.HostVisual;
    }

    private void SafeInvalidateMeasure()
    {
        Dispatcher.BeginInvoke(new Action(InvalidateMeasure), DispatcherPriority.Loaded);
    }

    private void HideContentHelper()
    {
        if (ThreadedHelper != null)
        {
            ThreadedHelper.Exit();
            ThreadedHelper = null;
            InvalidateMeasure();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (ThreadedHelper != null)
            return ThreadedHelper.DesiredSize;

        return base.MeasureOverride(availableSize);
    }

    public class ThreadedVisualHelper
    {
        private readonly CreateContentFunction _createContent;
        private readonly Action _invalidateMeasure;

        private readonly AutoResetEvent _sync =
            new AutoResetEvent(false);

        public ThreadedVisualHelper(
            CreateContentFunction createContent,
            Action invalidateMeasure)
        {
            HostVisual = new HostVisual();
            _createContent = createContent;
            _invalidateMeasure = invalidateMeasure;

            var backgroundUi = new Thread(CreateAndShowContent);
            backgroundUi.SetApartmentState(ApartmentState.STA);
            backgroundUi.Name = "BackgroundVisualHostThread";
            backgroundUi.IsBackground = true;
            backgroundUi.Start();

            _sync.WaitOne();
        }

        public HostVisual HostVisual { get; }
        public Size DesiredSize { get; private set; }
        private Dispatcher Dispatcher { get; set; }

        public void Exit()
        {
            Dispatcher.BeginInvokeShutdown(DispatcherPriority.Send);
        }

        private void CreateAndShowContent()
        {
            Dispatcher = Dispatcher.CurrentDispatcher;
            var source =
                new VisualTargetPresentationSource(HostVisual);
            _sync.Set();
            source.RootVisual = _createContent();
            DesiredSize = source.DesiredSize;
            _invalidateMeasure();

            Dispatcher.Run();
            source.Dispose();
        }
    }

    #region Private Fields

    public ThreadedVisualHelper ThreadedHelper;
    private HostVisual _hostVisual;

    #endregion Private Fields

    #region IsContentShowingProperty

    /// <summary>
    ///     Identifies the IsContentShowing dependency property.
    /// </summary>
    public static readonly DependencyProperty IsContentShowingProperty = DependencyProperty.Register(
        "IsContentShowing",
        typeof(bool),
        typeof(BackgroundVisualHost),
        new FrameworkPropertyMetadata(false, OnIsContentShowingChanged));

    /// <summary>
    ///     Gets or sets if the content is being displayed.
    /// </summary>
    public bool IsContentShowing
    {
        get => (bool)GetValue(IsContentShowingProperty);
        set => SetValue(IsContentShowingProperty, value);
    }

    private static void OnIsContentShowingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var bvh = (BackgroundVisualHost)d;

        if (bvh.CreateContent != null)
            if ((bool)e.NewValue)
                bvh.CreateContentHelper();
            else
                bvh.HideContentHelper();
    }

    #endregion IsContentShowingProperty

    #region CreateContent Property

    /// <summary>
    ///     Identifies the CreateContent dependency property.
    /// </summary>
    public static readonly DependencyProperty CreateContentProperty = DependencyProperty.Register(
        "CreateContent",
        typeof(CreateContentFunction),
        typeof(BackgroundVisualHost),
        new FrameworkPropertyMetadata(OnCreateContentChanged));

    /// <summary>
    ///     Gets or sets the function used to create the visual to display in a background thread.
    /// </summary>
    public CreateContentFunction CreateContent
    {
        get => (CreateContentFunction)GetValue(CreateContentProperty);
        set => SetValue(CreateContentProperty, value);
    }

    private static void OnCreateContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var bvh = (BackgroundVisualHost)d;

        if (bvh.IsContentShowing)
        {
            bvh.HideContentHelper();
            if (e.NewValue != null)
                bvh.CreateContentHelper();
        }
    }

    #endregion CreateContent Property
}
