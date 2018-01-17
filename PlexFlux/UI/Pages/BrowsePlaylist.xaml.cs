using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.IO;
using PlexLib;
using System.Net;

namespace PlexFlux.UI.Pages
{
    /// <summary>
    /// Interaction logic for BrowsePlaylist.xaml
    /// </summary>
    public partial class BrowsePlaylist : Page
    {
        public ObservableCollection<PlexTrack> Tracks
        {
            get;
            private set;
        }

        public PlexPlaylist Playlist
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

        private ContextMenu contextMenu;

        public BrowsePlaylist(ContextMenu contextMenu, PlexPlaylist playlist)
        {
            this.contextMenu = contextMenu;
            Playlist = playlist;
            Tracks = new ObservableCollection<PlexTrack>();

            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var app = (App)Application.Current;
            var mainWindow = MainWindow.GetInstance();

            IsLoading = true;

            try
            {
                var tracks = await app.plexClient.GetTracks(Playlist);
                Tracks.FromArray(tracks);
            }
            catch (Exception exception)
            {
                MessageBox.Show("Could not fetch data from remote server.\n" + exception.Message, "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            IsLoading = false;

            // TODO: display no track message if tracks.Count = 0
        }

        private void buttonMore_Click(object sender, RoutedEventArgs e)
        {
            contextMenu.PlacementTarget = buttonMore;
            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        private void TrackButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Component.TrackButton)sender;

            var index = -1;

            for (int i = 0; i < panelTracks.Items.Count; i++)
            {
                var dependencyObject = panelTracks.ItemContainerGenerator.ContainerFromIndex(i);
                var item = (Component.TrackButton)VisualTreeHelper.GetChild(dependencyObject, 0);

                if (sender == item)
                {
                    index = i;
                    break;
                }
            }

            var playQueue = PlayQueueManager.GetInstance();
            var track = playQueue.FromTracks(Tracks.ToArray(), index);
            playQueue.AddPlayedIndex();

            var playback = PlaybackManager.GetInstance();
            playback.Play(track);

            var mainWindow = MainWindow.GetInstance();
            mainWindow.GoToPlayQueue();
        }

        private async void TrackButton_DeleteClick(object sender, RoutedEventArgs e)
        {
            var button = (Component.TrackButton)sender;

            var index = -1;

            for (int i = 0; i < panelTracks.Items.Count; i++)
            {
                var dependencyObject = panelTracks.ItemContainerGenerator.ContainerFromIndex(i);
                var item = (Component.TrackButton)VisualTreeHelper.GetChild(dependencyObject, 0);

                if (sender == item)
                {
                    index = i;
                    break;
                }
            }

            var track = Tracks[index];

            if (MessageBox.Show("Do you really want to remove it from your playlist?", "PlexFlux", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var app = (App)Application.Current;

                try
                {
                    Playlist = await app.plexClient.DeleteTrackFromPlaylist(track, Playlist);
                }
                catch (WebException exception)
                {
                    MessageBox.Show("Could not send data to remote server.\n" + exception.Message, "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                finally
                {
                    Page_Loaded(sender, e);
                }
            }
        }

        private void DeleteFromPlaylist_Click(object sender, RoutedEventArgs e)
        {
            var button = (Component.TrackButton)((ContextMenu)((MenuItem)e.Source).Parent).PlacementTarget;
            TrackButton_DeleteClick(button, e);
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            var button = (Component.TrackButton)((ContextMenu)((MenuItem)e.Source).Parent).PlacementTarget;
            TrackButton_Click(button, e);
        }

        private void AddToPlayQueue_Click(object sender, RoutedEventArgs e)
        {
            var track = (PlexTrack)((MenuItem)e.Source).DataContext;

            var playQueue = PlayQueueManager.GetInstance();
            playQueue.Add(track);

            var mainWindow = MainWindow.GetInstance();
            mainWindow.FlashPlayQueue();
        }

        private void AddToUpcomings_Click(object sender, RoutedEventArgs e)
        {
            var track = (PlexTrack)((MenuItem)e.Source).DataContext;

            var upcomings = UpcomingManager.GetInstance();
            upcomings.Push(track);
        }
    }
}
