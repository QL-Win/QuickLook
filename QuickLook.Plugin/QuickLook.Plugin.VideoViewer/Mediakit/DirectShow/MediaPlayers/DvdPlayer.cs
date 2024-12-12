using DirectShowLib;
using DirectShowLib.Dvd;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace WPFMediaKit.DirectShow.MediaPlayers;

/// <summary>
/// Arguments for an event reporting that the user's
/// mouse is over a Dvd button.
/// </summary>
public class OverDvdButtonEventArgs : EventArgs
{
    public OverDvdButtonEventArgs(bool isOverDvdButton)
    {
        IsOverDvdButton = isOverDvdButton;
    }

    /// <summary>
    /// Flag that defines if the cursor is over a Dvd button
    /// </summary>
    public bool IsOverDvdButton { get; private set; }
}

/// <summary>
/// Value indicating the button to select
/// </summary>
public enum DvdRelativeButtonEnum
{
    /// <summary>
    /// Left button
    /// </summary>
    Left = 1,

    /// <summary>
    /// Lower button
    /// </summary>
    Lower = 2,

    /// <summary>
    /// Right button
    /// </summary>
    Right = 3,

    /// <summary>
    /// Upper button
    /// </summary>
    Upper = 4
}

/// <summary>
/// Defines Dvd error conditions
/// </summary>
public enum DvdError
{
    /// <summary>
    /// Something unexpected happened; perhaps content is authored incorrectly. Playback is stopped.
    /// </summary>
    Unexpected,

    /// <summary>
    /// Key exchange for DVD copy protection failed. Playback is stopped.
    /// </summary>
    CopyProtectFail,

    /// <summary>
    /// DVD-Video disc is authored incorrectly for specification version 1. x. Playback is stopped.
    /// </summary>
    InvalidDvd10Disc,

    /// <summary>
    /// The disc cannot be played because it is not authored to play in the system region.
    /// You can try fixing the region mismatch by changing the system region with Dvdrgn.exe.
    /// </summary>
    InvalidDiscRegion,

    /// <summary>
    /// Player parental level is lower than the lowest parental level available in the DVD content. Playback is stopped.
    /// </summary>
    LowParentalLevel,

    /// <summary>
    /// Analog copy protection distribution failed. Playback stopped.
    /// </summary>
    MacrovisionFail,

    /// <summary>
    /// No discs can be played because the system region does not match the decoder region.
    /// </summary>
    IncompatibleSystemAndDecoderRegions,

    /// <summary>
    /// The disc cannot be played because the disc is not authored to be played in the decoder's region.
    /// </summary>
    IncompatibleDiscAndDecoderRegions
}

public class DvdErrorArgs : EventArgs
{
    public DvdError Error { get; internal set; }
}

/// <summary>
/// Arguments for an event reporting a new DVD time.
/// </summary>
public class DvdTimeEventArgs : EventArgs
{
    public DvdTimeEventArgs(TimeSpan dvdTime)
    {
        DvdTime = dvdTime;
    }

    /// <summary>
    /// The current Dvd time reported.
    /// </summary>
    public TimeSpan DvdTime { get; private set; }
}

[ComImport, Guid("212690FB-83E5-4526-8FD7-74478B7939CD")]
internal class MicrosoftMpeg2VideoDecoder
{
}

[ComImport, Guid("E1F1A0B8-BEEE-490D-BA7C-066C40B5E2B9")]
internal class MicrosoftMpeg2AudioDecoder
{
}

/// <summary>
/// Plays a DVD disc or will play DVD video files from a path.
/// Normally, when a DVD is played with a custom allocator, IVideoWindow will
/// be queried from the graph.  Since there will be no IVideoWindow in this graph
/// the IDvdControl2 will crash when we try to use the SelectAtPosition and
/// ActivateAtPosition.  We get around this by sacrificing the Line21 pin and
/// connecting it to another video renderer that does have an IVideoWindow, but
/// we make sure to keep the actual hWnd hidden.
/// </summary>
public class DvdPlayer : MediaSeekingPlayer
{
    /// <summary>
    /// Constant value for converting media time back and forth
    /// to milliseconds;
    /// </summary>
    private const int MEDIA_TIME_TO_MILLISECONDS = 10000;

