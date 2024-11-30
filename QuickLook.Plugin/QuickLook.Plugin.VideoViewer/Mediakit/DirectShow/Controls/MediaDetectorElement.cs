using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WPFMediaKit.DirectShow.MediaPlayers;

namespace WPFMediaKit.DirectShow.Controls;

public class VideoFrame
{
    public VideoFrame(BitmapSource snapshot, TimeSpan mediaTime)
    {
        Snapshot = snapshot;
        MediaTime = mediaTime;
        snapshot.Freeze();
    }

    public BitmapSource Snapshot { get; private set; }

    public TimeSpan MediaTime { get; private set; }
}

/// <summary>
/// This control is not finished.  Do not use it ;)
/// </summary>
public class MediaDetectorElement : Control
{
    private readonly ObservableCollection<VideoFrame> m_frames;
    private readonly MediaDetector m_mediaDetector;

    private bool m_cancelLoadFrames;
    private double m_lastEndTime;
    private int m_lastFrameCount;
    private double m_lastStartTime;
    private ItemsControl m_videoFrameItems;

    static MediaDetectorElement()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MediaDetectorElement),
                                                 new FrameworkPropertyMetadata(typeof(MediaDetectorElement)));
    }

    public MediaDetectorElement()
    {
        m_frames = new ObservableCollection<VideoFrame>();
        Frames = new ReadOnlyObservableCollection<VideoFrame>(m_frames);

        m_mediaDetector = CreateMediaDetector();
        Loaded += MediaDetectorElement_Loaded;
    }

    public ReadOnlyObservableCollection<VideoFrame> Frames { get; private set; }

    #region MediaLength

    private static readonly DependencyPropertyKey MediaLengthPropertyKey
        = DependencyProperty.RegisterReadOnly("MediaLength", typeof(double), typeof(MediaDetectorElement),
                                              new FrameworkPropertyMetadata((double)0));

    public static readonly DependencyProperty MediaLengthProperty
        = MediaLengthPropertyKey.DependencyProperty;

    public double MediaLength
    {
        get { return (double)GetValue(MediaLengthProperty); }
    }

    protected void SetMediaLength(double value)
    {
        SetValue(MediaLengthPropertyKey, value);
    }

    #endregion MediaLength

    #region MediaSource

    public static readonly DependencyProperty MediaSourceProperty =
        DependencyProperty.Register("MediaSource", typeof(Uri), typeof(MediaDetectorElement),
                                    new FrameworkPropertyMetadata(null,
                                                                  new PropertyChangedCallback(OnMediaSourceChanged)));

    public Uri MediaSource
    {
        get { return (Uri)GetValue(MediaSourceProperty); }
        set { SetValue(MediaSourceProperty, value); }
    }

    private static void OnMediaSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((MediaDetectorElement)d).OnMediaSourceChanged(e);
    }

    protected virtual void OnMediaSourceChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue == null)
            return;

        if (IsLoaded)
            LoadMediaSource();
    }

    #endregion MediaSource

    #region VideoStartTime

    public static readonly DependencyProperty VideoStartTimeProperty =
        DependencyProperty.Register("VideoStartTime", typeof(double), typeof(MediaDetectorElement),
                                    new FrameworkPropertyMetadata((double)0,
                                                                  new PropertyChangedCallback(
                                                                      OnVideoStartTimeChanged)));

    public double VideoStartTime
    {
        get { return (double)GetValue(VideoStartTimeProperty); }
        set { SetValue(VideoStartTimeProperty, value); }
    }

    private static void OnVideoStartTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((MediaDetectorElement)d).OnVideoStartTimeChanged(e);
    }

    protected virtual void OnVideoStartTimeChanged(DependencyPropertyChangedEventArgs e)
    {
        if (IsLoaded)
            LoadVideoFrames();
    }

    #endregion VideoStartTime

    #region VideoEndTime

    public static readonly DependencyProperty VideoEndTimeProperty =
        DependencyProperty.Register("VideoEndTime", typeof(double), typeof(MediaDetectorElement),
                                    new FrameworkPropertyMetadata((double)0,
                                                                  new PropertyChangedCallback(OnVideoEndTimeChanged)));

    public double VideoEndTime
    {
        get { return (double)GetValue(VideoEndTimeProperty); }
        set { SetValue(VideoEndTimeProperty, value); }
    }

    private static void OnVideoEndTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((MediaDetectorElement)d).OnVideoEndTimeChanged(e);
    }

    protected virtual void OnVideoEndTimeChanged(DependencyPropertyChangedEventArgs e)
    {
        if (Parent != null)
            LoadVideoFrames();
    }

    #endregion VideoEndTime

    #region VideoFrameCount

    public static readonly DependencyProperty VideoFrameCountProperty =
        DependencyProperty.Register("VideoFrameCount", typeof(int), typeof(MediaDetectorElement),
                                    new FrameworkPropertyMetadata(0,
                                                                  new PropertyChangedCallback(
                                                                      OnVideoFrameCountChanged)));

    public int VideoFrameCount
    {
        get { return (int)GetValue(VideoFrameCountProperty); }
        set { SetValue(VideoFrameCountProperty, value); }
    }

    private static void OnVideoFrameCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((MediaDetectorElement)d).OnVideoFrameCountChanged(e);
    }

    protected virtual void OnVideoFrameCountChanged(DependencyPropertyChangedEventArgs e)
    {
        if (IsLoaded)
            LoadVideoFrames();
    }

    #endregion VideoFrameCount

    private void LoadMediaSource()
    {
        if (MediaSource == null)
            return;

        string filename = MediaSource.OriginalString;

        if (m_mediaDetector == null)
            return;

        m_mediaDetector.Dispatcher.BeginInvoke((Action)delegate
        {
            m_mediaDetector.LoadMedia(filename);
            SetMediaInfo();
        });
    }

    private void LoadVideoFrames()
    {
        double startTime = 0;
        double endTime = 0;
        int videoFrameCount = 0;

        startTime = VideoStartTime;
        endTime = VideoEndTime;
        videoFrameCount = VideoFrameCount;

        if (startTime == endTime)
            videoFrameCount = 1;

        if (endTime < startTime)
        {
            m_frames.Clear();
            return;
        }

        if (m_lastStartTime == startTime && m_lastEndTime == endTime && videoFrameCount == m_lastFrameCount)
            return;

        m_lastStartTime = startTime;
        m_lastEndTime = endTime;
        m_lastFrameCount = videoFrameCount;

        m_cancelLoadFrames = true;

        double timeIncrement = (endTime - startTime) / videoFrameCount;

        var times = new List<TimeSpan>();
        double sec = startTime;

        for (int i = 0; i < videoFrameCount; i++)
        {
            times.Add(TimeSpan.FromSeconds(sec));
            sec += timeIncrement;
        }

        m_mediaDetector.Dispatcher.BeginInvoke((Action)delegate
        {
            m_cancelLoadFrames = false;
            Dispatcher.Invoke((Action)(() => m_frames.Clear()));
            for (int i = 0; i < times.Count; i++)
            {
                if (m_cancelLoadFrames)
                    return;

                var frame = new VideoFrame(m_mediaDetector.GetImage(times[i]),
                                           times[i]);

                Dispatcher.Invoke((Action)(() => m_frames.Add(frame)));
            }

            var videoframe = new VideoFrame(m_mediaDetector.GetImage(TimeSpan.FromSeconds(endTime)),
                                            TimeSpan.FromSeconds(endTime));

            Dispatcher.Invoke((Action)(() => m_frames.Add(videoframe)));

            m_cancelLoadFrames = false;
        });
    }

    private void SetMediaInfo()
    {
        Dispatcher.BeginInvoke(
            (Action)
            delegate
            {
                SetMediaLength(Math.Max(m_mediaDetector.AudioStreamLength.TotalSeconds, m_mediaDetector.VideoStreamLength.TotalSeconds));
                LoadVideoFrames();
            });
    }

    private void MediaDetectorElement_Loaded(object sender, RoutedEventArgs e)
    {
        LoadMediaSource();
    }

    public override void OnApplyTemplate()
    {
        m_videoFrameItems = GetTemplateChild("PART_VideoFrameItems") as ItemsControl;

        if (m_videoFrameItems != null)
            m_videoFrameItems.ItemsSource = m_frames;

        base.OnApplyTemplate();
    }

    private static MediaDetector CreateMediaDetector()
    {
        MediaDetector detector = null;

        /* The reset event will block our thread while
         * we create an intialize the player */
        var reset = new ManualResetEventSlim(false);

        /* We need to create a new thread for our Dispatcher */
        var t = new Thread((ThreadStart)delegate
        {
            detector = new MediaDetector();

            /* We queue up a method to execute
             * when the Dispatcher is ran.
             * This will wake up the calling thread
             * that has been blocked by the reset event */
            detector.Dispatcher.Invoke((Action)(() => reset.Set()));

            Dispatcher.Run();
        })
        {
            Name = "MediaDetector",
            IsBackground = true
        };

        t.SetApartmentState(ApartmentState.STA);

        /* Starts the thread and creates the object */
        t.Start();

        /* We wait until our object is created and
         * the new Dispatcher is running */
        reset.Wait();

        return detector;
    }
}
