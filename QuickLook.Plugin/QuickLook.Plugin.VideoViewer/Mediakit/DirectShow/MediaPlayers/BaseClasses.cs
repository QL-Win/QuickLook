using DirectShowLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using WPFMediaKit.MediaFoundation;
using WPFMediaKit.MediaFoundation.Interop;
using WPFMediaKit.Threading;
using Size = System.Windows.Size;

namespace WPFMediaKit.DirectShow.MediaPlayers;

public enum MediaState
{
    Manual,
    Play,
    Stop,
    Close,
    Pause
}

public enum PlayerState
{
    Closed,
    Playing,
    Paused,
    Stopped,
    SteppingFrames
}

/// <summary>
/// The types of position formats that
/// are available for seeking media
/// </summary>
public enum MediaPositionFormat
{
    MediaTime,
    Frame,
    Byte,
    Field,
    Sample,
    None
}

/// <summary>
/// Delegate signature to notify of a new surface
/// </summary>
/// <param name="sender">The sender of the event</param>
/// <param name="pSurface">The pointer to the D3D surface</param>
public delegate void NewAllocatorSurfaceDelegate(object sender, IntPtr pSurface);

/// <summary>
/// The arguments that store information about a failed media attempt
/// </summary>
public class MediaFailedEventArgs : EventArgs
{
    public MediaFailedEventArgs(string message, Exception exception)
    {
        Message = message;
        Exception = exception;
    }

    public Exception Exception { get; protected set; }
    public string Message { get; protected set; }
}

/// <summary>
/// The custom allocator interface.  All custom allocators need
/// to implement this interface.
/// </summary>
public interface ICustomAllocator : IDisposable
{
    /// <summary>
    /// Invokes when a new frame has been allocated
    /// to a surface
    /// </summary>
    event Action NewAllocatorFrame;

    /// <summary>
    /// Invokes when a new surface has been allocated
    /// </summary>
    event NewAllocatorSurfaceDelegate NewAllocatorSurface;
}

[ComImport, Guid("FA10746C-9B63-4b6c-BC49-FC300EA5F256")]
internal class EnhancedVideoRenderer
{
}

/// <summary>
/// A low level window class that is used to provide interop with libraries
/// that require an hWnd
/// </summary>
public class HiddenWindow : NativeWindow
{
    public delegate IntPtr WndProcHookDelegate(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled);

    private readonly List<WndProcHookDelegate> m_handlerlist = new List<WndProcHookDelegate>();

    public void AddHook(WndProcHookDelegate method)
    {
        if (m_handlerlist.Contains(method))
            return;

        lock (((System.Collections.ICollection)m_handlerlist).SyncRoot)
            m_handlerlist.Add(method);
    }

    public void RemoveHook(WndProcHookDelegate method)
    {
        lock (((System.Collections.ICollection)m_handlerlist).SyncRoot)
            m_handlerlist.Remove(method);
    }

    /// <summary>
    /// Invokes the windows procedure associated to this window
    /// </summary>
    /// <param name="m">The window message to send to window</param>
    protected override void WndProc(ref Message m)
    {
        bool isHandled = false;

        lock (((System.Collections.ICollection)m_handlerlist).SyncRoot)
        {
            foreach (WndProcHookDelegate method in m_handlerlist)
            {
                method.Invoke(m.HWnd, m.Msg, m.WParam, m.LParam, ref isHandled);
                if (isHandled)
                    break;
            }
        }

        base.WndProc(ref m);
    }
}

/// <summary>
/// Specifies different types of DirectShow
/// Video Renderers
/// </summary>
public enum VideoRendererType
{
    VideoMixingRenderer9 = 0,
    EnhancedVideoRenderer
}

/// <summary>
/// The MediaPlayerBase is a base class to build raw, DirectShow based players.
/// It inherits from DispatcherObject to allow easy communication with COM objects
/// from different apartment thread models.
/// </summary>
public abstract class MediaPlayerBase : WorkDispatcherObject
{
    [DllImport("user32.dll", SetLastError = false)]
    private static extern IntPtr GetDesktopWindow();

    /// <summary>
    /// A static value to hold a count for all graphs.  Each graph
    /// has it's own value that it uses and is updated by the
    /// GraphInstanceCookie property in the get method
    /// </summary>
    private static int m_graphInstances;

    /// <summary>
    /// The custom windows message constant for graph events
    /// </summary>
    private const int WM_GRAPH_NOTIFY = 0x0400 + 13;

    /// <summary>
    /// One second in 100ns units
    /// </summary>
    public const long DSHOW_ONE_SECOND_UNIT = 10000000;

    /// <summary>
    /// The IBasicAudio volume value for silence
    /// </summary>
    private const int DSHOW_VOLUME_SILENCE = -10000;

    /// <summary>
    /// The IBasicAudio volume value for full volume
    /// </summary>
    private const int DSHOW_VOLUME_MAX = 0;