    /// <summary>
    /// The current time the DVD playback is at
    /// </summary>
    private TimeSpan m_currentDvdTime;

    /// <summary>
    /// Reference to the hidden render window
    /// </summary>
    private IVideoWindow m_dummyRenderWindow;

    /// <summary>
    /// The total number of DVD buttons currently on screen
    /// </summary>
    private int m_dvdButtonCount;

    /// <summary>
    /// The main interface for DVD control
    /// </summary>
    private IDvdControl2 m_dvdControl;

    /// <summary>
    /// The main interface for DVD information
    /// </summary>
    private IDvdInfo2 m_dvdInfo;

    /// <summary>
    /// The DirectShow filter graph
    /// </summary>
    private IGraphBuilder m_graph;

    /// <summary>
    /// The renderer used to render video to WPF
    /// </summary>
    private IBaseFilter m_renderer;

#if DEBUG

    /// <summary>
    /// The 'Running Objects Table'.  Used to remotely debug the graph
    /// </summary>
    private DsROTEntry m_rot;

#endif

    /// <summary>
    /// Used to store the dummy renderer target coords of the subpicture video
    /// </summary>
    private Rectangle m_renderTargetRect = Rectangle.Empty;

    /// <summary>
    /// The GUID of the DVD subpicture media type
    /// </summary>
    private readonly Guid DVD_SUBPICTURE_TYPE = new Guid("{E06D802D-DB46-11CF-B4D1-00805F6CBBEA}");

    /// <summary>
    /// Flag to remember if we are over a DVD button.
    /// </summary>
    private bool m_isOverButton;

    /// <summary>
    /// The input pin of the dummy renderer
    /// </summary>
    private IPin m_dummyRendererPin;

    /// <summary>
    /// Fires when a DVD has been inserted
    /// </summary>
    public event EventHandler OnDvdInserted;

    /// <summary>
    /// Fires when a DVD has been ejected
    /// </summary>
    public event EventHandler OnDvdEjected;

    /// <summary>
    /// Fires when the DVD time changes
    /// </summary>
    public event EventHandler<DvdTimeEventArgs> OnDvdTime;

    /// <summary>
    /// Fires when the mouse is over a DVD button
    /// </summary>
    public event EventHandler<OverDvdButtonEventArgs> OnOverDvdButton;

    /// <summary>
    /// Fires when a DVD specific error occurs
    /// </summary>
    public event EventHandler<DvdErrorArgs> OnDvdError;

    private bool m_dvdDirectoryDirty;
    private string m_dvdDirectory;

    /// <summary>
    /// The directory to try to play the DVD from.  If this is null then
    /// DirectShow will search for a DVD to play.
    /// </summary>
    public string DvdDirectory
    {
        get => m_dvdDirectory;
        set
        {
            m_dvdDirectory = value;
            m_dvdDirectoryDirty = true;
        }
    }

    #region Event Invokers

    private void InvokeDvdError(DvdError error)
    {
        var e = new DvdErrorArgs { Error = error };
        OnDvdError?.Invoke(this, e);
    }

    private void InvokeOnDvdTime(DvdTimeEventArgs e)
    {
        OnDvdTime?.Invoke(this, e);
    }

    private void InvokeOnOverDvdButton(bool isOverDvdButton)
    {
        var e = new OverDvdButtonEventArgs(isOverDvdButton);
        OnOverDvdButton?.Invoke(this, e);
    }

    private void InvokeOnDvdInserted()
    {
        OnDvdInserted?.Invoke(this, EventArgs.Empty);
    }

    private void InvokeOnDvdEjected()
    {
        OnDvdEjected?.Invoke(this, EventArgs.Empty);
    }

    #endregion Event Invokers

