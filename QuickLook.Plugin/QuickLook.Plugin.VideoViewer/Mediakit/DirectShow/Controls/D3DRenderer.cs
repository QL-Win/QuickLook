using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using WPFMediaKit.Effects;

namespace WPFMediaKit.DirectShow.Controls;

/// <summary>
/// The D3DRenderer class provides basic functionality needed
/// to render a D3D surface.  This class is abstract.
/// </summary>
public abstract class D3DRenderer : FrameworkElement
{
    #region Local Instances

    /// <summary>
    /// The D3DImage used to render video
    /// </summary>
    private D3DImage m_d3dImage;

    /// <summary>
    /// The Image control that has the source
    /// to the D3DImage
    /// </summary>
    private Image m_videoImage;

    /// <summary>
    /// We keep reference to the D3D surface so
    /// we can delay loading it to avoid a black flicker
    /// when loading new media
    /// </summary>
    private IntPtr m_pBackBuffer = IntPtr.Zero;

    /// <summary>
    /// Flag to tell us if we have a new D3D
    /// Surface available
    /// </summary>
    private bool m_newSurfaceAvailable;

    /// <summary>
    /// A weak reference of D3DRenderers that have been cloned
    /// </summary>
    private readonly List<WeakReference> m_clonedD3Drenderers = new List<WeakReference>();

    /// <summary>
    /// Backing field for the RenderOnCompositionTargetRendering flag.
    /// </summary>
    private bool m_renderOnCompositionTargetRendering;

    /// <summary>
    /// Temporary storage for the RenderOnCompositionTargetRendering flag.
    /// This is used to remember the value for when the control is loaded and unloaded.
    /// </summary>
    private bool m_renderOnCompositionTargetRenderingTemp;

    /// <summary>
    /// TryLock timeout for the invalidate video image. Low values means higher UI responsivity, but more video dropped frames.
    /// </summary>
    private Duration m_invalidateVideoImageLockDuration = new Duration(TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Flag to reduce redundant calls to the AddDirtyRect when the rendering thread is busy.
    /// Int instead of bool for Interlocked support.
    /// </summary>
    private int m_videoImageInvalid = 1;

    #endregion Local Instances

    #region Dependency Properties

    #region Stretch

    public static readonly DependencyProperty StretchProperty =
        DependencyProperty.Register("Stretch", typeof(Stretch), typeof(D3DRenderer),
            new FrameworkPropertyMetadata(Stretch.Uniform,
                new PropertyChangedCallback(OnStretchChanged)));

    /// <summary>
    /// Defines what rules are applied to the stretching of the video
    /// </summary>
    public Stretch Stretch
    {
        get { return (Stretch)GetValue(StretchProperty); }
        set { SetValue(StretchProperty, value); }
    }

    private static void OnStretchChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((D3DRenderer)d).OnStretchChanged(e);
    }

    private void OnStretchChanged(DependencyPropertyChangedEventArgs e)
    {
        m_videoImage.Stretch = (Stretch)e.NewValue;
    }

    #endregion Stretch

    #region StretchDirection

    public static readonly DependencyProperty StretchDirectionProperty =
        DependencyProperty.Register("StretchDirection", typeof(StretchDirection), typeof(D3DRenderer),
            new FrameworkPropertyMetadata(StretchDirection.Both,
                new PropertyChangedCallback(OnStretchDirectionChanged)));

    /// <summary>
    /// Gets or Sets the value that indicates how the video is scaled.  This is a dependency property.
    /// </summary>
    public StretchDirection StretchDirection
    {
        get { return (StretchDirection)GetValue(StretchDirectionProperty); }
        set { SetValue(StretchDirectionProperty, value); }
    }

