using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using WPFMediaKit.DirectShow.MediaPlayers;

namespace WPFMediaKit.DirectShow.Controls;

public class DvdPlayerElement : MediaSeekingElement
{
    #region Commands

    public static readonly RoutedCommand PlayTitleCommand = new RoutedCommand();
    public static readonly RoutedCommand GotoTitleMenuCommand = new RoutedCommand();
    public static readonly RoutedCommand GotoRootMenuCommand = new RoutedCommand();
    public static readonly RoutedCommand NextChapterCommand = new RoutedCommand();
    public static readonly RoutedCommand PreviousChapterCommand = new RoutedCommand();

    private void OnPlayTitleCommandExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is int == false)
            return;

        var title = (int)e.Parameter;

        PlayTitle(title);
    }

    private void OnCanExecutePlayTitleCommand(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = IsPlaying;
    }

    private void OnGotoTitleCommandExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        GotoTitleMenu();
    }

    private void OnCanExecuteGotoTitleCommand(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = IsPlaying;
    }

    private void OnGotoRootMenuExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        GotoRootMenu();
    }

    private void OnCanExecuteGotoRootMenu(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = IsPlaying;
    }

    private void OnNextChapterCommandExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        PlayNextChapter();
    }

    private void OnCanNextChapterCommand(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = IsPlaying;
    }

    private void OnPreviousChapterCommandExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        PlayPreviousChapter();
    }

    private void OnCanPreviousChapterCommand(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = IsPlaying;
    }

    #endregion Commands

    public DvdPlayerElement()
    {
        //RenderOnCompositionTargetRendering = true;

        CommandBindings.Add(new CommandBinding(GotoRootMenuCommand,
                                               OnGotoRootMenuExecuted,
                                               OnCanExecuteGotoRootMenu));

        CommandBindings.Add(new CommandBinding(GotoTitleMenuCommand,
                                               OnGotoTitleCommandExecuted,
                                               OnCanExecuteGotoTitleCommand));

        CommandBindings.Add(new CommandBinding(PlayTitleCommand,
                                   OnPlayTitleCommandExecuted,
                                   OnCanExecutePlayTitleCommand));

        CommandBindings.Add(new CommandBinding(PreviousChapterCommand,
                                   OnPreviousChapterCommandExecuted,
                                   OnCanPreviousChapterCommand));

        CommandBindings.Add(new CommandBinding(NextChapterCommand,
                       OnNextChapterCommandExecuted,
                       OnCanNextChapterCommand));
    }

    /// <summary>
    /// Fires when a DVD specific error occurs
    /// </summary>
    public event EventHandler<DvdErrorArgs> DvdError;

    private void InvokeDvdError(DvdError error)
    {
        var e = new DvdErrorArgs { Error = error };
        var dvdErrorHandler = DvdError;
        if (dvdErrorHandler != null) dvdErrorHandler(this, e);
    }

    #region Dependency Properties

    #region IsOverDvdButton

    private static readonly DependencyPropertyKey IsOverDvdButtonPropertyKey
        = DependencyProperty.RegisterReadOnly("IsOverDvdButton", typeof(bool), typeof(DvdPlayerElement),
            new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty IsOverDvdButtonProperty
        = IsOverDvdButtonPropertyKey.DependencyProperty;

    /// <summary>
    /// Gets if the mouse is over a DVD button.  This is a dependency property.
    /// </summary>
    public bool IsOverDvdButton
    {
        get { return (bool)GetValue(IsOverDvdButtonProperty); }
    }

    protected void SetIsOverDvdButton(bool value)
    {
        SetValue(IsOverDvdButtonPropertyKey, value);
    }

    #endregion IsOverDvdButton

    #region PlayOnInsert

    public static readonly DependencyProperty PlayOnInsertProperty =
        DependencyProperty.Register("PlayOnInsert", typeof(bool), typeof(DvdPlayerElement),
                                    new FrameworkPropertyMetadata(true,
                                                                  new PropertyChangedCallback(OnPlayOnInsertChanged)));

    /// <summary>
    /// Gets or sets if the DVD automatically plays when a DVD is inserted into the computer.
    /// This is a dependency property.
    /// </summary>
    public bool PlayOnInsert
    {
        get { return (bool)GetValue(PlayOnInsertProperty); }
        set { SetValue(PlayOnInsertProperty, value); }
    }

    private static void OnPlayOnInsertChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((DvdPlayerElement)d).OnPlayOnInsertChanged(e);
    }

    protected virtual void OnPlayOnInsertChanged(DependencyPropertyChangedEventArgs e)
    {
    }

    #endregion PlayOnInsert

    #region DvdEjected

    public static readonly RoutedEvent DvdEjectedEvent = EventManager.RegisterRoutedEvent("DvdEjected",
                                                                                          RoutingStrategy.Bubble,
                                                                                          typeof(RoutedEventHandler),
                                                                                          typeof(DvdPlayerElement));

    /// <summary>
    /// This event is fired when a DVD is ejected from the computer.  This is a bubbled, routed event.
    /// </summary>
    public event RoutedEventHandler DvdEjected
    {
        add { AddHandler(DvdEjectedEvent, value); }
        remove { RemoveHandler(DvdEjectedEvent, value); }
    }

    #endregion DvdEjected

    #region DvdInserted

    public static readonly RoutedEvent DvdInsertedEvent = EventManager.RegisterRoutedEvent("DvdInserted",
                                                                                           RoutingStrategy.Bubble,
                                                                                           typeof(RoutedEventHandler),
                                                                                           typeof(DvdPlayerElement));

    /// <summary>
    /// Fires when a DVD is inserted into the computer.
    /// This is a bubbled, routed event.
    /// </summary>
    public event RoutedEventHandler DvdInserted
    {
        add { AddHandler(DvdInsertedEvent, value); }
        remove { RemoveHandler(DvdInsertedEvent, value); }
    }

    #endregion DvdInserted

    #region CurrentDvdTime

    private static readonly DependencyPropertyKey CurrentDvdTimePropertyKey
        = DependencyProperty.RegisterReadOnly("CurrentDvdTime", typeof(TimeSpan), typeof(DvdPlayerElement),
            new FrameworkPropertyMetadata(TimeSpan.Zero));

    public static readonly DependencyProperty CurrentDvdTimeProperty
        = CurrentDvdTimePropertyKey.DependencyProperty;

    /// <summary>
    /// Gets the current time the DVD playback is at.  This is a read-only,
    /// dependency property.
    /// </summary>
    public TimeSpan CurrentDvdTime
    {
        get { return (TimeSpan)GetValue(CurrentDvdTimeProperty); }
    }

    protected void SetCurrentDvdTime(TimeSpan value)
    {
        SetValue(CurrentDvdTimePropertyKey, value);
    }

    #endregion CurrentDvdTime

    #region DvdDirectory

    public static readonly DependencyProperty DvdDirectoryProperty =
        DependencyProperty.Register("DvdDirectory", typeof(string), typeof(DvdPlayerElement),
            new FrameworkPropertyMetadata("",
                new PropertyChangedCallback(OnDvdDirectoryChanged)));

    /// <summary>
    /// Gets or sets the directory the DVD is located at (ie D:\VIDEO_TS).  If this is empty or null,
    /// then DirectShow will try to play the first DVD found in the computer.
    /// This is a dependency property.
    /// </summary>
    public string DvdDirectory
    {
        get { return (string)GetValue(DvdDirectoryProperty); }
        set { SetValue(DvdDirectoryProperty, value); }
    }

    private static void OnDvdDirectoryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((DvdPlayerElement)d).OnDvdDirectoryChanged(e);
    }

    protected virtual void OnDvdDirectoryChanged(DependencyPropertyChangedEventArgs e)
    {
        if (HasInitialized)
            PlayerSetDvdDirectory();
    }

    private void PlayerSetDvdDirectory()
    {
        var dvdDirectory = DvdDirectory;

        DvdPlayer.Dispatcher.BeginInvoke((Action)delegate
        {
            /* Set the source type */
            DvdPlayer.DvdDirectory = dvdDirectory;

            Dispatcher.BeginInvoke((Action)delegate
            {
                if (IsLoaded)
                    ExecuteMediaState(LoadedBehavior);
                //else
                //    ExecuteMediaState(UnloadedBehavior);
            });
        });
    }

    #endregion DvdDirectory

    #endregion Dependency Properties

    public override void EndInit()
    {
        PlayerSetDvdDirectory();
        base.EndInit();
    }

    #region Public Methods

    /// <summary>
    /// The SelectAngle method sets the new angle when the DVD Navigator is in an angle block
    /// </summary>
    /// <param name="angle">Value of the new angle, which must be from 1 through 9</param>
    public void SelectAngle(int angle)
    {
        DvdPlayer.Dispatcher.BeginInvoke((Action)(() => DvdPlayer.SelectAngle(angle)));
    }

    /// <summary>
    /// Returns the display from a submenu to its parent menu.
    /// </summary>
    public void ReturnFromSubmenu()
    {
        DvdPlayer.Dispatcher.BeginInvoke((Action)(() => DvdPlayer.ReturnFromSubmenu()));
    }

    /// <summary>
    /// Selects the specified relative button (upper, lower, right, left)
    /// </summary>
    /// <param name="button">Value indicating the button to select</param>
    public void SelectRelativeButton(DvdRelativeButtonEnum button)
    {
        DvdPlayer.Dispatcher.BeginInvoke((Action)(() => DvdPlayer.SelectRelativeButton(button)));
    }

    /// <summary>
    /// Leaves a menu and resumes playback.
    /// </summary>
    public void Resume()
    {
        DvdPlayer.Dispatcher.BeginInvoke((Action)(() => DvdPlayer.Resume()));
    }

    /// <summary>
    /// Plays the DVD forward at a specific speed
    /// </summary>
    /// <param name="speed">The speed multiplier to play back.</param>
    public void PlayForwards(double speed)
    {
        DvdPlayer.Dispatcher.BeginInvoke((Action)(() => DvdPlayer.PlayForwards(speed)));
    }

    /// <summary>
    /// Plays the DVD backwards at a specific speed
    /// </summary>
    /// <param name="speed">The speed multiplier to play back</param>
    public void PlayBackwards(double speed)
    {
        DvdPlayer.Dispatcher.BeginInvoke((Action)(() => DvdPlayer.PlayBackwards(speed)));
    }

    /// <summary>
    /// Play a title
    /// </summary>
    /// <param name="titleIndex">The index of the title to play back</param>
    public void PlayTitle(int titleIndex)
    {
        DvdPlayer.Dispatcher.BeginInvoke((Action)(() => DvdPlayer.PlayTitle(titleIndex)));
    }

    /// <summary>
    /// Plays the next chapter in the volume.
    /// </summary>
    public void PlayNextChapter()
    {
        DvdPlayer.Dispatcher.BeginInvoke((Action)(() => DvdPlayer.PlayNextChapter()));
    }

    /// <summary>
    /// Plays the previous chapter in the volume.
    /// </summary>
    public void PlayPreviousChapter()
    {
        DvdPlayer.Dispatcher.BeginInvoke((Action)(() => DvdPlayer.PlayPreviousChapter()));
    }

    /// <summary>
    /// Goes to the root menu of the DVD.
    /// </summary>
    public void GotoRootMenu()
    {
        DvdPlayer.Dispatcher.BeginInvoke((Action)(() => DvdPlayer.GotoRootMenu()));
    }

    /// <summary>
    /// Goes to the title menu of the DVD
    /// </summary>
    public void GotoTitleMenu()
    {
        DvdPlayer.Dispatcher.BeginInvoke((Action)(() => DvdPlayer.GotoTitleMenu()));
    }

    /// <summary>
    /// The Play method is overrided so we can
    /// set the source to the media
    /// </summary>
    public override void Play()
    {
        var prop = DesignerProperties.IsInDesignModeProperty;
        bool isInDesignMode = (bool)DependencyPropertyDescriptor
                    .FromProperty(prop, typeof(FrameworkElement))
                    .Metadata.DefaultValue;

        if (isInDesignMode)
            return;

        base.Play();
    }

    #endregion Public Methods

    #region Protected Methods

    protected DvdPlayer DvdPlayer
    {
        get { return MediaPlayerBase as DvdPlayer; }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        /* Get the position of the mouse over the video image */
        Point position = e.GetPosition(VideoImage);

        /* Calculate the ratio of where the mouse is, to the actual width of the video. */
        double widthMultiplier = position.X / VideoImage.ActualWidth;

        /* Calculate the ratio of where the mouse is, to the actual height of the video */
        double heightMultiplier = position.Y / VideoImage.ActualHeight;

        DvdPlayer.Dispatcher.BeginInvoke((Action)(() => DvdPlayer.SelectAtPosition(widthMultiplier, heightMultiplier)));
        base.OnMouseMove(e);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        Point position = e.GetPosition(VideoImage);

        /* Calculate the ratio of where the mouse is, to the actual width of the video. */
        double widthMultiplier = position.X / VideoImage.ActualWidth;

        /* Calculate the ratio of where the mouse is, to the actual height of the video */
        double heightMultiplier = position.Y / VideoImage.ActualHeight;

        DvdPlayer.Dispatcher.BeginInvoke((Action)(() => DvdPlayer.ActivateAtPosition(widthMultiplier, heightMultiplier)));

        base.OnMouseLeftButtonDown(e);
    }

    protected override MediaPlayerBase OnRequestMediaPlayer()
    {
        /* Initialize the DVD player and hook into it's events */
        var player = new DvdPlayer();
        player.OnDvdEjected += DvdPlayerOnDvdEjected;
        player.OnDvdInserted += DvdPlayerOnDvdInserted;
        player.OnOverDvdButton += DvdPlayerOnOverDvdButton;
        player.OnDvdTime += DvdPlayerOnDvdTime;
        player.OnDvdError += DvdPlayerOnDvdError;
        return player;
    }

    #endregion Protected Methods

    #region Private Methods

    private void DvdPlayerOnDvdError(object sender, DvdErrorArgs e)
    {
        Dispatcher.BeginInvoke((Action)(() => InvokeDvdError(e.Error)));
    }

    /// <summary>
    /// The handler for when a new DVD is hit.  The event is fired by the DVDPlayer class.
    /// </summary>
    private void DvdPlayerOnDvdTime(object sender, DvdTimeEventArgs e)
    {
        Dispatcher.BeginInvoke((Action)(() => SetCurrentDvdTime(e.DvdTime)));
    }

    /// <summary>
    /// The handler for when the mouse is over a DVD button.  This event is fired by the DVD Player class.
    /// </summary>
    private void DvdPlayerOnOverDvdButton(object sender, OverDvdButtonEventArgs e)
    {
        Dispatcher.BeginInvoke((Action)(() => SetIsOverDvdButton(e.IsOverDvdButton)));
    }

    /// <summary>
    /// Fires when a new DVD is inserted into a DVD player on the computer.
    /// </summary>
    private void DvdPlayerOnDvdInserted(object sender, EventArgs e)
    {
        Dispatcher.BeginInvoke((Action)delegate
        {
            RaiseEvent(new RoutedEventArgs(DvdInsertedEvent));

            if (PlayOnInsert)
                Play();
        });
    }

    /// <summary>
    /// Fires when the DVD is ejected from the computer.
    /// </summary>
    private void DvdPlayerOnDvdEjected(object sender, EventArgs e)
    {
        Dispatcher.BeginInvoke((Action)(() => RaiseEvent(new RoutedEventArgs(DvdEjectedEvent))));
    }

    #endregion Private Methods
}