    /// <summary>
    /// Navigates to the Root menu of the DVD title
    /// </summary>
    public void GotoRootMenu()
    {
        if (m_dvdControl == null)
            return;

        m_dvdControl.ShowMenu(DvdMenuId.Root,
                              DvdCmdFlags.Block | DvdCmdFlags.Flush,
                              out IDvdCmd cmd);

        if (cmd != null)
            Marshal.ReleaseComObject(cmd);
    }

    /// <summary>
    /// Gets the total number of titles on the DVD
    /// </summary>
    public int TitleCount
    {
        get
        {
            VerifyAccess();

            if (m_dvdInfo == null)
                return 0;

            m_dvdInfo.GetDVDVolumeInfo(out _,
                                       out _,
                                       out _,
                                       out int titleCount);

            return titleCount;
        }
    }

    /// <summary>
    /// Navigates to the Title menu of the DVD
    /// </summary>
    public void GotoTitleMenu()
    {
        VerifyAccess();

        if (m_dvdControl == null)
            return;

        m_dvdControl.ShowMenu(DvdMenuId.Title,
                              DvdCmdFlags.Block | DvdCmdFlags.Flush,
                              out IDvdCmd cmd);

        if (cmd != null)
            Marshal.ReleaseComObject(cmd);
    }

    /// <summary>
    /// Returns the display from a submenu to its parent menu
    /// </summary>
    public void ReturnFromSubmenu()
    {
        VerifyAccess();

        if (m_dvdControl == null)
            return;

        m_dvdControl.ReturnFromSubmenu(DvdCmdFlags.None, out IDvdCmd cmd);

        if (cmd != null)
            Marshal.ReleaseComObject(cmd);
    }

    /// <summary>
    /// The SelectAngle method sets the new angle when the DVD Navigator is in an angle block
    /// </summary>
    /// <param name="angle">Value of the new angle, which must be from 1 through 9</param>
    public void SelectAngle(int angle)
    {
        VerifyAccess();

        if (m_dvdControl == null)
            return;

        m_dvdControl.SelectAngle(angle, DvdCmdFlags.None, out IDvdCmd cmd);

        if (cmd != null)
            Marshal.ReleaseComObject(cmd);
    }

    /// <summary>
    /// Leaves a menu and resumes playback.
    /// </summary>
    public void Resume()
    {
        VerifyAccess();

        if (m_dvdControl == null)
            return;

        m_dvdControl.Resume(DvdCmdFlags.None, out IDvdCmd cmd);

        if (cmd != null)
            Marshal.ReleaseComObject(cmd);
    }

    /// <summary>
    /// Selects the specified relative button (upper, lower, right, left)
    /// </summary>
    public void SelectRelativeButton(DvdRelativeButtonEnum relativeButton)
    {
        VerifyAccess();

        if (m_dvdControl == null)
            return;

        m_dvdControl.SelectRelativeButton((DvdRelativeButton)relativeButton);
    }

    /// <summary>
    /// Selects a menu item at a certain position on the video
    /// </summary>
    /// <param name="widthRatio">The percentage to the right</param>
    /// <param name="heightRatio">The percentage to the bottom</param>
    public void SelectAtPosition(double widthRatio, double heightRatio)
    {
        VerifyAccess();

        if (m_dvdControl == null || m_dvdButtonCount == 0)
            return;

        /* We base our exact point based on the size of the subpicture target rect */
        var pixelPoint = new Point((int)(m_renderTargetRect.Width * widthRatio),
                                   (int)(m_renderTargetRect.Height * heightRatio));

        int hr = m_dvdControl.SelectAtPosition(pixelPoint);

        if (hr == 0)
        {
            if (m_isOverButton == false)
            {
                m_isOverButton = true;
                InvokeOnOverDvdButton(m_isOverButton);
            }
        }
        else
        {
            if (m_isOverButton)
            {
                m_isOverButton = false;
                InvokeOnOverDvdButton(m_isOverButton);
            }
        }
    }