    /// <summary>
    /// The IBasicAudio balance max absolute value
    /// </summary>
    private const int DSHOW_BALACE_MAX_ABS = 10000;

    /// <summary>
    /// Rate which our DispatcherTimer polls the graph
    /// </summary>
    private const int DSHOW_TIMER_POLL_MS = 33;

    /// <summary>
    /// UserId value for the VMR9 Allocator - Not entirely useful
    /// for this application of the VMR
    /// </summary>
    private readonly IntPtr m_userId = new IntPtr(unchecked((int)0xDEADBEEF));

    /// <summary>
    /// Static lock.  Seems multiple EVR controls instantiated at the same time crash
    /// </summary>
    private static readonly object m_videoRendererInitLock = new object();

    /// <summary>
    /// DirectShow interface for controlling audio
    /// functions such as volume and balance
    /// </summary>
    private IBasicAudio m_basicAudio;

    /// <summary>
    /// The custom DirectShow allocator
    /// </summary>
    private ICustomAllocator m_customAllocator;

    /// <summary>
    /// The DirectShow filter graph reference
    /// </summary>
    private IFilterGraph m_graph;

    /// <summary>
    /// The hWnd pointer we use for D3D stuffs
    /// </summary>
    private HiddenWindow m_window;

    /// <summary>
    /// The DirectShow interface for controlling the
    /// filter graph.  This provides, Play, Pause, Stop, etc
    /// functionality.
    /// </summary>
    private IMediaControl m_mediaControl;

    /// <summary>
    /// The DirectShow interface for getting events
    /// that occur in the FilterGraph.
    /// </summary>
    private IMediaEventEx m_mediaEvent;

    /// <summary>
    /// Flag for if our media has video
    /// </summary>
    private bool m_hasVideo;

    /// <summary>
    /// The natural video pixel height, if applicable
    /// </summary>
    private int m_naturalVideoHeight;

    /// <summary>
    /// The natural video pixel width, if applicable
    /// </summary>
    private int m_naturalVideoWidth;

    /// <summary>
    /// Our Win32 timer to poll the DirectShow graph
    /// </summary>
    private System.Timers.Timer m_timer;

    /// <summary>
    /// The current state of the player
    /// </summary>
    private PlayerState m_playerState = PlayerState.Closed;

    /// <summary>
    /// This objects last stand
    /// </summary>
    ~MediaPlayerBase()
    {
        Dispose();
    }

    /// <summary>
    /// The global instance Id of the graph.  We use this
    /// for the WndProc callback method.
    /// </summary>
    private int? m_graphInstanceId;

    /// <summary>
    /// The globally unqiue identifier of the graph
    /// </summary>
    protected int GraphInstanceId
    {
        get
        {
            if (m_graphInstanceId != null)
                return m_graphInstanceId.Value;

            /* Increment our static value and store the current
             * instance id of our player graph */
            m_graphInstanceId = Interlocked.Increment(ref m_graphInstances);

            return m_graphInstanceId.Value;
        }
    }

    /// <summary>
    /// Helper function to get a valid hWnd to
    /// use with DirectShow and Direct3D
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    private void GetMainWindowHwndHelper()
    {
        if (m_window == null)
            m_window = new HiddenWindow();
        else
            return;

        if (m_window.Handle == IntPtr.Zero)
        {
            lock (m_window)
            {
                m_window.CreateHandle(new CreateParams());
            }
        }
    }

    protected virtual HiddenWindow HwndHelper
    {
        get
        {
            if (m_window != null)
                return m_window;

            GetMainWindowHwndHelper();

            return m_window;
        }
    }

    /// <summary>
    /// Is true if the media contains renderable video
    /// </summary>
    public virtual bool HasVideo
    {
        get
        {
            return m_hasVideo;
        }
        protected set
        {
            m_hasVideo = value;
        }
    }

    /// <summary>
    /// Gets the natural pixel width of the current media.
    /// The value will be 0 if there is no video in the media.
    /// </summary>
    public virtual int NaturalVideoWidth
    {
        get
        {
            VerifyAccess();
            return m_naturalVideoWidth;
        }
        protected set
        {
            VerifyAccess();
            m_naturalVideoWidth = value;
        }
    }

    /// <summary>
    /// Gets the natural pixel height of the current media.
    /// The value will be 0 if there is no video in the media.
    /// </summary>
    public virtual int NaturalVideoHeight
    {
        get
        {
            VerifyAccess();
            return m_naturalVideoHeight;
        }
        protected set
        {
            VerifyAccess();
            m_naturalVideoHeight = value;
        }
    }

