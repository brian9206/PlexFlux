using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PlexLib;

namespace PlexFlux.UI.Component
{
    /// <summary>
    /// Interaction logic for ArtistButton.xaml
    /// </summary>
    public partial class MediaObjectItem : UserControl
    {
        public static readonly DependencyProperty MediaObjectProperty =
            DependencyProperty.Register("MediaObject", typeof(IPlexMediaObject), typeof(MediaObjectItem));

        public IPlexMediaObject MediaObject
        {
            get
            {
                return (IPlexMediaObject)GetValue(MediaObjectProperty);
            }
            set
            {
                SetValue(MediaObjectProperty, value);
            }
        }

        public ContextMenu Menu
        {
            get => button.ContextMenu;
        }

        public event RoutedEventHandler Click;

        public MediaObjectItem()
        {
            InitializeComponent();
        }

        private void LoadArtwork()
        {
            if (MediaObject == null || MediaObject.Thumb == null)
            {
                imageArtwork.Source = null;
                return;
            }

            var app = (App)Application.Current;

            BitmapImage bitmap = new BitmapImage();
            bitmap.DownloadCompleted += Bitmap_DownloadCompleted;

            bitmap.BeginInit();
            bitmap.UriSource = app.plexClient.GetPhotoTranscodeUrl(MediaObject.Thumb, (int)ActualWidth, (int)ActualHeight);
            bitmap.CacheOption = BitmapCacheOption.OnDemand;
            bitmap.EndInit();

            imageArtwork.Source = bitmap;
        }

        private async Task<PlexTrack[]> GetTracksByMediaObject()
        {
            var app = (App)Application.Current;

            PlexTrack[] tracks = null;

            if (MediaObject is PlexAlbum)
            {
                tracks = await app.plexClient.GetTracks(MediaObject as PlexAlbum);
            }
            else if (MediaObject is PlexArtist)
            {
                var trackList = new List<PlexTrack>();
                var albums = await app.plexClient.GetAlbums(MediaObject as PlexArtist);

                foreach (var album in albums)
                    trackList.AddRange(await app.plexClient.GetTracks(album));

                tracks = trackList.ToArray();
            }
            else if (MediaObject is PlexTrack)
            {
                tracks = new PlexTrack[]
                {
                    MediaObject as PlexTrack
                };
            }

            return tracks;
        }

        private void Bitmap_DownloadCompleted(object sender, EventArgs e)
        {
            imageArtwork.BeginStoryboard((Storyboard)FindResource("FadeIn"));
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadArtwork();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, e);
        }

        // menu item handler
        private async void MenuItem_Play_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var playQueue = PlayQueueManager.GetInstance();
                var track = playQueue.FromTracks(await GetTracksByMediaObject());
                playQueue.AddPlayedIndex();

                var playback = PlaybackManager.GetInstance();
                playback.Play(track);

                var mainWindow = MainWindow.GetInstance();
                mainWindow.GoToPlayQueue();
            }
            catch
            {
                MessageBox.Show("Could not fetch data from remote server.", "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void MenuItem_AddToPlayQueue_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var playQueue = PlayQueueManager.GetInstance();
                playQueue.AddRange(await GetTracksByMediaObject());

                var mainWindow = MainWindow.GetInstance();
                mainWindow.FlashPlayQueue();
            }
            catch
            {
                MessageBox.Show("Could not fetch data from remote server.", "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void MenuItem_AddToUpcomings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var upcomings = UpcomingManager.GetInstance();

                foreach (var track in await GetTracksByMediaObject())
                    upcomings.Push(track);

                var mainWindow = MainWindow.GetInstance();
                mainWindow.FlashPlayQueue();
            }
            catch
            {
                MessageBox.Show("Could not fetch data from remote server.", "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void MenuItem_AddToPlaylist_Click(object sender, RoutedEventArgs e)
        {
            var window = new PlaylistSelectionWindow();
            if (window.ShowDialog() != true)
                return;

            var app = (App)Application.Current;
            var playlist = window.SelectedPlaylist;
            
            try
            {
                await app.plexClient.AddItemToPlaylist(playlist, MediaObject);

                var mainWindow = MainWindow.GetInstance();
                mainWindow.FlashPlaylist(playlist);
            }
            catch
            {
                MessageBox.Show("Could not fetch data from remote server.", "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
