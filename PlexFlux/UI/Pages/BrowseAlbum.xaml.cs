using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace PlexFlux.UI.Pages
{
    /// <summary>
    /// Interaction logic for BrowseAlbum.xaml
    /// </summary>
    public partial class BrowseAlbum : Page
    {
        public static readonly DependencyProperty AlbumProperty =
            DependencyProperty.Register("Album", typeof(PlexAlbum), typeof(BrowseAlbum));

        public PlexAlbum Album
        {
            get
            {
                return (PlexAlbum)GetValue(AlbumProperty);
            }
            set
            {
                SetValue(AlbumProperty, value);
            }
        }

        public ICollectionView Tracks
        {
            get;
            private set;
        }

        public ObservableCollection<PlexTrack> TracksData
        {
            get;
            private set;
        }

        public ContextMenu Menu
        {
            get;
            private set;
        }

        public bool IsLoading
        {
            get
            {
                return panelLoading.Visibility == Visibility.Visible;
            }
            set
            {
                panelLoading.Visibility = value ? panelLoading.Visibility : Visibility.Hidden;
            }
        }

        private bool isLoaded = false;
        private CancellationTokenSource artworkResizeTokenSource;

        public BrowseAlbum(PlexAlbum album, ContextMenu contextMenu)
        {
            artworkResizeTokenSource = new CancellationTokenSource();
            TracksData = new ObservableCollection<PlexTrack>();

            Album = album;
            Tracks = CollectionViewSource.GetDefaultView(TracksData);
            Tracks.Filter = Tracks_Filter;

            Menu = contextMenu;

            InitializeComponent();
        }

        private void LoadArtwork()
        {
            if (Album == null || Album.Thumb == null)
            {
                imageArtwork.Source = null;
                return;
            }

            var app = (App)Application.Current;

            BitmapImage bitmap = new BitmapImage();
            bitmap.DownloadCompleted += Bitmap_DownloadCompleted;

            bitmap.BeginInit();
            bitmap.UriSource = app.plexClient.GetPhotoTranscodeUrl(Album.Thumb, (int)panelArtwork.ActualWidth, (int)panelArtwork.ActualHeight);
            bitmap.CacheOption = BitmapCacheOption.OnDemand;
            bitmap.EndInit();

            imageArtwork.Source = bitmap;
        }

        private void Bitmap_DownloadCompleted(object sender, EventArgs e)
        {
            imageArtwork.BeginStoryboard((Storyboard)FindResource("FadeIn"));
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            IsLoading = true;

            LoadArtwork();

            if (!isLoaded)
            {
                try
                {
                    var app = (App)Application.Current;
                    var tracks = await app.plexClient.GetTracks(Album);
                    TracksData.FromArray(tracks);
                }
                catch
                {
                    MessageBox.Show("Could not fetch data from remote server.", "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }

            IsLoading = false;
            isLoaded = true;
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (imageArtwork.Source == null)
                return;

            artworkResizeTokenSource.Cancel();
            artworkResizeTokenSource = new CancellationTokenSource();

            Task.Factory.StartNew(() =>
            {
                var cancelToken = artworkResizeTokenSource.Token;

                Thread.Sleep(500);

                if (cancelToken.IsCancellationRequested)
                    return;

                // reload artwork in UI thread
                var app = (App)Application.Current;
                Task.Factory.StartNew(LoadArtwork, CancellationToken.None, TaskCreationOptions.None, app.uiContext);

            }, artworkResizeTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = MainWindow.GetInstance();
            mainWindow.Frame.GoBack();
        }

        private void textFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            Tracks.Refresh();
        }

        private bool Tracks_Filter(object item)
        {
            var track = item as PlexTrack;
            return track.Title.ToLower().Contains(textFilter.Text.ToLower());
        }

        private void TrackButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Component.TrackButton)sender;

            button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            button.ContextMenu.PlacementTarget = (UIElement)sender;
            button.ContextMenu.IsOpen = true;
        }

        private void MenuItem_Play_Click(object sender, RoutedEventArgs e)
        {
            var button = (Component.TrackButton)((ContextMenu)((MenuItem)e.Source).Parent).PlacementTarget;
            var index = ItemsControlHelper.FindIndexByItemChild(panelTracks, button);

            var playQueue = PlayQueueManager.GetInstance();
            var track = playQueue.FromTracks(Tracks, index);
            playQueue.AddPlayedIndex();

            var playback = PlaybackManager.GetInstance();
            playback.Play(track);

            var mainWindow = MainWindow.GetInstance();
            mainWindow.GoToPlayQueue();
        }

        private void MenuItem_AddToPlayQueue_Click(object sender, RoutedEventArgs e)
        {
            var track = (PlexTrack)((MenuItem)e.Source).DataContext;

            var playQueue = PlayQueueManager.GetInstance();
            playQueue.Add(track);

            var mainWindow = MainWindow.GetInstance();
            mainWindow.FlashPlayQueue();
        }

        private void MenuItem_AddToUpcomings_Click(object sender, RoutedEventArgs e)
        {
            var track = (PlexTrack)((MenuItem)e.Source).DataContext;

            var upcomings = UpcomingManager.GetInstance();
            upcomings.Push(track);

            var mainWindow = MainWindow.GetInstance();
            mainWindow.FlashPlayQueue();
        }

        private async void MenuItem_AddToPlaylist_Click(object sender, RoutedEventArgs e)
        {
            var track = (PlexTrack)((MenuItem)e.Source).DataContext;

            var window = new PlaylistSelectionWindow();
            if (window.ShowDialog() != true)
                return;

            var app = (App)Application.Current;
            var playlist = window.SelectedPlaylist;

            try
            {
                await app.plexClient.AddItemToPlaylist(playlist, track);

                var mainWindow = MainWindow.GetInstance();
                mainWindow.FlashPlaylist(playlist);
            }
            catch
            {
                MessageBox.Show("Could not fetch data from remote server.", "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void buttonMore_Click(object sender, RoutedEventArgs e)
        {
            Menu.PlacementTarget = buttonMore;
            Menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            Menu.IsOpen = true;
        }
    }
}
