using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
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
                imageArtwork.Visibility = Visibility.Collapsed;
                imageArtworkNone.Visibility = MediaObject != null && MediaObject.Thumb == null ? Visibility.Visible : Visibility.Collapsed;
                return;
            }

            var app = (App)Application.Current;

            BitmapImage bitmap = new BitmapImage();
            bitmap.DownloadCompleted += Bitmap_DownloadCompleted;

            bitmap.BeginInit();
            bitmap.UriSource = app.plexClient.GetPhotoTranscodeUrl(MediaObject.Thumb, (int)ActualWidth, (int)ActualHeight);
            bitmap.CacheOption = BitmapCacheOption.Default;
            bitmap.EndInit();

            imageArtwork.Source = bitmap;

            imageArtwork.Visibility = Visibility.Visible;
            imageArtworkNone.Visibility = Visibility.Collapsed;
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

        public void OpenContextMenu()
        {
            button.ContextMenu.IsOpen = true;
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

        private void MenuItem_OpenInWebBrowser_Click(object sender, RoutedEventArgs e)
        {
            var app = (App)Application.Current;

            IPlexMediaObject mediaObject = MediaObject;

            // workaround: Plex Web Client does not support displaying track details so we navigate to its album instead of the actual track
            if (mediaObject is PlexTrack track)
                mediaObject = track.Album;

            Process.Start("explorer.exe",
                "\"https://app.plex.tv/desktop#!/server/" + HttpUtility.UrlEncode(app.plexConnection.Server.MachineIdentifier) + "/details?key=" + HttpUtility.UrlEncode(mediaObject.MetadataUrl.Replace("/children", string.Empty)) + "\"");
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
