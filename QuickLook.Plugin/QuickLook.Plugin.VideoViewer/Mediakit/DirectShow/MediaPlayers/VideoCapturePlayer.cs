using DirectShowLib;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WPFMediaKit.DirectShow.MediaPlayers;

public class VideoSampleArgs : EventArgs
{
    public Bitmap VideoFrame { get; internal set; }
}

/// <summary>
/// A Player that plays video from a video capture device.
/// </summary>
public class VideoCapturePlayer : MediaPlayerBase, ISampleGrabberCB
{
    [DllImport("Kernel32.dll", EntryPoint = "RtlMoveMemory")]
    private static extern void CopyMemory(IntPtr destination, IntPtr source, [MarshalAs(UnmanagedType.U4)] int length);

    #region Locals

    /// <summary>
    /// The video capture pixel height
    /// </summary>
    private int m_desiredHeight = 240;

    /// <summary>
    /// The video capture pixel width
    /// </summary>
    private int m_desiredWidth = 320;

    /// <summary>
    /// The video capture's frames per second
    /// </summary>
    private int m_fps = 30;

    /// <summary>
    /// Our DirectShow filter graph
    /// </summary>
    private IGraphBuilder m_graph;

    /// <summary>
    /// The DirectShow video renderer
    /// </summary>
    private IBaseFilter m_renderer;

    /// <summary>
    /// The capture device filter
    /// </summary>
    private IBaseFilter m_captureDevice;

    /// <summary>
    /// The name of the video capture source device
    /// </summary>
    private string m_videoCaptureSource;

    /// <summary>
    /// Flag to detect if the capture source has changed
    /// </summary>
    private bool m_videoCaptureSourceChanged;

    /// <summary>
    /// The video capture device
    /// </summary>
    private DsDevice m_videoCaptureDevice;

    /// <summary>
    /// Flag to detect if the capture source device has changed
    /// </summary>
    private bool m_videoCaptureDeviceChanged;

    /// <summary>
    /// The sample grabber interface used for getting samples in a callback
    /// </summary>
    private ISampleGrabber m_sampleGrabber;

    private string m_fileName;

#if DEBUG
    private DsROTEntry m_rotEntry;
#endif

    #endregion Locals

    /// <summary>
    /// Gets or sets if the instance fires an event for each of the samples
    /// </summary>
    public bool EnableSampleGrabbing { get; set; }

    /// <summary>
    /// Fires when a new video sample is ready
    /// </summary>
    public event EventHandler<VideoSampleArgs> NewVideoSample;

    private void InvokeNewVideoSample(VideoSampleArgs e)
    {
        EventHandler<VideoSampleArgs> sample = NewVideoSample;
        if (sample != null) sample(this, e);
    }

    /// <summary>
    /// The name of the video capture source to use
    /// </summary>
    public string VideoCaptureSource
    {
        get
        {
            VerifyAccess();
            return m_videoCaptureSource;
        }
        set
        {
            VerifyAccess();
            m_videoCaptureSource = value;
            m_videoCaptureSourceChanged = true;

            /* Free our unmanaged resources when
             * the source changes */
            FreeResources();
        }
    }

    public DsDevice VideoCaptureDevice
    {
        get
        {
            VerifyAccess();
            return m_videoCaptureDevice;
        }
        set
        {
            VerifyAccess();
            m_videoCaptureDevice = value;
            m_videoCaptureDeviceChanged = true;

            /* Free our unmanaged resources when
             * the source changes */
            FreeResources();
        }
    }

    /// <summary>
    /// The frames per-second to play
    /// the capture device back at
    /// </summary>
    public int FPS
    {
        get
        {
            VerifyAccess();
            return m_fps;
        }
        set
        {
            VerifyAccess();

            /* We support only a minimum of
             * one frame per second */
            if (value < 1)
                value = 1;

            m_fps = value;
        }
    }

    /// <summary>
    /// Gets or sets if Yuv is the prefered color space
    /// </summary>
    public bool UseYuv { get; set; }

    /// <summary>
    /// The desired pixel width of the video
    /// </summary>
    public int DesiredWidth
    {
        get
        {
            VerifyAccess();
            return m_desiredWidth;
        }
        set
        {
            VerifyAccess();
            m_desiredWidth = value;
        }
    }

