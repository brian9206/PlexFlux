using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PlexLib;

namespace PlexFlux.UI.Pages
{
    /// <summary>
    /// Interaction logic for BrowseLibrary.xaml
    /// </summary>
    public partial class BrowseLibrary : Page
    {
        public ICollectionView MediaObjects
        {
            get;
            private set;
        }

        public ObservableCollection<IPlexMediaObject> MediaObjectsData
        {
            get;
            private set;
        }

        public PlexLibrary Library
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

        public BrowseLibrary(ContextMenu contextMenu, PlexLibrary library)
        {
            MediaObjectsData = new ObservableCollection<IPlexMediaObject>();
            MediaObjects = CollectionViewSource.GetDefaultView(MediaObjectsData);
            MediaObjects.Filter = MediaObjects_Filter;

            Library = library;

            InitializeComponent();
        }

        private async Task LoadMediaObjects(string category)
        {
            var app = (App)Application.Current;

            MediaObjectsData.Clear();
            IsLoading = true;

            foreach (MenuItem menuItem in ctxmenuCategory.Items)
                menuItem.IsChecked = (string)menuItem.Header == category;

            try
            {
                switch (category)
                {
                    case "Artists":
                        var artists = await app.plexClient.GetArtists(Library);
                        MediaObjectsData.FromArray(artists);

                        break;

                    case "Albums":
                        var albums = await app.plexClient.GetAlbums(Library);
                        MediaObjectsData.FromArray(albums);

                        break;

                    case "Tracks":
                        var tracks = await app.plexClient.GetTracks(Library);
                        MediaObjectsData.FromArray(tracks);

                        break;
                }
            }
            catch
            {
                MessageBox.Show("Could not fetch data from remote server.", "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }

            // remember user preference
            app.config.LibraryDefaultCategory = category;

            IsLoading = false;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            var app = (App)Application.Current;
            await LoadMediaObjects(app.config.LibraryDefaultCategory);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = (FrameworkElement)sender;
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            button.ContextMenu.IsOpen = true;
        }

        private async void MenuItem_Category_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;

            if (menuItem.IsChecked)
                return;

            await LoadMediaObjects((string)menuItem.Header);
        }

        private bool MediaObjects_Filter(object item)
        {
            var mediaObject = item as IPlexMediaObject;
            return mediaObject.Title.ToLower().Contains(textFilter.Text.ToLower());
        }

        private void textFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            MediaObjects.Refresh();
        }

        private void MediaObjectItem_Click(object sender, RoutedEventArgs e)
        {
            var mediaObjectItem = (Component.MediaObjectItem)sender;

            if (mediaObjectItem.MediaObject is PlexArtist)
            {
                var browseArtist = new BrowseArtist(mediaObjectItem.MediaObject as PlexArtist, mediaObjectItem.Menu);

                var mainWindow = MainWindow.GetInstance();
                mainWindow.Frame.Navigate(browseArtist);
            }

            else if (mediaObjectItem.MediaObject is PlexAlbum)
            {
                var browseAlbum = new BrowseAlbum(mediaObjectItem.MediaObject as PlexAlbum, mediaObjectItem.Menu);

                var mainWindow = MainWindow.GetInstance();
                mainWindow.Frame.Navigate(browseAlbum);
            }
        }
    }
}