    /// <summary>
    /// Activates a menu item at a certain position on the video
    /// </summary>
    /// <param name="widthRatio">The ratio to the right</param>
    /// <param name="heightRatio">The ratio to the bottom</param>
    public void ActivateAtPosition(double widthRatio, double heightRatio)
    {
        VerifyAccess();

        if (m_dvdControl == null || m_dvdButtonCount == 0)
            return;

        /* We base our exact point based on the size of the subpicture target rect */
        var pixelPoint = new Point((int)(m_renderTargetRect.Width * widthRatio),
                                   (int)(m_renderTargetRect.Height * heightRatio));

        m_dvdControl.ActivateAtPosition(pixelPoint);
    }

    /// <summary>
    /// Sets the number of DVD buttons found in the current DVD video
    /// </summary>
    /// <param name="buttonCount">The total number of buttons</param>
    private void SetDvdButtonCount(int buttonCount)
    {
        m_dvdButtonCount = buttonCount;

        if (m_dvdButtonCount == 0)
        {
            m_isOverButton = false;
            InvokeOnOverDvdButton(m_isOverButton);
        }

        var mediaType = new AMMediaType();
        m_dummyRendererPin.ConnectionMediaType(mediaType);

        /* Check to see if its a video media type */
        if (mediaType.formatType != FormatType.VideoInfo2 &&
            mediaType.formatType != FormatType.VideoInfo)
        {
            DsUtils.FreeAMMediaType(mediaType);
            return;
        }

        var videoInfo = new VideoInfoHeader();

        /* Read the video info header struct from the native pointer */
        Marshal.PtrToStructure(mediaType.formatPtr, videoInfo);

        /* Get the target rect */
        m_renderTargetRect = videoInfo.TargetRect.ToRectangle();

        DsUtils.FreeAMMediaType(mediaType);
    }

    /// <summary>
    /// Plays a specific title by a given title index
    /// </summary>
    public void PlayTitle(int titleIndex)
    {
        VerifyAccess();

        m_dvdControl.PlayTitle(titleIndex, DvdCmdFlags.Flush, out IDvdCmd cmd);

        if (cmd != null)
            Marshal.ReleaseComObject(cmd);
    }

    /// <summary>
    /// Plays the next chapter of the DVD
    /// </summary>
    public void PlayNextChapter()
    {
        VerifyAccess();

        if (m_dvdControl == null)
            return;

        m_dvdControl.PlayNextChapter(DvdCmdFlags.Flush, out IDvdCmd cmd);

        if (cmd != null)
            Marshal.ReleaseComObject(cmd);
    }

    /// <summary>
    /// Plays the DVD forward
    /// </summary>
    /// <param name="speed">The speed at which the playback is done</param>
    public void PlayForwards(double speed)
    {
        VerifyAccess();

        if (m_dvdControl == null)
            return;

        m_dvdControl.PlayForwards(speed, DvdCmdFlags.None, out IDvdCmd cmd);

        if (cmd != null)
            Marshal.ReleaseComObject(cmd);
    }

    /// <summary>
    /// Plays the DVD backwards
    /// </summary>
    /// <param name="speed">The speed at the playback is done</param>
    public void PlayBackwards(double speed)
    {
        VerifyAccess();

        if (m_dvdControl == null)
            return;

        m_dvdControl.PlayBackwards(speed, DvdCmdFlags.None, out IDvdCmd cmd);

        if (cmd != null)
            Marshal.ReleaseComObject(cmd);
    }

    /// <summary>
    /// Plays the previous chapter of the DVD
    /// </summary>
    public void PlayPreviousChapter()
    {
        VerifyAccess();

        if (m_dvdControl == null)
            return;

        m_dvdControl.PlayPrevChapter(DvdCmdFlags.Flush, out IDvdCmd cmd);

        if (cmd != null)
            Marshal.ReleaseComObject(cmd);
    }