    /// <summary>
    /// The desired pixel height of the video
    /// </summary>
    public int DesiredHeight
    {
        get
        {
            VerifyAccess();
            return m_desiredHeight;
        }
        set
        {
            VerifyAccess();
            m_desiredHeight = value;
        }
    }

    public string FileName
    {
        get
        {
            //VerifyAccess();
            return m_fileName;
        }
        set
        {
            //VerifyAccess();
            m_fileName = value;
        }
    }

    /// <summary>
    /// Plays the video capture device
    /// </summary>
    public override void Play()
    {
        VerifyAccess();

        if (m_graph == null)
            SetupGraph();

        base.Play();
    }

    /// <summary>
    /// Pauses the video capture device
    /// </summary>
    public override void Pause()
    {
        VerifyAccess();

        if (m_graph == null)
            SetupGraph();

        base.Pause();
    }

    public void ShowCapturePropertyPages(IntPtr hwndOwner)
    {
        VerifyAccess();

        if (m_captureDevice == null)
            return;

        using (var dialog = new PropertyPageHelper(m_captureDevice))
        {
            dialog.Show(hwndOwner);
        }
    }

    /// <summary>
    /// Configures the DirectShow graph to play the selected video capture
    /// device with the selected parameters
    /// </summary>
    private void SetupGraph()
    {
        /* Clean up any messes left behind */
        FreeResources();

        try
        {
            /* Create a new graph */
            m_graph = (IGraphBuilder)new FilterGraphNoThread();

#if DEBUG
            m_rotEntry = new DsROTEntry(m_graph);
#endif

            /* Create a capture graph builder to help
             * with rendering a capture graph */
            var graphBuilder = (ICaptureGraphBuilder2)new CaptureGraphBuilder2();

            /* Set our filter graph to the capture graph */
            int hr = graphBuilder.SetFiltergraph(m_graph);
            DsError.ThrowExceptionForHR(hr);

            /* Add our capture device source to the graph */
            if (m_videoCaptureSourceChanged)
            {
                m_captureDevice = AddFilterByName(m_graph,
                                                  FilterCategory.VideoInputDevice,
                                                  VideoCaptureSource);

                m_videoCaptureSourceChanged = false;
            }
            else if (m_videoCaptureDeviceChanged)
            {
                m_captureDevice = AddFilterByDevicePath(m_graph,
                                                        FilterCategory.VideoInputDevice,
                                                        VideoCaptureDevice.DevicePath);

                m_videoCaptureDeviceChanged = false;
            }

            /* If we have a null capture device, we have an issue */
            if (m_captureDevice == null)
                throw new WPFMediaKitException(string.Format("Capture device {0} not found or could not be created", VideoCaptureSource));

            if (UseYuv && !EnableSampleGrabbing)
            {
                /* Configure the video output pin with our parameters and if it fails
                 * then just use the default media subtype*/
                if (!SetVideoCaptureParameters(graphBuilder, m_captureDevice, MediaSubType.YUY2))
                    SetVideoCaptureParameters(graphBuilder, m_captureDevice, Guid.Empty);
            }
            else
                /* Configure the video output pin with our parameters */
                SetVideoCaptureParameters(graphBuilder, m_captureDevice, Guid.Empty);

            var rendererType = VideoRendererType.VideoMixingRenderer9;

            /* Creates a video renderer and register the allocator with the base class */
            m_renderer = CreateVideoRenderer(rendererType, m_graph, 1);

            if (rendererType == VideoRendererType.VideoMixingRenderer9)
            {
                var mixer = m_renderer as IVMRMixerControl9;

                if (mixer != null && !EnableSampleGrabbing && UseYuv)
                {
                    VMR9MixerPrefs dwPrefs;
                    mixer.GetMixingPrefs(out dwPrefs);
                    dwPrefs &= ~VMR9MixerPrefs.RenderTargetMask;
                    dwPrefs |= VMR9MixerPrefs.RenderTargetYUV;
                    /* Prefer YUV */
                    mixer.SetMixingPrefs(dwPrefs);
                }
            }

            if (EnableSampleGrabbing)
            {
                m_sampleGrabber = (ISampleGrabber)new SampleGrabber();
                SetupSampleGrabber(m_sampleGrabber);
                hr = m_graph.AddFilter(m_sampleGrabber as IBaseFilter, "SampleGrabber");
                DsError.ThrowExceptionForHR(hr);
            }

            IBaseFilter mux = null;
            IFileSinkFilter sink = null;
            if (!string.IsNullOrEmpty(this.m_fileName))
            {
                hr = graphBuilder.SetOutputFileName(MediaSubType.Asf, this.m_fileName, out mux, out sink);
                DsError.ThrowExceptionForHR(hr);

                hr = graphBuilder.RenderStream(PinCategory.Capture, MediaType.Video, m_captureDevice, null, mux);
                DsError.ThrowExceptionForHR(hr);

                // use the first audio device
                var audioDevices = DsDevice.GetDevicesOfCat(FilterCategory.AudioInputDevice);

                if (audioDevices.Length > 0)
                {
                    var audioDevice = AddFilterByDevicePath(m_graph,
                                                        FilterCategory.AudioInputDevice,
                                                        audioDevices[0].DevicePath);

                    hr = graphBuilder.RenderStream(PinCategory.Capture, MediaType.Audio, audioDevice, null, mux);
                    DsError.ThrowExceptionForHR(hr);
                }
            }

            hr = graphBuilder.RenderStream(PinCategory.Preview,
                                           MediaType.Video,
                                           m_captureDevice,
                                           null,
                                           m_renderer);

            DsError.ThrowExceptionForHR(hr);

            /* Register the filter graph
             * with the base classes */
            SetupFilterGraph(m_graph);

            /* Sets the NaturalVideoWidth/Height */
            SetNativePixelSizes(m_renderer);

            HasVideo = true;

            /* Make sure we Release() this COM reference */
            if (mux != null)
            {
                Marshal.ReleaseComObject(mux);
            }
            if (sink != null)
            {
                Marshal.ReleaseComObject(sink);
            }

            Marshal.ReleaseComObject(graphBuilder);
        }
        catch (Exception ex)
        {
            /* Something got fuct up */
            FreeResources();
            InvokeMediaFailed(new MediaFailedEventArgs(ex.Message, ex));
        }

        /* Success */
        InvokeMediaOpened();
    }