    private static void OnStretchDirectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((D3DRenderer)d).OnStretchDirectionChanged(e);
    }

    protected virtual void OnStretchDirectionChanged(DependencyPropertyChangedEventArgs e)
    {
        m_videoImage.StretchDirection = (StretchDirection)e.NewValue;
    }

    #endregion StretchDirection

    #region IsRenderingEnabled

    public static readonly DependencyProperty IsRenderingEnabledProperty =
        DependencyProperty.Register("IsRenderingEnabled", typeof(bool), typeof(D3DRenderer),
            new FrameworkPropertyMetadata(true));

    /// <summary>
    /// Enables or disables rendering of the video
    /// </summary>
    public bool IsRenderingEnabled
    {
        get { return (bool)GetValue(IsRenderingEnabledProperty); }
        set { SetValue(IsRenderingEnabledProperty, value); }
    }

    #endregion IsRenderingEnabled

    #region NaturalVideoHeight

    private static readonly DependencyPropertyKey NaturalVideoHeightPropertyKey
        = DependencyProperty.RegisterReadOnly("NaturalVideoHeight", typeof(int), typeof(MediaElementBase),
            new FrameworkPropertyMetadata(0));

    public static readonly DependencyProperty NaturalVideoHeightProperty
        = NaturalVideoHeightPropertyKey.DependencyProperty;

    /// <summary>
    /// Gets the natural pixel height of the current media.
    /// The value will be 0 if there is no video in the media.
    /// </summary>
    public int NaturalVideoHeight
    {
        get { return (int)GetValue(NaturalVideoHeightProperty); }
    }

    /// <summary>
    /// Internal method to set the read-only NaturalVideoHeight DP
    /// </summary>
    protected void SetNaturalVideoHeight(int value)
    {
        SetValue(NaturalVideoHeightPropertyKey, value);
    }

    #endregion NaturalVideoHeight

    #region NaturalVideoWidth

    private static readonly DependencyPropertyKey NaturalVideoWidthPropertyKey
        = DependencyProperty.RegisterReadOnly("NaturalVideoWidth", typeof(int), typeof(MediaElementBase),
            new FrameworkPropertyMetadata(0));

    public static readonly DependencyProperty NaturalVideoWidthProperty
        = NaturalVideoWidthPropertyKey.DependencyProperty;

    /// <summary>
    /// Gets the natural pixel width of the current media.
    /// The value will be 0 if there is no video in the media.
    /// </summary>
    public int NaturalVideoWidth
    {
        get { return (int)GetValue(NaturalVideoWidthProperty); }
    }

    /// <summary>
    /// Internal method to set the read-only NaturalVideoWidth DP
    /// </summary>
    protected void SetNaturalVideoWidth(int value)
    {
        SetValue(NaturalVideoWidthPropertyKey, value);
    }

    #endregion NaturalVideoWidth

    #region HasVideo

    private static readonly DependencyPropertyKey HasVideoPropertyKey
        = DependencyProperty.RegisterReadOnly("HasVideo", typeof(bool), typeof(MediaElementBase),
            new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty HasVideoProperty
        = HasVideoPropertyKey.DependencyProperty;

    /// <summary>
    /// Is true if the media contains renderable video
    /// </summary>
    public bool HasVideo
    {
        get { return (bool)GetValue(HasVideoProperty); }
    }

    /// <summary>
    /// Internal method for setting the read-only HasVideo DP
    /// </summary>
    protected void SetHasVideo(bool value)
    {
        SetValue(HasVideoPropertyKey, value);
    }

    #endregion HasVideo

    #region DeeperColor

    public static readonly DependencyProperty DeeperColorProperty =
        DependencyProperty.Register("DeeperColor", typeof(bool), typeof(D3DRenderer),
            new FrameworkPropertyMetadata(false,
                new PropertyChangedCallback(OnDeeperColorChanged)));

    public bool DeeperColor
    {
        get { return (bool)GetValue(DeeperColorProperty); }
        set { SetValue(DeeperColorProperty, value); }
    }

    private static void OnDeeperColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((D3DRenderer)d).OnDeeperColorChanged(e);
    }

    protected virtual void OnDeeperColorChanged(DependencyPropertyChangedEventArgs e)
    {
        ToggleDeeperColorEffect((bool)e.NewValue);
    }

    #endregion DeeperColor

    #endregion Dependency Properties

    #region Private Methods

    private void ToggleDeeperColorEffect(bool isEnabled)
    {
        m_videoImage.Effect = isEnabled ? new DeeperColorEffect() : null;
    }

    /// <summary>
    /// Handler for when the D3DRenderer is unloaded
    /// </summary>
    private void D3DRendererUnloaded(object sender, RoutedEventArgs e)
    {
        /* Remember what the property value was */
        m_renderOnCompositionTargetRenderingTemp = RenderOnCompositionTargetRendering;

        /* Make sure to unhook the static event hook because we are unloading */
        RenderOnCompositionTargetRendering = false;
    }

    /// <summary>
    /// Handler for when the D3DRenderer is loaded
    /// </summary>
    private void D3DRendererLoaded(object sender, RoutedEventArgs e)
    {
        /* Restore the property's value */
        RenderOnCompositionTargetRendering = m_renderOnCompositionTargetRenderingTemp;
    }

    /// <summary>
    /// Initializes the D3DRenderer control
    /// </summary>
    private void InitializeD3DVideo()
    {
        if (m_videoImage != null)
            return;

        /* Create our Image and it's D3DImage source */
        m_videoImage = new Image();
        m_d3dImage = new D3DImage();

        /* Set our default stretch value of our video */
        m_videoImage.Stretch = (Stretch)StretchProperty.DefaultMetadata.DefaultValue;
        m_videoImage.StretchDirection = (StretchDirection)StretchProperty.DefaultMetadata.DefaultValue;

        /* Our source of the video image is the D3DImage */
        m_videoImage.Source = D3DImage;

        /* Register the Image as a visual child */
        AddVisualChild(m_videoImage);

        /* Bind the horizontal alignment dp of this control to that of the video image */
        var horizontalAlignmentBinding = new Binding("HorizontalAlignment") { Source = this };
        m_videoImage.SetBinding(HorizontalAlignmentProperty, horizontalAlignmentBinding);

        /* Bind the vertical alignment dp of this control to that of the video image */
        var verticalAlignmentBinding = new Binding("VerticalAlignment") { Source = this };
        m_videoImage.SetBinding(VerticalAlignmentProperty, verticalAlignmentBinding);

        ToggleDeeperColorEffect((bool)DeeperColorProperty.DefaultMetadata.DefaultValue);
    }

    private void CompositionTargetRendering(object sender, EventArgs e)
    {
        InternalInvalidateVideoImage();
    }

    /// <summary>
    /// Sets the backbuffer for any cloned D3DRenderers
    /// </summary>
    private void SetBackBufferForClones()
    {
        var backBuffer = m_pBackBuffer;
        ForEachCloneD3DRenderer(r => r.SetBackBuffer(backBuffer));
    }

    /// <summary>
    /// Configures D3DImage with a new surface.  This happens immediately
    /// </summary>
    private void SetBackBufferInternal(IntPtr backBuffer)
    {
        /* Do nothing if we don't have a new surface available */
        if (!m_newSurfaceAvailable)
            return;

        if (!D3DImage.Dispatcher.CheckAccess())
        {
            D3DImage.Dispatcher.BeginInvoke((Action)(() => SetBackBufferInternal(backBuffer)));
            return;
        }

        /* We have this around a try/catch just in case we
         * lose the device and our Surface is invalid. The
         * try/catch may not be needed, but testing needs
         * to take place before it's removed */
        try
        {
            D3DImage.Lock();
            //When front buffer is unavailable, use software render to keep rendering.
            D3DImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, backBuffer, true);
        }
        catch
        { }
        finally
        {
            D3DImage.Unlock();
        }

        SetNaturalWidthHeight();

        /* Clear our flag, so this won't be ran again
         * until a new surface is sent */
        m_newSurfaceAvailable = false;
    }

    private void SetNaturalWidthHeight()
    {
        SetNaturalVideoHeight(m_d3dImage.PixelHeight);
        SetNaturalVideoWidth(m_d3dImage.PixelWidth);
    }

    private bool GetSetVideoImageInvalid(bool value)
    {
        int oldValue = Interlocked.Exchange(ref m_videoImageInvalid, value ? 1 : 0);
        return oldValue == 1;
    }

    /// <summary>
    /// Invalidates any possible cloned renderer we may have
    /// </summary>
    private void InvalidateClonedVideoImages()
    {
        ForEachCloneD3DRenderer(r => r.InvalidateVideoImage());
    }

    private void ForEachCloneD3DRenderer(Action<D3DRenderer> action)
    {
        lock (m_clonedD3Drenderers)
        {
            bool needClean = false;
            foreach (var rendererRef in m_clonedD3Drenderers)
            {
                var renderer = rendererRef.Target as D3DRenderer;
                if (renderer != null)
                    action(renderer);
                else
                    needClean = true;
            }

            if (needClean)
                CleanZombieRenderers();
        }
    }

    /// <summary>
    /// Cleans up any dead references we may have to any cloned renderers
    /// </summary>
    private void CleanZombieRenderers()
    {
        lock (m_clonedD3Drenderers)
        {
            m_clonedD3Drenderers.RemoveAll(c => !c.IsAlive);
        }
    }

    /// <summary>
    /// Used as a clone for a D3DRenderer
    /// </summary>
    private class ClonedD3DRenderer : D3DRenderer
    { }

    #endregion Private Methods

    #region Protected Methods

    protected override Size MeasureOverride(Size availableSize)
    {
        m_videoImage.Measure(availableSize);
        return m_videoImage.DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        m_videoImage.Arrange(new Rect(finalSize));
        return finalSize;
    }

    protected override int VisualChildrenCount
    {
        get
        {
            return 1;
        }
    }

    protected override Visual GetVisualChild(int index)
    {
        if (index > 0)
            throw new IndexOutOfRangeException();

        return m_videoImage;
    }

    protected D3DImage D3DImage
    {
        get
        {
            return m_d3dImage;
        }
    }

    protected Image VideoImage
    {
        get
        {
            return m_videoImage;
        }
    }

    /// <summary>
    /// Renders the video with WPF's rendering using the CompositionTarget.Rendering event
    /// </summary>
    protected bool RenderOnCompositionTargetRendering
    {
        get
        {
            return m_renderOnCompositionTargetRendering;
        }
        set
        {
            /* If it is being set to true and it was previously false
             * then hook into the event */
            if (value && !m_renderOnCompositionTargetRendering)
                CompositionTarget.Rendering += CompositionTargetRendering;
            else if (!value)
                CompositionTarget.Rendering -= CompositionTargetRendering;

            m_renderOnCompositionTargetRendering = value;
            m_renderOnCompositionTargetRenderingTemp = value;
        }
    }

    /// <summary>
    /// Configures D3DImage with a new surface.  The back buffer is
    /// not set until we actually receive a frame, this way we
    /// can avoid a black flicker between media changes
    /// </summary>
    /// <param name="backBuffer">The unmanaged pointer to the Direct3D Surface</param>
    protected void SetBackBuffer(IntPtr backBuffer)
    {
        /* We only do this if target rendering is enabled because we must use an Invoke
         * instead of a BeginInvoke to keep the Surfaces in sync and Invoke could be dangerous
         * in other situations */
        if (RenderOnCompositionTargetRendering)
        {
            if (!D3DImage.Dispatcher.CheckAccess())
            {
                D3DImage.Dispatcher.Invoke((Action)(() => SetBackBuffer(backBuffer)), DispatcherPriority.Render);
                return;
            }
        }

        /* Flag a new surface */
        m_newSurfaceAvailable = true;
        m_pBackBuffer = backBuffer;

        /* Make a special case for target rendering */
        if (RenderOnCompositionTargetRendering || m_pBackBuffer == IntPtr.Zero)
        {
            SetBackBufferInternal(m_pBackBuffer);
        }

        SetBackBufferForClones();
    }

    protected void InvalidateVideoImage()
    {
        GetSetVideoImageInvalid(true);

        if (!m_renderOnCompositionTargetRendering)
            InternalInvalidateVideoImage();
    }

    /// <summary>
    /// Invalidates the entire Direct3D image, notifying WPF to redraw
    /// </summary>
    protected void InternalInvalidateVideoImage()
    {
        /* Ensure we run on the correct Dispatcher */
        if (!D3DImage.Dispatcher.CheckAccess())
        {
            D3DImage.Dispatcher.BeginInvoke((Action)(() => InternalInvalidateVideoImage()));
            return;
        }

        /* If there is a new Surface to set,
         * this method will do the trick */
        SetBackBufferInternal(m_pBackBuffer);

        // may save a few AddDirtyRect calls when the rendering thread is too busy
        // or RenderOnCompositionTargetRendering is set but the video is not playing
        bool invalid = GetSetVideoImageInvalid(false);
        if (!invalid)
            return;

        /* Only render the video image if possible, or if IsRenderingEnabled is true */
        if (IsRenderingEnabled && m_pBackBuffer != IntPtr.Zero)
        {
            try
            {
                if (!D3DImage.TryLock(InvalidateVideoImageLockDuration))
                    return;
                /* Invalidate the entire image */
                D3DImage.AddDirtyRect(new Int32Rect(0, /* Left */
                                                    0, /* Top */
                                                    D3DImage.PixelWidth, /* Width */
                                                    D3DImage.PixelHeight /* Height */));
            }
            catch (Exception)
            { }
            finally
            {
                D3DImage.Unlock();
            }
        }

        /* Invalidate all of our cloned D3DRenderers */
        InvalidateClonedVideoImages();
    }

    #endregion Protected Methods

    protected D3DRenderer()
    {
        InitializeD3DVideo();

        /* Hook into the framework events */
        Loaded += D3DRendererLoaded;
        Unloaded += D3DRendererUnloaded;
    }

    /// <summary>
    /// TryLock timeout for the invalidate video image. Low values means higher UI responsivity, but more video dropped frames.
    /// </summary>
    public Duration InvalidateVideoImageLockDuration
    {
        get { return m_invalidateVideoImageLockDuration; }
        set
        {
            if (value == null)
                throw new ArgumentNullException("InvalidateVideoImageLockDuration");
            m_invalidateVideoImageLockDuration = value;
        }
    }

    /// <summary>
    /// Creates a clone of the D3DRenderer.  This is a work for the visual
    /// brush not working cross-threaded
    /// </summary>
    /// <returns></returns>
    public D3DRenderer CloneD3DRenderer()
    {
        var renderer = new ClonedD3DRenderer();

        lock (m_clonedD3Drenderers)
        {
            m_clonedD3Drenderers.Add(new WeakReference(renderer));
        }

        renderer.SetBackBuffer(m_pBackBuffer);
        return renderer;
    }

    /// <summary>
    /// Creates a cloned D3DImage image of the current video frame.
    /// Return null in case of the frame is not valid.
    /// The image can be used thread-safe.
    /// </summary>
    public D3DImage CloneSingleFrameD3DImage()
    {
        D3DImage d3d = new D3DImage();

        /* We have this around a try/catch just in case we
         * lose the device and our Surface is invalid. The
         * try/catch may not be needed, but testing needs
         * to take place before it's removed */
        try
        {
            D3DImageUtils.SetBackBufferWithLock(d3d, m_pBackBuffer);
        }
        catch (Exception)
        {
            return null;
        }
        return d3d;
    }
}

public static class D3DImageUtils
{
    public static void AddDirtyRectAll(D3DImage d3d)
    {
        d3d.AddDirtyRect(new Int32Rect(0, 0, d3d.PixelWidth, d3d.PixelHeight));
    }

    /// <summary>
    /// Convenient method for making frame shots.
    /// For the exceptions, see <see cref="D3DImage.SetBackBuffer(D3DResourceType, IntPtr)". />
    /// </summary>
    public static void SetBackBufferWithLock(D3DImage d3d, IntPtr backBuffer)
    {
        try
        {
            d3d.Lock();
            d3d.SetBackBuffer(D3DResourceType.IDirect3DSurface9, backBuffer);
            // necessary when rendering on WPF screen only
            AddDirtyRectAll(d3d);
        }
        finally
        {
            d3d.Unlock();
        }
    }
}
