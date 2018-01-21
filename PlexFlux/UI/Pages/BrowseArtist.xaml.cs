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
    /// Interaction logic for BrowseArtist.xaml
    /// </summary>
    public partial class BrowseArtist : Page
    {
        public static readonly DependencyProperty ArtistProperty =
            DependencyProperty.Register("Artist", typeof(PlexArtist), typeof(BrowseArtist));

        public PlexArtist Artist
        {
            get
            {
                return (PlexArtist)GetValue(ArtistProperty);
            }
            set
            {
                SetValue(ArtistProperty, value);
            }
        }

        public ICollectionView Albums
        {
            get;
            private set;
        }

        public ObservableCollection<PlexAlbum> AlbumsData
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

        public BrowseArtist(PlexArtist artist, ContextMenu contextMenu)
        {
            artworkResizeTokenSource = new CancellationTokenSource();
            AlbumsData = new ObservableCollection<PlexAlbum>();

            Artist = artist;
            Albums = CollectionViewSource.GetDefaultView(AlbumsData);
            Albums.Filter = Albums_Filter;

            Menu = contextMenu;

            InitializeComponent();
        }

        private void LoadArtwork()
        {
            if (Artist == null || Artist.Thumb == null)
            {
                imageArtwork.Source = null;
                return;
            }

            var app = (App)Application.Current;

            BitmapImage bitmap = new BitmapImage();
            bitmap.DownloadCompleted += Bitmap_DownloadCompleted;

            bitmap.BeginInit();
            bitmap.UriSource = app.plexClient.GetPhotoTranscodeUrl(Artist.Thumb, (int)panelArtwork.ActualWidth, (int)panelArtwork.ActualHeight);
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
                    var albums = await app.plexClient.GetAlbums(Artist);
                    AlbumsData.FromArray(albums);
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
            Albums.Refresh();
        }

        private bool Albums_Filter(object item)
        {
            var album = item as PlexAlbum;
            return album.Title.ToLower().Contains(textFilter.Text.ToLower());
        }

        private void MediaObjectItem_Click(object sender, RoutedEventArgs e)
        {
            var mediaObjectItem = (Component.MediaObjectItem)sender;
            var browseAlbum = new BrowseAlbum(mediaObjectItem.MediaObject as PlexAlbum, mediaObjectItem.Menu);

            var mainWindow = MainWindow.GetInstance();
            mainWindow.Frame.Navigate(browseAlbum);
        }

        private void buttonMore_Click(object sender, RoutedEventArgs e)
        {
            Menu.PlacementTarget = buttonMore;
            Menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            Menu.IsOpen = true;
        }
    }
}
