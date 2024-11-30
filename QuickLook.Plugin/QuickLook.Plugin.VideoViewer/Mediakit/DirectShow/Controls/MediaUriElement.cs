using System;
using System.Windows;
using System.Windows.Threading;
using WPFMediaKit.DirectShow.MediaPlayers;

namespace WPFMediaKit.DirectShow.Controls;

/// <summary>
/// The MediaUriElement is a WPF control that plays media of a given
/// Uri. The Uri can be a file path or a Url to media.  The MediaUriElement
/// inherits from the MediaSeekingElement, so where available, seeking is
/// also supported.
/// </summary>
public class MediaUriElement : MediaSeekingElement
{
    /// <summary>
    /// The current MediaUriPlayer
    /// </summary>
    public MediaUriPlayer MediaUriPlayer
    {
        get
        {
            return MediaPlayerBase as MediaUriPlayer;
        }
    }

    #region VideoRenderer

    public static readonly DependencyProperty VideoRendererProperty =
        DependencyProperty.Register("VideoRenderer", typeof(VideoRendererType), typeof(MediaUriElement),
            new FrameworkPropertyMetadata(VideoRendererType.EnhancedVideoRenderer,
                new PropertyChangedCallback(OnVideoRendererChanged)));

    public VideoRendererType VideoRenderer
    {
        get { return (VideoRendererType)GetValue(VideoRendererProperty); }
        set { SetValue(VideoRendererProperty, value); }
    }

    private static void OnVideoRendererChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((MediaUriElement)d).OnVideoRendererChanged(e);
    }

    protected virtual void OnVideoRendererChanged(DependencyPropertyChangedEventArgs e)
    {
        if (HasInitialized)
            PlayerSetVideoRenderer();
    }

    private void PlayerSetVideoRenderer()
    {
        var videoRendererType = VideoRenderer;
        MediaUriPlayer.Dispatcher.BeginInvoke((Action)delegate
        {
            MediaUriPlayer.VideoRenderer = videoRendererType;
        });
    }

    #endregion VideoRenderer

    /// <summary>
    /// Step the count of frames.
    /// </summary>
    /// <param name="framecount">count of frames to step</param>
    public void FrameStep(int framecount)
    {
        MediaUriPlayer.Dispatcher.BeginInvoke((Action)delegate
        {
            MediaUriPlayer.StepFrame(framecount);
        });
    }

    #region AudioRenderer

    public static readonly DependencyProperty AudioRendererProperty =
        DependencyProperty.Register("AudioRenderer", typeof(string), typeof(MediaUriElement),
            new FrameworkPropertyMetadata(MediaUriPlayer.DEFAULT_AUDIO_RENDERER_NAME,
                new PropertyChangedCallback(OnAudioRendererChanged)));

    /// <summary>
    /// The name of the audio renderer device to use. Null to disable audio.
    /// </summary>
    public string AudioRenderer
    {
        get { return (string)GetValue(AudioRendererProperty); }
        set { SetValue(AudioRendererProperty, value); }
    }

    private static void OnAudioRendererChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((MediaUriElement)d).OnAudioRendererChanged(e);
    }

    protected virtual void OnAudioRendererChanged(DependencyPropertyChangedEventArgs e)
    {
        if (HasInitialized)
            PlayerSetAudioRenderer();
    }

    private void PlayerSetAudioRenderer()
    {
        var audioDevice = AudioRenderer;

        MediaUriPlayer.Dispatcher.BeginInvoke((Action)delegate
        {
            /* Sets the audio device to use with the player */
            MediaUriPlayer.AudioRenderer = audioDevice;
        });
    }

    #endregion AudioRenderer

    #region Source

    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register("Source", typeof(Uri), typeof(MediaUriElement),
            new FrameworkPropertyMetadata(null,
                new PropertyChangedCallback(OnSourceChanged)));

    /// <summary>
    /// The Uri source to the media.  This can be a file path or a
    /// URL source
    /// </summary>
    public Uri Source
    {
        get { return (Uri)GetValue(SourceProperty); }
        set { SetValue(SourceProperty, value); }
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((MediaUriElement)d).OnSourceChanged(e);
    }

    protected void OnSourceChanged(DependencyPropertyChangedEventArgs e)
    {
        if (HasInitialized)
            PlayerSetSource();
    }

    private void PlayerSetSource()
    {
        var source = Source;
        var rendererType = VideoRenderer;

        MediaPlayerBase.Dispatcher.BeginInvoke((Action)delegate
        {
            /* Set the renderer type */
            MediaUriPlayer.VideoRenderer = rendererType;

            /* Set the source type */
            MediaUriPlayer.Source = source;

            Dispatcher.BeginInvoke((Action)delegate
            {
                if (IsLoaded)
                    ExecuteMediaState(LoadedBehavior);
                //else
                //    ExecuteMediaState(UnloadedBehavior);
            });
        });
    }

    #endregion Source

    #region Loop

    public static readonly DependencyProperty LoopProperty =
        DependencyProperty.Register("Loop", typeof(bool), typeof(MediaUriElement),
            new FrameworkPropertyMetadata(false,
                new PropertyChangedCallback(OnLoopChanged)));

    /// <summary>
    /// Gets or sets whether the media should return to the begining
    /// once the end has reached
    /// </summary>
    public bool Loop
    {
        get { return (bool)GetValue(LoopProperty); }
        set { SetValue(LoopProperty, value); }
    }

    private static void OnLoopChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((MediaUriElement)d).OnLoopChanged(e);
    }

    protected virtual void OnLoopChanged(DependencyPropertyChangedEventArgs e)
    {
        if (HasInitialized)
            PlayerSetLoop();
    }

    private void PlayerSetLoop()
    {
        var loop = Loop;
        MediaPlayerBase.Dispatcher.BeginInvoke((Action)delegate
        {
            MediaUriPlayer.Loop = loop;
        });
    }

    #endregion Loop

    public override void EndInit()
    {
        PlayerSetVideoRenderer();
        PlayerSetAudioRenderer();
        PlayerSetLoop();
        PlayerSetSource();
        base.EndInit();
    }

    public void DumpGraphInfo(string fileName)
    {
        MediaUriPlayer.DumpGraphInfo(fileName);
    }

    /// <summary>
    /// The Play method is overrided so we can
    /// set the source to the media
    /// </summary>
    public override void Play()
    {
        EnsurePlayerThread();
        base.Play();
    }

    /// <summary>
    /// The Pause method is overrided so we can
    /// set the source to the media
    /// </summary>
    public override void Pause()
    {
        EnsurePlayerThread();

        base.Pause();
    }

    /// <summary>
    /// Gets the instance of the media player to initialize
    /// our base classes with
    /// </summary>
    protected override MediaPlayerBase OnRequestMediaPlayer()
    {
        var player = new MediaUriPlayer();
        return player;
    }
}