    /// <summary>
    /// Builds the DVD DirectShow graph
    /// </summary>
    private void BuildGraph()
    {
        try
        {
            FreeResources();

            int hr;

            /* Create our new graph */
            m_graph = (IGraphBuilder)new FilterGraphNoThread();

#if DEBUG
            m_rot = new DsROTEntry(m_graph);
#endif
            /* We are going to use the VMR9 for now.  The EVR does not
             * seem to work with the interactive menus yet.  It should
             * play Dvds fine otherwise */
            var rendererType = VideoRendererType.VideoMixingRenderer9;

            /* Creates and initializes a new renderer ready to render to WPF */
            m_renderer = CreateVideoRenderer(rendererType, m_graph, 2);

            /* Do some VMR9 specific stuff */
            if (rendererType == VideoRendererType.VideoMixingRenderer9)
            {
                if (m_renderer is IVMRMixerControl9 mixer)
                {
                    mixer.GetMixingPrefs(out VMR9MixerPrefs dwPrefs);
                    dwPrefs &= ~VMR9MixerPrefs.RenderTargetMask;
                    dwPrefs |= VMR9MixerPrefs.RenderTargetYUV;

                    /* Enable this line to prefer YUV */
                    //hr = mixer.SetMixingPrefs(dwPrefs);
                }
            }

            /* Create a new DVD Navigator. */
            var dvdNav = (IBaseFilter)new DVDNavigator();

            /* The DVDControl2 interface lets us control DVD features */
            m_dvdControl = dvdNav as IDvdControl2;

            if (m_dvdControl == null)
                throw new WPFMediaKitException("Could not QueryInterface the IDvdControl2 interface");

            /* QueryInterface the DVDInfo2 */
            m_dvdInfo = dvdNav as IDvdInfo2;

            /* If a Dvd directory has been set then use it, if not, let DShow find the Dvd */
            if (!string.IsNullOrEmpty(DvdDirectory))
            {
                hr = m_dvdControl.SetDVDDirectory(DvdDirectory);
                DsError.ThrowExceptionForHR(hr);
            }

            /* This gives us the DVD time in Hours-Minutes-Seconds-Frame time format, and other options */
            hr = m_dvdControl.SetOption(DvdOptionFlag.HMSFTimeCodeEvents, true);
            DsError.ThrowExceptionForHR(hr);

            /* If the graph stops, resume at the same point */
            m_dvdControl.SetOption(DvdOptionFlag.ResetOnStop, false);

            hr = m_graph.AddFilter(dvdNav, "DVD Navigator");
            DsError.ThrowExceptionForHR(hr);

            IPin dvdVideoPin = null;
            IPin dvdAudioPin = null;
            IPin dvdSubPicturePin = null;

            IPin dvdNavPin;
            int i = 0;

            /* Loop all the output pins on the DVD Navigator, trying to find which pins are which.
             * We could more easily find the pins by name, but this is more fun...and more flexible
             * if we ever want to use a 3rd party DVD navigator that used different pin names */
            while ((dvdNavPin = DsFindPin.ByDirection(dvdNav, PinDirection.Output, i)) != null)
            {
                var mediaTypes = new AMMediaType[1];
                IntPtr pFetched = IntPtr.Zero;

                IEnumMediaTypes mediaTypeEnum;
                dvdNavPin.EnumMediaTypes(out mediaTypeEnum);

                /* Loop over each of the mediaTypes of each pin */
                while (mediaTypeEnum.Next(1, mediaTypes, pFetched) == 0)
                {
                    AMMediaType mediaType = mediaTypes[0];

                    /* This will be the video stream pin */
                    if (mediaType.subType == MediaSubType.Mpeg2Video)
                    {
                        /* Keep the ref and we'll work with it later */
                        dvdVideoPin = dvdNavPin;
                        break;
                    }

                    /* This will be the audio stream pin */
                    if (mediaType.subType == MediaSubType.DolbyAC3 ||
                       mediaType.subType == MediaSubType.Mpeg2Audio)
                    {
                        /* Keep the ref and we'll work with it later */
                        dvdAudioPin = dvdNavPin;
                        break;
                    }

                    /* This is the Dvd sub picture pin.  This generally
                     * shows overlays for Dvd menus and sometimes closed captions */
                    if (mediaType.subType == DVD_SUBPICTURE_TYPE)
                    {
                        /* Keep the ref and we'll work with it later */
                        dvdSubPicturePin = dvdNavPin;
                        break;
                    }
                }

                mediaTypeEnum.Reset();
                Marshal.ReleaseComObject(mediaTypeEnum);
                i++;
            }

            /* This is the windowed renderer.  This is *NEEDED* in order
             * for interactive menus to work with the other VMR9 in renderless mode */
            var dummyRenderer = (IBaseFilter)new VideoMixingRenderer9();
            var dummyRendererConfig = (IVMRFilterConfig9)dummyRenderer;

            /* In order for this interactive menu trick to work, the VMR9
             * must be set to Windowed.  We will make sure the window is hidden later on */
            hr = dummyRendererConfig.SetRenderingMode(VMR9Mode.Windowed);
            DsError.ThrowExceptionForHR(hr);

            hr = dummyRendererConfig.SetNumberOfStreams(1);
            DsError.ThrowExceptionForHR(hr);

            hr = m_graph.AddFilter(dummyRenderer, "Dummy Windowed");
            DsError.ThrowExceptionForHR(hr);

            if (dvdAudioPin != null)
            {
                /* This should render out to the default audio device. We
                 * could modify this code here to go out any audio
                 * device, such as SPDIF or another sound card */
                hr = m_graph.Render(dvdAudioPin);
                DsError.ThrowExceptionForHR(hr);
            }

            /* Get the first input pin on our dummy renderer */
            m_dummyRendererPin = DsFindPin.ByConnectionStatus(dummyRenderer, /* Filter to search */
                                                              PinConnectedStatus.Unconnected,
                                                              0);

            /* Get an available pin on our real renderer */
            IPin rendererPin = DsFindPin.ByConnectionStatus(m_renderer, /* Filter to search */
                                                            PinConnectedStatus.Unconnected,
                                                            0); /* Pin index */

            /* Connect the pin to the renderer */
            hr = m_graph.Connect(dvdVideoPin, rendererPin);
            DsError.ThrowExceptionForHR(hr);

            /* Get the next available pin on our real renderer */
            rendererPin = DsFindPin.ByConnectionStatus(m_renderer, /* Filter to search */
                                                       PinConnectedStatus.Unconnected,
                                                       0); /* Pin index */

            /* Render the sub picture, which will connect
             * the DVD navigator to the codec, not the renderer */
            hr = m_graph.Render(dvdSubPicturePin);
            DsError.ThrowExceptionForHR(hr);

            /* These are the subtypes most likely to be our dvd subpicture */
            var preferedSubpictureTypes = new[]{MediaSubType.ARGB4444,
                                                MediaSubType.AI44,
                                                MediaSubType.AYUV,
                                                MediaSubType.ARGB32};
            IPin dvdSubPicturePinOut = null;

            /* Find what should be the subpicture pin out */
            foreach (var guidType in preferedSubpictureTypes)
            {
                dvdSubPicturePinOut = FindPinInGraphByMediaType(guidType, /* GUID of the media type being searched for */
                                                                PinDirection.Output,
                                                                m_graph); /* Our current graph */
                if (dvdSubPicturePinOut != null)
                    break;
            }

            if (dvdSubPicturePinOut == null)
                throw new WPFMediaKitException("Could not find the sub picture pin out");

            /* Here we connec thte Dvd sub picture pin to the video renderer.
             * This enables the overlays on Dvd menus and some closed
             * captions to be rendered. */
            hr = m_graph.Connect(dvdSubPicturePinOut, rendererPin);
            DsError.ThrowExceptionForHR(hr);

            /* Search for the Line21 out in the graph */
            IPin line21Out = FindPinInGraphByMediaType(MediaType.AuxLine21Data,
                                                       PinDirection.Output,
                                                       m_graph);
            if (line21Out == null)
                throw new WPFMediaKitException("Could not find the Line21 pin out");

            /* We connect our line21Out out in to the dummy renderer
             * this is what ultimatly makes interactive DVDs work with
             * VMR9 in renderless (for WPF) */
            hr = m_graph.Connect(line21Out, m_dummyRendererPin);
            DsError.ThrowExceptionForHR(hr);

            /* This is the dummy renderers Win32 window. */
            m_dummyRenderWindow = dummyRenderer as IVideoWindow;

            if (m_dummyRenderWindow == null)
                throw new WPFMediaKitException("Could not QueryInterface for IVideoWindow");

            ConfigureDummyWindow();

            /* Setup our base classes with this filter graph */
            SetupFilterGraph(m_graph);

            /* Sets the NaturalVideoWidth/Height */
            SetNativePixelSizes(m_renderer);
        }
        catch (Exception ex)
        {
            FreeResources();
            InvokeMediaFailed(new MediaFailedEventArgs(ex.Message, ex));
            return;
        }

        InvokeMediaOpened();
    }

