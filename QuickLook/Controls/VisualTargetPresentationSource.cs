using System.Windows;
using System.Windows.Media;

namespace QuickLook.Controls
{
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
}