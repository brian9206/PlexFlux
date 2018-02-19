using System;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Web;
using GongSolutions.Wpf.DragDrop;
using PlexLib;

namespace PlexFlux.UI.Pages
{
    /// <summary>
    /// Interaction logic for BrowsePlaylist.xaml
    /// </summary>
    public partial class BrowsePlaylist : Page, IDropTarget
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

        #region "IDropTarget implementation"
        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            var sourceItem = dropInfo.Data as PlexTrack;
            var targetItem = dropInfo.TargetItem as PlexTrack;

            if (sourceItem != null && targetItem != null && dropInfo.DragInfo.VisualSource == dropInfo.VisualTarget)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = DragDropEffects.Move;
            }
        }

        async void IDropTarget.Drop(IDropInfo dropInfo)
        {
            var source = (Component.TrackButton)VisualTreeHelper.GetChild(dropInfo.DragInfo.VisualSourceItem, 0);
            int sourceIdx = ItemsControlHelper.FindIndexByItemChild(panelTracks, source);
            int targetIdx = dropInfo.InsertIndex - 1;

            // nothing need to work
            if (sourceIdx == targetIdx)
                return;

            if (sourceIdx > targetIdx)
                targetIdx--;

            var sourceItem = dropInfo.Data as PlexTrack;
            var targetItem = targetIdx == -1 ? null : ((Component.TrackButton)ItemsControlHelper.GetItemChildByIndex(panelTracks, targetIdx)).Track;

            // move it
            panelTracks.IsEnabled = false;

            var app = (App)Application.Current;

            try
            {
                await app.plexClient.MoveTrackInPlaylist(Playlist, sourceItem, targetItem);

                // update UI
                Tracks.Remove(sourceItem);
                Tracks.Insert(targetIdx == -1 ? 0 : targetIdx, sourceItem);
            }
            catch
            {
                MessageBox.Show("Failed to update data in the remote server.", "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            finally
            {
                panelTracks.IsEnabled = true;
            }
        }
        #endregion

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

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
        }

        private void buttonMore_Click(object sender, RoutedEventArgs e)
        {
            contextMenu.PlacementTarget = buttonMore;
            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        private void TrackButton_Click(object sender, RoutedEventArgs e)
        {
            var index = ItemsControlHelper.FindIndexByItemChild(panelTracks, sender as DependencyObject);
            if (index == -1)
                return;

            var playQueue = PlayQueueManager.GetInstance();
            var track = playQueue.FromTracks(Tracks.ToArray(), index);
            playQueue.AddPlayedIndex();

            var playback = PlaybackManager.GetInstance();
            playback.Play(track);

            var app = (App)Application.Current;
            app.config.LastPlaylist = Playlist.MetadataUrl;

            var mainWindow = MainWindow.GetInstance();
            mainWindow.GoToPlayQueue();
        }

        private async void TrackButton_DeleteClick(object sender, RoutedEventArgs e)
        {
            var index = ItemsControlHelper.FindIndexByItemChild(panelTracks, sender as DependencyObject);
            if (index == -1)
                return;

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

        private void OpenInWebBrowser_Click(object sender, RoutedEventArgs e)
        {
            var track = (PlexTrack)((MenuItem)e.Source).DataContext;
            var app = (App)Application.Current;

            Process.Start("explorer.exe",
                "\"https://app.plex.tv/desktop#!/server/" + HttpUtility.UrlEncode(app.plexConnection.Server.MachineIdentifier) + "/details?key=" + HttpUtility.UrlEncode(track.Album.MetadataUrl.Replace("/children", string.Empty)) + "\"");
        }
    }
}