    /// <summary>
    /// Configures our "dummy" IVideoWindow to work well
    /// with our interactive menus and to make sure the
    /// window remains hidden from view.
    /// </summary>
    private void ConfigureDummyWindow()
    {
        /* We want to hide our dummy renderer window */
        int hr = m_dummyRenderWindow.put_WindowState(WindowState.Hide);
        DsError.ThrowExceptionForHR(hr);

        WindowStyle windowStyle;

        /* Get the current style of the window */
        m_dummyRenderWindow.get_WindowStyle(out windowStyle);
        DsError.ThrowExceptionForHR(hr);

        /* Remove these styles using bitwise magic */
        windowStyle &= ~WindowStyle.SysMenu;
        windowStyle &= ~WindowStyle.Caption;
        windowStyle &= ~WindowStyle.Border;

        /* Change the window to our new style */
        hr = m_dummyRenderWindow.put_WindowStyle(windowStyle);
        DsError.ThrowExceptionForHR(hr);

        /* This should hide the window from view */
        hr = m_dummyRenderWindow.put_Visible(OABool.False);
        DsError.ThrowExceptionForHR(hr);

        /* Turn off auto show, so the renderer doesn't try to show itself */
        hr = m_dummyRenderWindow.put_AutoShow(OABool.False);
        DsError.ThrowExceptionForHR(hr);
    }