    /// <summary>
    /// Sets the capture parameters for the video capture device
    /// </summary>
    private bool SetVideoCaptureParameters(ICaptureGraphBuilder2 capGraph, IBaseFilter captureFilter, Guid mediaSubType)
    {
        /* The stream config interface */
        object streamConfig;

        /* Get the stream's configuration interface */
        int hr = capGraph.FindInterface(PinCategory.Capture,
                                        MediaType.Video,
                                        captureFilter,
                                        typeof(IAMStreamConfig).GUID,
                                        out streamConfig);

        DsError.ThrowExceptionForHR(hr);

        var videoStreamConfig = streamConfig as IAMStreamConfig;

        /* If QueryInterface fails... */
        if (videoStreamConfig == null)
        {
            throw new WPFMediaKitException("Failed to get IAMStreamConfig");
        }

        /* The media type of the video */
        AMMediaType media;

        /* Get the AMMediaType for the video out pin */
        hr = videoStreamConfig.GetFormat(out media);
        DsError.ThrowExceptionForHR(hr);

        /* Make the VIDEOINFOHEADER 'readable' */
        var videoInfo = new VideoInfoHeader();
        Marshal.PtrToStructure(media.formatPtr, videoInfo);

        /* Setup the VIDEOINFOHEADER with the parameters we want */
        videoInfo.AvgTimePerFrame = DSHOW_ONE_SECOND_UNIT / FPS;
        videoInfo.BmiHeader.Width = DesiredWidth;
        videoInfo.BmiHeader.Height = DesiredHeight;

        if (mediaSubType != Guid.Empty)
        {
            int fourCC = 0;
            byte[] b = mediaSubType.ToByteArray();
            fourCC = b[0];
            fourCC |= b[1] << 8;
            fourCC |= b[2] << 16;
            fourCC |= b[3] << 24;

            videoInfo.BmiHeader.Compression = fourCC;
            media.subType = mediaSubType;
        }

        /* Copy the data back to unmanaged memory */
        Marshal.StructureToPtr(videoInfo, media.formatPtr, false);

        /* Set the format */
        hr = videoStreamConfig.SetFormat(media);

        /* We don't want any memory leaks, do we? */
        DsUtils.FreeAMMediaType(media);

        if (hr < 0)
            return false;

        return true;
    }

