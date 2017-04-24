using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using QuickLook.Annotations;

namespace QuickLook.Plugin
{
    /// <summary>
    ///     Interaction logic for ViewContentContainer.xaml
    /// </summary>
    public partial class ViewContentContainer : UserControl, INotifyPropertyChanged
    {
        private string _title = string.Empty;

        public ViewContentContainer()
        {
            InitializeComponent();
        }

        public string Title
        {
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
            get => _title;
        }

        public IViewer ViewerPlugin { get; set; }

        public Size PreferedSize { get; set; }

        public bool CanResize { get; set; } = true;

        public event PropertyChangedEventHandler PropertyChanged;

        public void SetContent(object content)
        {
            container.Content = content;
        }

        public double SetPreferedSizeFit(Size size, double maxRatio)
        {
            if (maxRatio > 1)
                maxRatio = 1;

            var max = GetMaximumDisplayBound();

            var widthRatio = max.Width * maxRatio / size.Width;
            var heightRatio = max.Height * maxRatio / size.Height;

            var ratio = Math.Min(widthRatio, heightRatio);

            PreferedSize = new Size {Width = size.Width * ratio, Height = size.Height * ratio};

            return ratio;
        }

        public Size GetMaximumDisplayBound()
        {
            return new Size(SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}