    /// <summary>
    /// Gets or sets the position in miliseconds of the media
    /// </summary>
    public override long MediaPosition
    {
        get => (long)m_currentDvdTime.TotalMilliseconds * MEDIA_TIME_TO_MILLISECONDS;
        set
        {
            var timeCode = new DvdHMSFTimeCode();
            var time = TimeSpan.FromMilliseconds(value / (double)MEDIA_TIME_TO_MILLISECONDS);

            timeCode.bHours = (byte)time.Hours;
            timeCode.bMinutes = (byte)time.Minutes;
            timeCode.bSeconds = (byte)time.Seconds;

            IDvdCmd cmd;
            if (m_dvdControl == null)
                return;

            m_dvdControl.PlayAtTime(timeCode, DvdCmdFlags.None, out cmd);

            if (cmd != null)
                Marshal.ReleaseComObject(cmd);
        }
    }

    /// <summary>
    /// Here we extract out the new Dvd duration of
    /// the title currently being played
    /// </summary>
    private void SetTitleDuration()
    {
        var totalTime = new DvdHMSFTimeCode();
        int hr = m_dvdInfo.GetTotalTitleTime(totalTime, out DvdTimeCodeFlags flags);

        if (hr != 0)
            return;

        /* Convert the total time of the title to milliseconds */
        Duration = (long)new TimeSpan(totalTime.bHours,
                                      totalTime.bMinutes,
                                      totalTime.bSeconds).TotalMilliseconds * MEDIA_TIME_TO_MILLISECONDS;
    }

