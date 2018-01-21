using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Threading;
using PlexLib;

namespace PlexFlux.UI.Component
{
    /// <summary>
    /// Interaction logic for TrackButton.xaml
    /// </summary>
    public partial class TrackButton : UserControl
    {
        public static readonly DependencyProperty TrackProperty =
            DependencyProperty.Register("Track", typeof(PlexTrack), typeof(TrackButton));

        public static readonly DependencyProperty DoNotUpdatePlayingProperty =
            DependencyProperty.Register("DoNotUpdatePlaying", typeof(bool), typeof(TrackButton));

        public static readonly DependencyProperty DeleteButtonVisibilityProperty =
            DependencyProperty.Register("DeleteButtonVisibility", typeof(Visibility), typeof(TrackButton), new PropertyMetadata(Visibility.Visible));

        public PlexTrack Track
        {
            get
            {
                return (PlexTrack)GetValue(TrackProperty);
            }
            set
            {
                SetValue(TrackProperty, value);
            }
        }

        public bool DoNotUpdatePlaying
        {
            get
            {
                return (bool)GetValue(DoNotUpdatePlayingProperty);
            }
            set
            {
                SetValue(DoNotUpdatePlayingProperty, value);
            }
        }

        public Visibility DeleteButtonVisibility
        {
            get
            {
                return (Visibility)GetValue(DeleteButtonVisibilityProperty);
            }
            set
            {
                SetValue(DeleteButtonVisibilityProperty, value);
            }
        }

        public bool IsPlaying
        {
            get;
            private set;
        }

        #region "Click event"
        public event RoutedEventHandler Click;
        public event RoutedEventHandler DeleteClick;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, e);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            DeleteClick?.Invoke(this, e);
        }
        #endregion

        public TrackButton()
        {
            InitializeComponent();
        }

        private void button_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var app = (App)Application.Current;

            // load artwork if available
            if (Track != null && Track.Thumb != null)
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = app.plexClient.GetPhotoTranscodeUrl(Track.Thumb, 50, 50);
                bitmap.CacheOption = BitmapCacheOption.OnDemand;
                bitmap.EndInit();

                imageArtwork.Source = bitmap;
            }

            var playbackManager = PlaybackManager.GetInstance();
            playbackManager.PlaybackStateChanged += TrackButton_PlaybackStateChanged;
            TrackButton_PlaybackStateChanged(playbackManager.Track, new EventArgs());
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            PlaybackManager.GetInstance().PlaybackStateChanged -= TrackButton_PlaybackStateChanged;
        }

        private void TrackButton_PlaybackStateChanged(object sender, EventArgs e)
        {
            var app = (App)Application.Current;
            var playbackManager = PlaybackManager.GetInstance();

            // update UI
            Task.Factory.StartNew(() =>
            {
                if (DoNotUpdatePlaying)
                    return;

                if (playbackManager.Track == null || Track == null || playbackManager.Track.MetadataUrl != Track.MetadataUrl || playbackManager.PlaybackState == NAudio.Wave.PlaybackState.Stopped)
                {
                    IsPlaying = false;
                }
                else
                {
                    IsPlaying = true;
                }

                button.Tag = IsPlaying;

            }, CancellationToken.None, TaskCreationOptions.None, app.uiContext);
        }
    }
}
