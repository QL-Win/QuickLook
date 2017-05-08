using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FontAwesome.WPF;
using Unosquare.FFmpegMediaElement;

namespace QuickLook.Plugin.VideoViewer
{
    /// <summary>
    ///     Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class ViewerPanel : UserControl, IDisposable
    {
        public ViewerPanel()
        {
            InitializeComponent();

            buttonPlayPause.MouseLeftButtonUp += TogglePlayPause;

            mediaElement.PropertyChanged += ChangePlayPauseButton;
            mediaElement.MouseLeftButtonUp += TogglePlayPause;
            mediaElement.MediaErrored += ShowErrorOverlay;
            mediaElement.MediaFailed += ShowErrorOverlay;
        }

        public void Dispose()
        {
            mediaElement?.Dispose();
        }

        private void TogglePlayPause(object sender, MouseButtonEventArgs e)
        {
            if (mediaElement.IsPlaying)
                mediaElement.Pause();
            else
                mediaElement.Play();
        }

        private void ChangePlayPauseButton(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "IsPlaying" && e.PropertyName != "HasMediaEnded")
                return;

            buttonPlayPause.Icon = mediaElement.IsPlaying
                ? FontAwesomeIcon.PauseCircleOutline
                : FontAwesomeIcon.PlayCircleOutline;
        }

        private void ShowErrorOverlay(object sender, MediaErrorRoutedEventArgs e)
        {
            mediaElement.Stop();
            errorOverlay.Visibility = Visibility.Visible;
        }

        public void LoadAndPlay(string path)
        {
            mediaElement.Source = new Uri(path);
            mediaElement.Play();
        }

        ~ViewerPanel()
        {
            GC.SuppressFinalize(this);
            Dispose();
        }
    }
}