    /// <summary>
    /// Is called when a new media event code occurs on the graph
    /// </summary>
    /// <param name="code">The event code that occured</param>
    /// <param name="lparam1">The first parameter sent by the graph</param>
    /// <param name="lparam2">The second parameter sent by the graph</param>
    protected override void OnMediaEvent(EventCode code, IntPtr lparam1, IntPtr lparam2)
    {
        switch (code)
        {
            case EventCode.DvdCurrentHmsfTime:
                /* This is time in hours, minutes, seconds, frames format.
                 * The the time is one, 4 byte integer, each byte representing
                 * an hour, minute, second or frame */
                byte[] times = BitConverter.GetBytes(lparam1.ToInt32());
                m_currentDvdTime = new TimeSpan(times[0], times[1], times[2]);

                /* Report the time to anyone that cares to listen */
                InvokeOnDvdTime(new DvdTimeEventArgs(m_currentDvdTime));
                break;

            case EventCode.DvdDomainChange:
                break;

            case EventCode.DvdTitleChange:
                SetTitleDuration();
                break;

            case EventCode.DvdChapterStart:
                SetTitleDuration();
                break;

            case EventCode.DvdAudioStreamChange:
                break;

            case EventCode.DvdSubPicictureStreamChange:
                break;

            case EventCode.DvdAngleChange:
                /* For porn? */
                break;

            case EventCode.DvdButtonChange:
                /* Keep track of button counts */
                SetDvdButtonCount(lparam1.ToInt32());
                break;

            case EventCode.DvdValidUopsChange:
                break;

            case EventCode.DvdStillOn:
                break;

            case EventCode.DvdStillOff:
                break;

            case EventCode.DvdCurrentTime:
                break;

            case EventCode.DvdError:
                /* Notify any listener of any Dvd specific
                 * errors we may get when loading or playing Dvds */
                InvokeDvdError((DvdError)lparam1.ToInt32());
                break;

            case EventCode.DvdWarning:
                break;

            case EventCode.DvdChapterAutoStop:
                break;

            case EventCode.DvdNoFpPgc:
                break;

            case EventCode.DvdPlaybackRateChange:
                break;

            case EventCode.DvdParentalLevelChange:
                break;

            case EventCode.DvdPlaybackStopped:
                break;

            case EventCode.DvdAnglesAvailable:
                break;

            case EventCode.DvdPlayPeriodAutoStop:
                break;

            case EventCode.DvdButtonAutoActivated:
                break;

            case EventCode.DvdCmdStart:
                break;

            case EventCode.DvdCmdEnd:
                break;

            case EventCode.DvdDiscEjected:
                InvokeOnDvdEjected();
                break;

            case EventCode.DvdDiscInserted:
                /* For some reason we only get this
                 * event when a Dvd graph has successfully
                 * been started.  Otherwise it does not work */
                InvokeOnDvdInserted();
                break;

            case EventCode.DvdKaraokeMode:
                /* For drunks */
                break;

            default:
                break;
        }

        base.OnMediaEvent(code, lparam1, lparam2);
    }

    /// <summary>
    /// Plays the Dvd
    /// </summary>
    public override void Play()
    {
        if (m_dvdControl == null || m_dvdDirectoryDirty)
        {
            m_dvdDirectoryDirty = false;
            Stop();
            BuildGraph();
        }

        base.Play();
    }

    /// <summary>
    /// Frees any allocated or unmanaged resources
    /// </summary>
    protected override void FreeResources()
    {
        base.FreeResources();
#if DEBUG
        if (m_rot != null)
            m_rot.Dispose();
#endif
        if (m_dummyRendererPin != null)
        {
            Marshal.ReleaseComObject(m_dummyRendererPin);
            m_dummyRendererPin = null;
        }
        if (m_dummyRenderWindow != null)
        {
            Marshal.ReleaseComObject(m_dummyRenderWindow);
            m_dummyRenderWindow = null;
        }
        if (m_renderer != null)
        {
            Marshal.ReleaseComObject(m_renderer);
            m_renderer = null;
        }
        if (m_dvdInfo != null)
        {
            Marshal.ReleaseComObject(m_dvdInfo);
            m_dvdInfo = null;
        }
        if (m_dvdControl != null)
        {
            Marshal.ReleaseComObject(m_dvdControl);
            m_dvdControl = null;
        }
        if (m_graph != null)
        {
            Marshal.ReleaseComObject(m_graph);
            m_graph = null;
        }
    }
}