    /// <summary>
    /// Gets or sets the audio volume.  Specifies the volume, as a
    /// number from 0 to 1.  Full volume is 1, and 0 is silence.
    /// </summary>
    public virtual double Volume
    {
        get
        {
            VerifyAccess();

            /* Check if we even have an
             * audio interface */
            if (m_basicAudio == null)
                return 0;

            int dShowVolume;

            /* Get the current volume value from the interface */
            m_basicAudio.get_Volume(out dShowVolume);

            /* Do calulations to convert to a base of 0 for silence */
            dShowVolume -= DSHOW_VOLUME_SILENCE;
            return (double)dShowVolume / -DSHOW_VOLUME_SILENCE;
        }
        set
        {
            VerifyAccess();

            /* Check if we even have an
             * audio interface */
            if (m_basicAudio == null)
                return;

            if (value <= 0) /* Value should not be negative or else we treat as silence */
                m_basicAudio.put_Volume(DSHOW_VOLUME_SILENCE);
            else if (value >= 1)/* Value should not be greater than one or else we treat as maximum volume */
                m_basicAudio.put_Volume(DSHOW_VOLUME_MAX);
            else
            {
                /* With the IBasicAudio interface, sound is DSHOW_VOLUME_SILENCE
                 * for silence and DSHOW_VOLUME_MAX for full volume
                 * so we calculate that here based off an input of 0 of silence and 1.0
                 * for full audio */
                int dShowVolume = (int)((1 - value) * DSHOW_VOLUME_SILENCE);
                m_basicAudio.put_Volume(dShowVolume);
            }
        }
    }

    /// <summary>
    /// Gets or sets the balance on the audio.
    /// The value can range from -1 to 1. The value -1 means the right channel is attenuated by 100 dB
    /// and is effectively silent. The value 1 means the left channel is silent. The neutral value is 0,
    /// which means that both channels are at full volume. When one channel is attenuated, the other
    /// remains at full volume.
    /// </summary>
    public virtual double Balance
    {
        get
        {
            VerifyAccess();

            /* Check if we even have an
             * audio interface */
            if (m_basicAudio == null)
                return 0;

            int balance;

            /* Get the interface supplied balance value */
            m_basicAudio.get_Balance(out balance);

            /* Calc and return the balance based on 0 == silence */
            return (double)balance / DSHOW_BALACE_MAX_ABS;
        }
        set
        {
            VerifyAccess();

            /* Check if we even have an
             * audio interface */
            if (m_basicAudio == null)
                return;

            /* Calc the dshow balance value */
            int balance = (int)value * DSHOW_BALACE_MAX_ABS;

            m_basicAudio.put_Balance(balance);
        }
    }

    /// <summary>
    /// Get the current state of the player
    /// </summary>
    public virtual PlayerState PlayerState
    {
        get { return this.m_playerState; }
        protected set
        {
            PlayerState oldVal = m_playerState;
            m_playerState = value;

            if (PlayerStateChanged != null && oldVal != value)
                PlayerStateChanged(oldVal, value);
        }
    }

    /// <summary>
    /// Event notifies when there is a new video frame
    /// to be rendered
    /// </summary>
    public event Action NewAllocatorFrame;

    /// <summary>
    /// Event notifies when there is a new surface allocated
    /// </summary>
    public event NewAllocatorSurfaceDelegate NewAllocatorSurface;

    /// <summary>
    /// Event notifies when the player changes state
    /// </summary>
    public event Action<PlayerState, PlayerState> PlayerStateChanged;

    /// <summary>
    /// Frees any remaining memory
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        //GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Part of the dispose pattern
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        //if (m_disposed)
        //    return;

        if (!disposing)
            return;

        if (m_window != null)
        {
            m_window.RemoveHook(WndProcHook);
            m_window.DestroyHandle();
            m_window = null;
        }

        if (m_timer != null)
            m_timer.Dispose();

        m_timer = null;