    private Bitmap m_videoFrame;

    private void InitializeBitmapFrame(int width, int height)
    {
        if (m_videoFrame != null)
        {
            m_videoFrame.Dispose();
        }

        m_videoFrame = new Bitmap(width, height, PixelFormat.Format24bppRgb);
    }

    #region ISampleGrabberCB Members

    int ISampleGrabberCB.SampleCB(double sampleTime, IMediaSample pSample)
    {
        var mediaType = new AMMediaType();

        /* We query for the media type the sample grabber is using */
        int hr = m_sampleGrabber.GetConnectedMediaType(mediaType);

        var videoInfo = new VideoInfoHeader();

        /* 'Cast' the pointer to our managed struct */
        Marshal.PtrToStructure(mediaType.formatPtr, videoInfo);

        /* The stride is "How many bytes across for each pixel line (0 to width)" */
        int stride = Math.Abs(videoInfo.BmiHeader.Width * (videoInfo.BmiHeader.BitCount / 8 /* eight bits per byte */));
        int width = videoInfo.BmiHeader.Width;
        int height = videoInfo.BmiHeader.Height;

        if (m_videoFrame == null)
            InitializeBitmapFrame(width, height);

        if (m_videoFrame == null)
            return 0;

        BitmapData bmpData = m_videoFrame.LockBits(new Rectangle(0, 0, width, height),
                                                   ImageLockMode.ReadWrite,
                                                   PixelFormat.Format24bppRgb);

        /* Get the pointer to the pixels */
        IntPtr pBmp = bmpData.Scan0;

        IntPtr samplePtr;

        /* Get the native pointer to the sample */
        pSample.GetPointer(out samplePtr);

        int pSize = stride * height;

        /* Copy the memory from the sample pointer to our bitmap pixel pointer */
        CopyMemory(pBmp, samplePtr, pSize);

        m_videoFrame.UnlockBits(bmpData);

        InvokeNewVideoSample(new VideoSampleArgs { VideoFrame = m_videoFrame });

        DsUtils.FreeAMMediaType(mediaType);

        /* Dereference the sample COM object */
        Marshal.ReleaseComObject(pSample);
        return 0;
    }

    int ISampleGrabberCB.BufferCB(double sampleTime, IntPtr pBuffer, int bufferLen)
    {
        throw new NotImplementedException();
    }

    #endregion ISampleGrabberCB Members

    private void SetupSampleGrabber(ISampleGrabber sampleGrabber)
    {
        var mediaType = new DirectShowLib.AMMediaType
        {
            majorType = MediaType.Video,
            subType = MediaSubType.RGB24,
            formatType = FormatType.VideoInfo
        };

        int hr = sampleGrabber.SetMediaType(mediaType);

        DsUtils.FreeAMMediaType(mediaType);
        DsError.ThrowExceptionForHR(hr);

        hr = sampleGrabber.SetCallback(this, 0);
        DsError.ThrowExceptionForHR(hr);
    }

    protected override void FreeResources()
    {
        /* We run the StopInternal() to avoid any
         * Dispatcher VeryifyAccess() issues */
        StopInternal();

        /* Let's clean up the base
         * class's stuff first */
        base.FreeResources();

#if DEBUG
        if (m_rotEntry != null)
            m_rotEntry.Dispose();

        m_rotEntry = null;
#endif
        if (m_videoFrame != null)
        {
            m_videoFrame.Dispose();
            m_videoFrame = null;
        }
        if (m_renderer != null)
        {
            Marshal.FinalReleaseComObject(m_renderer);
            m_renderer = null;
        }
        if (m_captureDevice != null)
        {
            Marshal.FinalReleaseComObject(m_captureDevice);
            m_captureDevice = null;
        }
        if (m_sampleGrabber != null)
        {
            Marshal.FinalReleaseComObject(m_sampleGrabber);
            m_sampleGrabber = null;
        }
        if (m_graph != null)
        {
            Marshal.FinalReleaseComObject(m_graph);
            m_graph = null;

            InvokeMediaClosed(EventArgs.Empty);
        }
    }
}
