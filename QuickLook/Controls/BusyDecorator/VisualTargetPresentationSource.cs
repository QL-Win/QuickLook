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

using System.Windows;
using System.Windows.Media;

namespace QuickLook.Controls.BusyDecorator;

public class VisualTargetPresentationSource : PresentationSource
{
    private readonly VisualTarget _visualTarget;
    private bool _isDisposed;

    public VisualTargetPresentationSource(HostVisual hostVisual)
    {
        _visualTarget = new VisualTarget(hostVisual);
        AddSource();
    }

    public Size DesiredSize { get; private set; }

    public override Visual RootVisual
    {
        get => _visualTarget.RootVisual;
        set
        {
            var oldRoot = _visualTarget.RootVisual;

            // Set the root visual of the VisualTarget.  This visual will
            // now be used to visually compose the scene.
            _visualTarget.RootVisual = value;

            // Tell the PresentationSource that the root visual has
            // changed.  This kicks off a bunch of stuff like the
            // Loaded event.
            RootChanged(oldRoot, value);

            // Kickoff layout...
            var rootElement = value as UIElement;
            if (rootElement != null)
            {
                rootElement.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                rootElement.Arrange(new Rect(rootElement.DesiredSize));

                DesiredSize = rootElement.DesiredSize;
            }
            else
            {
                DesiredSize = new Size(0, 0);
            }
        }
    }

    public override bool IsDisposed => _isDisposed;

    protected override CompositionTarget GetCompositionTargetCore()
    {
        return _visualTarget;
    }

    internal void Dispose()
    {
        RemoveSource();
        _isDisposed = true;
    }
}