        if (CheckAccess())
        {
            FreeResources();
            Dispatcher.BeginInvokeShutdown();
        }
        else
        {
            Dispatcher.BeginInvoke((Action)delegate
            {
                FreeResources();
                Dispatcher.BeginInvokeShutdown();
            });
        }
    }

    /// <summary>
    /// Polls the graph for various data about the media that is playing
    /// </summary>
    protected virtual void OnGraphTimerTick()
    {
    }

    /// <summary>
    /// Is called when a new media event code occurs on the graph
    /// </summary>
    /// <param name="code">The event code that occured</param>
    /// <param name="param1">The first parameter sent by the graph</param>
    /// <param name="param2">The second parameter sent by the graph</param>
    protected virtual void OnMediaEvent(EventCode code, IntPtr param1, IntPtr param2)
    {
        switch (code)
        {
            case EventCode.Complete:
                InvokeMediaEnded(null);
                StopGraphPollTimer();
                break;

            case EventCode.Paused:
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// Starts the graph polling timer to update possibly needed
    /// things like the media position
    /// </summary>
    protected void StartGraphPollTimer()
    {
        if (m_timer == null)
        {
            m_timer = new System.Timers.Timer();
            m_timer.Interval = DSHOW_TIMER_POLL_MS;
            m_timer.Elapsed += TimerElapsed;
        }

        m_timer.Enabled = true;

        /* Make sure we get windows messages */
        AddWndProcHook();
    }

    private void ProcessGraphEvents()
    {
        Dispatcher.BeginInvoke((Action)delegate
        {
            if (m_mediaEvent != null)
            {
                IntPtr param1;
                IntPtr param2;
                EventCode code;

                /* Get all the queued events from the interface */
                while (m_mediaEvent.GetEvent(out code, out param1, out param2, 0) == 0)
                {
                    /* Handle anything for this event code */
                    OnMediaEvent(code, param1, param2);

                    /* Free everything..we only need the code */
                    m_mediaEvent.FreeEventParams(code, param1, param2);
                }
            }
        });
    }

    private void TimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
        Dispatcher.BeginInvoke((Action)delegate
        {
            ProcessGraphEvents();
            OnGraphTimerTick();
        });
    }

    /// <summary>
    /// Stops the graph polling timer
    /// </summary>
    protected void StopGraphPollTimer()
    {
        if (m_timer != null)
        {
            m_timer.Stop();
            m_timer.Dispose();
            m_timer = null;
        }

        /* Stop listening to windows messages */
        RemoveWndProcHook();
    }

    /// <summary>
    /// Removes our hook that listens to windows messages
    /// </summary>
    private void RemoveWndProcHook()
    {
        /* Make sure to stop our IMediaEventEx also */
        UnsetMediaEventExNotifyWindow();
        //HwndHelper.RemoveHook(WndProcHook);
    }

    /// <summary>
    /// Adds a hook that listens to windows messages
    /// </summary>
    private void AddWndProcHook()
    {
        // HwndHelper.AddHook(WndProcHook);
    }

    /// <summary>
    /// Receives windows messages.  This is primarily used to get
    /// events that happen on our graph
    /// </summary>
    /// <param name="hwnd">The window handle</param>
    /// <param name="msg">The message Id</param>
    /// <param name="wParam">The message's wParam value</param>
    /// <param name="lParam">The message's lParam value</param>
    /// <param name="handled">A value that indicates whether the message was handled. Set the value to true if the message was handled; otherwise, false. </param>
    private IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        ProcessGraphEvents();

        return IntPtr.Zero;
    }

    /// <summary>
    /// Unhooks the IMediaEventEx from the notification hWnd
    /// </summary>
    private void UnsetMediaEventExNotifyWindow()
    {
        if (m_mediaEvent == null)
            return;

        /* Setting the notify window to IntPtr.Zero unsubscribes the events */
        //int hr = m_mediaEvent.SetNotifyWindow(IntPtr.Zero, WM_GRAPH_NOTIFY, (IntPtr)GraphInstanceId);
    }

    /// <summary>
    /// Sets the MediaEventEx interface
    /// </summary>
    private void SetMediaEventExInterface(IMediaEventEx mediaEventEx)
    {
        m_mediaEvent = mediaEventEx;

        //int hr = m_mediaEvent.SetNotifyWindow(HwndHelper.Handle, WM_GRAPH_NOTIFY, (IntPtr)GraphInstanceId);
    }

    /// <summary>
    /// Configures all general DirectShow interfaces that the
    /// FilterGraph supplies.
    /// </summary>
    /// <param name="graph">The FilterGraph to setup</param>
    protected virtual void SetupFilterGraph(IFilterGraph graph)
    {
        m_graph = graph;

        /* Setup the interfaces and query basic information
         * on the graph that is passed */
        SetBasicAudioInterface(m_graph as IBasicAudio);
        SetMediaControlInterface(m_graph as IMediaControl);
        SetMediaEventExInterface(m_graph as IMediaEventEx);
    }

    /// <summary>
    /// Sets the MediaControl interface
    /// </summary>
    private void SetMediaControlInterface(IMediaControl mediaControl)
    {
        m_mediaControl = mediaControl;
    }

    /// <summary>
    /// Sets the basic audio interface for controlling
    /// volume and balance
    /// </summary>
    protected void SetBasicAudioInterface(IBasicAudio basicAudio)
    {
        m_basicAudio = basicAudio;
    }

    /// <summary>
    /// Notifies when the media has successfully been opened
    /// </summary>
    public event Action MediaOpened;

    /// <summary>
    /// Notifies when the media has been closed
    /// </summary>
    public event Action MediaClosed;

    /// <summary>
    /// Notifies when the media has failed and produced an exception
    /// </summary>
    public event EventHandler<MediaFailedEventArgs> MediaFailed;

    /// <summary>
    /// Notifies when the media has completed
    /// </summary>
    public event Action MediaEnded;

    /// <summary>
    /// Registers the custom allocator and hooks into it's supplied events
    /// </summary>
    protected void RegisterCustomAllocator(ICustomAllocator allocator)
    {
        FreeCustomAllocator();

        if (allocator == null)
            return;

        m_customAllocator = allocator;

        m_customAllocator.NewAllocatorFrame += CustomAllocatorNewAllocatorFrame;
        m_customAllocator.NewAllocatorSurface += CustomAllocatorNewAllocatorSurface;
    }

    /// <summary>
    /// Local event handler for the custom allocator's new surface event
    /// </summary>
    private void CustomAllocatorNewAllocatorSurface(object sender, IntPtr pSurface)
    {
        InvokeNewAllocatorSurface(pSurface);
    }

    /// <summary>
    /// Local event handler for the custom allocator's new frame event
    /// </summary>
    private void CustomAllocatorNewAllocatorFrame()
    {
        InvokeNewAllocatorFrame();
    }

    /// <summary>
    /// Disposes of the current allocator
    /// </summary>
    protected void FreeCustomAllocator()
    {
        if (m_customAllocator == null)
            return;

        m_customAllocator.NewAllocatorFrame -= CustomAllocatorNewAllocatorFrame;
        m_customAllocator.NewAllocatorSurface -= CustomAllocatorNewAllocatorSurface;

        m_customAllocator.Dispose();

        if (Marshal.IsComObject(m_customAllocator))
            Marshal.ReleaseComObject(m_customAllocator);

        m_customAllocator = null;
    }

    /// <summary>
    /// Resets the local graph resources to their
    /// default settings
    /// </summary>
    private void ResetLocalGraphResources()
    {
        m_graph = null;

        if (m_basicAudio != null)
            Marshal.ReleaseComObject(m_basicAudio);
        m_basicAudio = null;

        if (m_mediaControl != null)
            Marshal.ReleaseComObject(m_mediaControl);
        m_mediaControl = null;

        if (m_mediaEvent != null)
            Marshal.ReleaseComObject(m_mediaEvent);
        m_mediaEvent = null;
    }

    /// <summary>
    /// Frees any allocated or unmanaged resources
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    protected virtual void FreeResources()
    {
        StopGraphPollTimer();
        ResetLocalGraphResources();
        FreeCustomAllocator();
    }

    /// <summary>
    /// Creates a new renderer and configures it with a custom allocator
    /// </summary>
    /// <param name="rendererType">The type of renderer we wish to choose</param>
    /// <param name="graph">The DirectShow graph to add the renderer to</param>
    /// <param name="streamCount">Number of input pins for the renderer</param>
    /// <returns>An initialized DirectShow renderer</returns>
    protected IBaseFilter CreateVideoRenderer(VideoRendererType rendererType, IGraphBuilder graph, int streamCount)
    {
        IBaseFilter renderer;

        switch (rendererType)
        {
            case VideoRendererType.VideoMixingRenderer9:
                renderer = CreateVideoMixingRenderer9(graph, streamCount);
                break;

            case VideoRendererType.EnhancedVideoRenderer:
                renderer = CreateEnhancedVideoRenderer(graph, streamCount);
                break;

            default:
                throw new ArgumentOutOfRangeException("rendererType");
        }

        return renderer;
    }

    /// <summary>
    /// Creates a new renderer and configures it with a custom allocator
    /// </summary>
    /// <param name="rendererType">The type of renderer we wish to choose</param>
    /// <param name="graph">The DirectShow graph to add the renderer to</param>
    /// <returns>An initialized DirectShow renderer</returns>
    protected IBaseFilter CreateVideoRenderer(VideoRendererType rendererType, IGraphBuilder graph)
    {
        return CreateVideoRenderer(rendererType, graph, 1);
    }

    /// <summary>
    /// Creates an instance of the EVR
    /// </summary>
    private IBaseFilter CreateEnhancedVideoRenderer(IGraphBuilder graph, int streamCount)
    {
        EvrPresenter presenter;
        IBaseFilter filter;

        lock (m_videoRendererInitLock)
        {
            var evr = new EnhancedVideoRenderer();
            filter = evr as IBaseFilter;

            int hr = graph.AddFilter(filter, string.Format("Renderer: {0}", VideoRendererType.EnhancedVideoRenderer));
            DsError.ThrowExceptionForHR(hr);

            /* QueryInterface for the IMFVideoRenderer */
            var videoRenderer = filter as IMFVideoRenderer;

            if (videoRenderer == null)
                throw new WPFMediaKitException("Could not QueryInterface for the IMFVideoRenderer");

            /* Create a new EVR presenter */
            presenter = EvrPresenter.CreateNew();

            /* Initialize the EVR renderer with the custom video presenter */
            hr = videoRenderer.InitializeRenderer(null, presenter.VideoPresenter);
            DsError.ThrowExceptionForHR(hr);

            var presenterSettings = presenter.VideoPresenter as IEVRPresenterSettings;
            if (presenterSettings == null)
                throw new WPFMediaKitException("Could not QueryInterface for the IEVRPresenterSettings");

            presenterSettings.SetBufferCount(3);

            /* Use our interop hWnd */
            IntPtr handle = GetDesktopWindow();//HwndHelper.Handle;

            /* QueryInterface the IMFVideoDisplayControl */
            var displayControl = presenter.VideoPresenter as IMFVideoDisplayControl;

            if (displayControl == null)
                throw new WPFMediaKitException("Could not QueryInterface the IMFVideoDisplayControl");

            /* Configure the presenter with our hWnd */
            hr = displayControl.SetVideoWindow(handle);
            DsError.ThrowExceptionForHR(hr);

            var filterConfig = filter as IEVRFilterConfig;

            if (filterConfig != null)
                filterConfig.SetNumberOfStreams(streamCount);
        }

        RegisterCustomAllocator(presenter);

        return filter;
    }

    /// <summary>
    /// Creates a new VMR9 renderer and configures it with an allocator.
    /// <para>
    /// COMException is transalted to the WPFMediaKitException.
    /// </para>
    /// </summary>
    /// <returns>An initialized DirectShow VMR9 renderer.</returns>
    /// <exception cref="WPFMediaKitException">When creating of VMR9 fails.</exception>
    private IBaseFilter CreateVideoMixingRenderer9(IGraphBuilder graph, int streamCount)
    {
        try
        {
            return CreateVideoMixingRenderer9Inner(graph, streamCount);
        }
        catch (COMException ex)
        {
            throw new WPFMediaKitException("Could not create VMR9. " + Vmr9Allocator.VMR9_ERROR, ex);
        }
    }

    /// <summary>
    /// Creates a new VMR9 renderer and configures it with an allocator.
    /// </summary>
    /// <returns>An initialized DirectShow VMR9 renderer.</returns>
    /// <exception cref="COMException">When creating of VMR9 fails.</exception>
    /// <exception cref="WPFMediaKitException">When creating of VMR9 fails.</exception>
    private IBaseFilter CreateVideoMixingRenderer9Inner(IGraphBuilder graph, int streamCount)
    {
        IBaseFilter vmr9 = new VideoMixingRenderer9() as IBaseFilter;
        var filterConfig = vmr9 as IVMRFilterConfig9;
        if (filterConfig == null)
            throw new WPFMediaKitException("Could not query VMR9 filter configuration. " + Vmr9Allocator.VMR9_ERROR);

        /* We will only have one video stream connected to the filter */
        int hr = filterConfig.SetNumberOfStreams(streamCount);
        DsError.ThrowExceptionForHR(hr);

        /* Setting the renderer to "Renderless" mode
         * sounds counter productive, but its what we
         * need to do for setting up a custom allocator */
        hr = filterConfig.SetRenderingMode(VMR9Mode.Renderless);
        DsError.ThrowExceptionForHR(hr);

        /* Query the allocator interface */
        var vmrSurfAllocNotify = vmr9 as IVMRSurfaceAllocatorNotify9;
        if (vmrSurfAllocNotify == null)
            throw new WPFMediaKitException("Could not query the VMR surface allocator. " + Vmr9Allocator.VMR9_ERROR);

        var allocator = new Vmr9Allocator();

        /* We supply our custom allocator to the renderer */
        hr = vmrSurfAllocNotify.AdviseSurfaceAllocator(m_userId, allocator);
        DsError.ThrowExceptionForHR(hr);

        hr = allocator.AdviseNotify(vmrSurfAllocNotify);
        DsError.ThrowExceptionForHR(hr);

        RegisterCustomAllocator(allocator);

        hr = graph.AddFilter(vmr9,
                             string.Format("Renderer: {0}", VideoRendererType.VideoMixingRenderer9));
        DsError.ThrowExceptionForHR(hr);

        return vmr9;
    }

    /// <summary>
    /// Plays the media
    /// </summary>
    public virtual void Play()
    {
        VerifyAccess();

        if (m_basicAudio != null)
        {
            //Balance = Balance;
            //Volume = Volume;
        }

        if (m_mediaControl != null)
        {
            m_mediaControl.Run();
            StartGraphPollTimer();
            PlayerState = PlayerState.Playing;
        }
    }

    /// <summary>
    /// Stops the media
    /// </summary>
    public virtual void Stop()
    {
        VerifyAccess();

        StopInternal();
    }

    /// <summary>
    /// Stops the media, but does not VerifyAccess() on
    /// the Dispatcher.  This can be used by destructors
    /// because it happens on another thread and our
    /// DirectShow graph and COM run in MTA
    /// </summary>
    protected void StopInternal()
    {
        if (m_mediaControl != null)
        {
            m_mediaControl.Stop();
            FilterState filterState;
            m_mediaControl.GetState(0, out filterState);

            while (filterState != FilterState.Stopped)
                m_mediaControl.GetState(2, out filterState);

            PlayerState = PlayerState.Stopped;
        }
    }

    /// <summary>
    /// Closes the media and frees its resources
    /// </summary>
    public virtual void Close()
    {
        VerifyAccess();
        StopInternal();
        FreeResources();
        PlayerState = PlayerState.Closed;
    }

    /// <summary>
    /// Pauses the media
    /// </summary>
    public virtual void Pause()
    {
        VerifyAccess();

        if (m_mediaControl != null)
        {
            m_mediaControl.Pause();
            PlayerState = PlayerState.Paused;
        }
    }

    #region Event Invokes

    /// <summary>
    /// Invokes the MediaEnded event, notifying any subscriber that
    /// media has reached the end
    /// </summary>
    protected void InvokeMediaEnded(EventArgs e)
    {
        var mediaEndedHandler = MediaEnded;
        if (mediaEndedHandler != null)
            mediaEndedHandler();
    }

    /// <summary>
    /// Invokes the MediaOpened event, notifying any subscriber that
    /// media has successfully been opened
    /// </summary>
    protected void InvokeMediaOpened()
    {
        /* This is generally a good place to start
         * our polling timer */
        StartGraphPollTimer();

        var mediaOpenedHandler = MediaOpened;
        if (mediaOpenedHandler != null)
            mediaOpenedHandler();
    }

    /// <summary>
    /// Invokes the MediaClosed event, notifying any subscriber that
    /// the opened media has been closed
    /// </summary>
    protected void InvokeMediaClosed(EventArgs e)
    {
        StopGraphPollTimer();

        var mediaClosedHandler = MediaClosed;
        if (mediaClosedHandler != null)
            mediaClosedHandler();
    }

    /// <summary>
    /// Invokes the MediaFailed event, notifying any subscriber that there was
    /// a media exception.
    /// </summary>
    /// <param name="e">The MediaFailedEventArgs contains the exception that caused this event to fire</param>
    protected void InvokeMediaFailed(MediaFailedEventArgs e)
    {
        var mediaFailedHandler = MediaFailed;
        if (mediaFailedHandler != null)
            mediaFailedHandler(this, e);
    }

    /// <summary>
    /// Invokes the NewAllocatorFrame event, notifying any subscriber that new frame
    /// is ready to be presented.
    /// </summary>
    protected void InvokeNewAllocatorFrame()
    {
        var newAllocatorFrameHandler = NewAllocatorFrame;
        if (newAllocatorFrameHandler != null)
            newAllocatorFrameHandler();
    }

    /// <summary>
    /// Invokes the NewAllocatorSurface event, notifying any subscriber of a new surface
    /// </summary>
    /// <param name="pSurface">The COM pointer to the D3D surface</param>
    protected void InvokeNewAllocatorSurface(IntPtr pSurface)
    {
        var del = NewAllocatorSurface;
        if (del != null)
            del(this, pSurface);
    }

    #endregion Event Invokes

    #region Helper Methods

    /// <summary>
    /// Sets the natural pixel resolution the video in the graph
    /// </summary>
    /// <param name="renderer">The video renderer</param>
    protected void SetNativePixelSizes(IBaseFilter renderer)
    {
        Size size = GetVideoSize(renderer, PinDirection.Input, 0);

        NaturalVideoHeight = (int)size.Height;
        NaturalVideoWidth = (int)size.Width;

        HasVideo = true;
    }

    /// <summary>
    /// Gets the video resolution of a pin on a renderer.
    /// </summary>
    /// <param name="renderer">The renderer to inspect</param>
    /// <param name="direction">The direction the pin is</param>
    /// <param name="pinIndex">The zero based index of the pin to inspect</param>
    /// <returns>If successful a video resolution is returned.  If not, a 0x0 size is returned</returns>
    protected static Size GetVideoSize(IBaseFilter renderer, PinDirection direction, int pinIndex)
    {
        var size = new Size();

        var mediaType = new AMMediaType();
        IPin pin = DsFindPin.ByDirection(renderer, direction, pinIndex);

        if (pin == null)
            goto done;

        int hr = pin.ConnectionMediaType(mediaType);

        if (hr != 0)
            goto done;

        /* Check to see if its a video media type */
        if (mediaType.formatType != FormatType.VideoInfo2 &&
            mediaType.formatType != FormatType.VideoInfo)
        {
            goto done;
        }

        var videoInfo = new VideoInfoHeader();

        /* Read the video info header struct from the native pointer */
        Marshal.PtrToStructure(mediaType.formatPtr, videoInfo);

        Rectangle rect = videoInfo.SrcRect.ToRectangle();
        size = new Size(rect.Width, rect.Height);

    done:
        DsUtils.FreeAMMediaType(mediaType);

        if (pin != null)
            Marshal.ReleaseComObject(pin);
        return size;
    }

    /// <summary>
    /// Removes all filters from a DirectShow graph
    /// </summary>
    /// <param name="graphBuilder">The DirectShow graph to remove all the filters from</param>
    protected static void RemoveAllFilters(IGraphBuilder graphBuilder)
    {
        if (graphBuilder == null)
            return;

        IEnumFilters enumFilters;

        /* The list of filters from the DirectShow graph */
        var filtersArray = new List<IBaseFilter>();

        if (graphBuilder == null)
            throw new ArgumentNullException("graphBuilder");

        /* Gets the filter enumerator from the graph */
        int hr = graphBuilder.EnumFilters(out enumFilters);
        DsError.ThrowExceptionForHR(hr);

        try
        {
            /* This array is filled with reference to a filter */
            var filters = new IBaseFilter[1];
            IntPtr fetched = IntPtr.Zero;

            /* Get reference to all the filters */
            while (enumFilters.Next(filters.Length, filters, fetched) == 0)
            {
                /* Add the filter to our array */
                filtersArray.Add(filters[0]);
            }
        }
        finally
        {
            /* Enum filters is a COM, so release that */
            Marshal.ReleaseComObject(enumFilters);
        }

        /* Loop over and release each COM */
        for (int i = 0; i < filtersArray.Count; i++)
        {
            graphBuilder.RemoveFilter(filtersArray[i]);
            while (Marshal.ReleaseComObject(filtersArray[i]) > 0)
            { }
        }
    }

    /// <summary>
    /// Adds a filter to a DirectShow graph based on it's name and filter category
    /// </summary>
    /// <param name="graphBuilder">The graph builder to add the filter to</param>
    /// <param name="deviceCategory">The category the filter belongs to</param>
    /// <param name="friendlyName">The friendly name of the filter</param>
    /// <returns>Reference to the IBaseFilter that was added to the graph or returns null if unsuccessful</returns>
    protected static IBaseFilter AddFilterByName(IGraphBuilder graphBuilder, Guid deviceCategory, string friendlyName)
    {
        var devices = DsDevice.GetDevicesOfCat(deviceCategory);

        var deviceList = (from d in devices
                          where d.Name == friendlyName
                          select d).ToList();
        DsDevice device = deviceList.FirstOrDefault();

        foreach (var item in deviceList)
        {
            if (item != device)
                item.Dispose();
        }

        return AddFilterByDevice(graphBuilder, device);
    }

    protected static IBaseFilter AddFilterByDevicePath(IGraphBuilder graphBuilder, Guid deviceCategory, string devicePath)
    {
        var devices = DsDevice.GetDevicesOfCat(deviceCategory);

        var deviceList = (from d in devices
                          where d.DevicePath == devicePath
                          select d).ToList();
        DsDevice device = deviceList.FirstOrDefault();

        foreach (var item in deviceList)
        {
            if (item != device)
                item.Dispose();
        }

        return AddFilterByDevice(graphBuilder, device);
    }

    private static IBaseFilter AddFilterByDevice(IGraphBuilder graphBuilder, DsDevice device)
    {
        if (graphBuilder == null)
            throw new ArgumentNullException("graphBuilder");
        if (device == null)
            return null;

        var filterGraph = graphBuilder as IFilterGraph2;

        if (filterGraph == null)
            return null;

        IBaseFilter filter = null;
        int hr = filterGraph.AddSourceFilterForMoniker(device.Mon, null, device.Name, out filter);
        DsError.ThrowExceptionForHR(hr);
        return filter;
    }

    /// <summary>
    /// Finds a pin that exists in a graph.
    /// </summary>
    /// <param name="majorOrMinorMediaType">The GUID of the major or minor type of the media</param>
    /// <param name="pinDirection">The direction of the pin - in/out</param>
    /// <param name="graph">The graph to search in</param>
    /// <returns>Returns null if the pin was not found, or if a pin is found, returns the first instance of it</returns>
    protected static IPin FindPinInGraphByMediaType(Guid majorOrMinorMediaType, PinDirection pinDirection, IGraphBuilder graph)
    {
        IEnumFilters enumFilters;

        /* Get the filter enum */
        graph.EnumFilters(out enumFilters);

        /* Init our vars */
        var filters = new IBaseFilter[1];
        var fetched = IntPtr.Zero;
        IPin pin = null;
        IEnumMediaTypes mediaTypesEnum = null;

        /* Loop over each filter in the graph */
        while (enumFilters.Next(1, filters, fetched) == 0)
        {
            var filter = filters[0];

            int i = 0;

            /* Loop over each pin in the filter */
            while ((pin = DsFindPin.ByDirection(filter, pinDirection, i)) != null)
            {
                /* Get the pin enumerator */
                pin.EnumMediaTypes(out mediaTypesEnum);
                var mediaTypesFetched = IntPtr.Zero;
                var mediaTypes = new AMMediaType[1];

                /* Enumerate the media types on the pin */
                while (mediaTypesEnum.Next(1, mediaTypes, mediaTypesFetched) == 0)
                {
                    /* See if the major or subtype meets our requirements */
                    if (mediaTypes[0].majorType.Equals(majorOrMinorMediaType) || mediaTypes[0].subType.Equals(majorOrMinorMediaType))
                    {
                        /* We found a match */
                        goto done;
                    }
                }
                i++;
            }
        }

    done:
        if (mediaTypesEnum != null)
        {
            mediaTypesEnum.Reset();
            Marshal.ReleaseComObject(mediaTypesEnum);
        }

        enumFilters.Reset();
        Marshal.ReleaseComObject(enumFilters);

        return pin;
    }

    #endregion Helper Methods
}
