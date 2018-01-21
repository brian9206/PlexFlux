using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PlexFlux.UI.Component
{
    /// <summary>
    /// Interaction logic for PlaybackControl.xaml
    /// </summary>
    public partial class PlaybackControl : UserControl
    {
        public PlaybackControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var playbackManager = PlaybackManager.GetInstance();
            playbackManager.PlaybackStateChanged += PlaybackManager_PlaybackStateChanged;
            playbackManager.RepeatChanged += PlaybackManager_RepeatChanged;
            playbackManager.ShuffleChanged += PlaybackManager_ShuffleChanged;

            PlaybackManager_RepeatChanged(this, e);
            PlaybackManager_ShuffleChanged(this, e);
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            var playbackManager = PlaybackManager.GetInstance();
            playbackManager.PlaybackStateChanged -= PlaybackManager_PlaybackStateChanged;
            playbackManager.RepeatChanged -= PlaybackManager_RepeatChanged;
            playbackManager.ShuffleChanged -= PlaybackManager_ShuffleChanged;
        }

        private void PlaybackManager_PlaybackStateChanged(object sender, EventArgs e)
        {
            var app = (App)Application.Current;

            Task.Factory.StartNew(() =>
            {
                var playbackManager = PlaybackManager.GetInstance();

                if (playbackManager.PlaybackState == NAudio.Wave.PlaybackState.Playing)
                {
                    buttonPlay.Visibility = Visibility.Collapsed;
                    buttonPause.Visibility = Visibility.Visible;
                }
                else
                {
                    buttonPlay.Visibility = Visibility.Visible;
                    buttonPause.Visibility = Visibility.Collapsed;
                }

            }, CancellationToken.None, TaskCreationOptions.None, app.uiContext);
        }

        private void PlaybackManager_RepeatChanged(object sender, EventArgs e)
        {
            var playbackManager = PlaybackManager.GetInstance();
            pathRepeat.Fill = new SolidColorBrush(playbackManager.IsRepeat ? Color.FromRgb(0xCC, 0x7B, 0x19) : Color.FromRgb(255, 255, 255));
        }

        private void PlaybackManager_ShuffleChanged(object sender, EventArgs e)
        {
            var playbackManager = PlaybackManager.GetInstance();
            pathShuffle.Fill = new SolidColorBrush(playbackManager.IsShuffle ? Color.FromRgb(0xCC, 0x7B, 0x19) : Color.FromRgb(255, 255, 255));
        }

        private void buttonVolumeControl_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = MainWindow.GetInstance();
            mainWindow.IsVolumeControlVisible = !mainWindow.IsVolumeControlVisible;
        }

        private void Repeat_Click(object sender, RoutedEventArgs e)
        {
            var playbackManager = PlaybackManager.GetInstance();
            playbackManager.IsRepeat = !playbackManager.IsRepeat;
        }

        private void Shuffle_Click(object sender, RoutedEventArgs e)
        {
            var playbackManager = PlaybackManager.GetInstance();
            playbackManager.IsShuffle = !playbackManager.IsShuffle;
        }
    }
}
