using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using PlexLib;
using System.Windows.Media.Animation;

namespace PlexFlux.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region "Singleton"
        private static MainWindow instance;

        public static MainWindow GetInstance()
        {
            return instance;
        }
        #endregion

        private PlexServer server;

        public PlexServer Server
        {
            get { return server; }
            private set
            {
                server = value;
                OnPropertyChanged("Server");
                OnPropertyChanged("ServerName");
                OnPropertyChanged("ServerAddress");
            }
        }

        public string ServerName
        {
            get
            {
                if (Server == null)
                    return "PlexFlux";
                else
                    return Server.Name;
            }
        }

        public string ServerAddress
        {
            get
            {
                if (Server == null)
                    return "Not connected";
                else
                    return Server.Url;
            }
        }

        public bool IsLoading
        {
            get { return panelLoading.Visibility == Visibility.Collapsed; }
            set { panelLoading.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
        }

        public ObservableCollection<PlexPlaylist> Playlists
        {
            get;
            private set;
        }

        public ObservableCollection<PlexLibrary> Libraries
        {
            get;
            private set;
        }

        public bool IsVolumeControlVisible
        {
            get
            {
                return panelVolumeControl.Visibility == Visibility.Visible;
            }
            set
            {
                panelVolumeControl.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public MainWindow()
        {
            instance = this;

            Reset();
            InitializeComponent();

            var app = (App)Application.Current;

            // restore window pos and size
            if (app.config.WindowPosX != int.MinValue && app.config.WindowPosX != app.config.WindowPosY)
            {
                Left = app.config.WindowPosX;
                Top = app.config.WindowPosY;
                WindowStartupLocation = WindowStartupLocation.Manual;
            }

            if (app.config.WindowSizeW != int.MinValue && app.config.WindowSizeW != app.config.WindowSizeH)
            {
                Width = app.config.WindowSizeW;
                Height = app.config.WindowSizeH;
            }
        }

        public void Reset()
        {
            Server = null;
            Playlists = new ObservableCollection<PlexPlaylist>();
            Libraries = new ObservableCollection<PlexLibrary>();
            PlaybackManager.GetInstance().Reset();
        }

        public void GoToPlayQueue()
        {
            SelectSidebarItem(buttonPlayQueue);

            var playQueue = new Pages.PlayQueue(buttonPlayQueue.ContextMenu);
            frame.Navigate(playQueue);
        }

        public void FlashPlayQueue()
        {
            buttonPlayQueue.BeginStoryboard((Storyboard)FindResource("NotifyFlash"));
        }

        private void SelectSidebarItem(object sender)
        {
            buttonPlayQueue.IsEnabled = sender != buttonPlayQueue;

            for (int i = 0; i < panelPlaylists.Items.Count; i++)
            {
                var dependencyObject = panelPlaylists.ItemContainerGenerator.ContainerFromIndex(i);
                var item = (Component.PlaylistSidebarItem)VisualTreeHelper.GetChild(dependencyObject, 0);

                item.IsButtonEnabled = sender != item;
            }

            for (int i = 0; i < panelLibraries.Items.Count; i++)
            {
                var dependencyObject = panelLibraries.ItemContainerGenerator.ContainerFromIndex(i);
                var item = (Component.LibrarySidebarItem)VisualTreeHelper.GetChild(dependencyObject, 0);

                item.IsButtonEnabled = sender != item;
            }
        }

        private async Task Refresh()
        {
            var tasks = new Task[]
            {
                FetchPlaylists(),
                FetchLibraries()
            };

            await Task.WhenAll(tasks);
        }

        private async Task FetchPlaylists()
        {
            // do nothing if we are not connected
            if (Server == null)
                return;

            var app = (App)Application.Current;

            try
            {
                var playlists = await app.plexClient.GetPlaylists();
                Playlists.FromArray(playlists);
            }
            catch (Exception)
            {
                // suppress error
            }
        }

        private async Task FetchLibraries()
        {
            // do nothing if we are not connected
            if (Server == null)
                return;

            var app = (App)Application.Current;

            try
            {
                var libraries = await app.plexClient.GetLibraries();
                Libraries.FromEnumerable(libraries.Where(library => library.Type == "artist"));
            }
            catch (Exception)
            {
                // suppress error
            }
        }

        #region "INotifyPropertyChanged implementation"
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(String info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
        #endregion

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var playQueue = PlayQueueManager.GetInstance();
            playQueue.TrackChanged += PlayQueue_TrackChanged;

            // load play queue
            buttonPlayQueue_Click(buttonPlayQueue, e);

            IsLoading = true;
            IsVolumeControlVisible = false;

            var app = (App)Application.Current;
            var servers = await app.GetPlexServers();
            var server = servers.Where(srv => srv.MachineIdentifier == app.config.ServerMachineIdentifier).FirstOrDefault();

            if (server == null)
                server = app.SelectPlexServer();
            else
                app.ConnectToPlexServer(server);

            Server = server;
            await Refresh();

            IsLoading = false;
        }

        private void PlayQueue_TrackChanged(object sender, EventArgs e)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            IsVolumeControlVisible = false;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.IsRepeat)
                return;

            switch (e.Key)
            {
                case Key.Space:
                    if (MediaCommands.Play.CanExecute(null, this))
                        MediaCommands.Play.Execute(null, this);
                    else if (MediaCommands.Pause.CanExecute(null, this))
                        MediaCommands.Pause.Execute(null, this);

                    e.Handled = true;
                    break;

            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            var app = (App)Application.Current;
            app.config.WindowSizeW = (int)Width;
            app.config.WindowSizeH = (int)Height;

            app.config.WindowPosX = (int)Left;
            app.config.WindowPosY = (int)Top;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            var app = (App)Application.Current;
            app.Shutdown();
        }

        private void buttonApp_Click(object sender, RoutedEventArgs e)
        {
            buttonApp.ContextMenu.PlacementTarget = buttonApp;
            buttonApp.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            buttonApp.ContextMenu.IsOpen = true;
        }

        private void MenuItemSwitchUser_Click(object sender, RoutedEventArgs e)
        {
            var app = (App)Application.Current;
            app.config.AuthenticationToken = string.Empty;
            app.config.Save();

            // dc
            Reset();

            // create new MyPlexClient
            app.myPlexClient = new MyPlexClient(app.deviceInfo);

            // reload
            Window_Loaded(sender, e);
        }

        private async void MenuItemSwitchServer_Click(object sender, RoutedEventArgs e)
        {
            var app = (App)Application.Current;
            var server = app.SelectPlexServer();

            if (server != null)
            {
                app.ConnectToPlexServer(server);

                Server = server;
                await Refresh();
            }
        }

        private void buttonPlayQueue_Click(object sender, RoutedEventArgs e)
        {
            GoToPlayQueue();
        }

        private void buttonPlayQueue_RemoveAll_Click(object sender, RoutedEventArgs e)
        {
            var playQueue = PlayQueueManager.GetInstance();
            playQueue.RemoveAll();

            FlashPlayQueue();
        }

        private async void RefreshPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            IsLoading = true;

            GoToPlayQueue();
            await FetchPlaylists();

            IsLoading = false;
        }

        private void PlaylistSidebarItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (Component.PlaylistSidebarItem)sender;

            var page = new Pages.BrowsePlaylist(((FrameworkElement)sender).ContextMenu, item.Playlist);
            frame.Navigate(page);

            SelectSidebarItem(sender);
        }

        private async void PlaylistSidebarItem_PlayAll_Click(object sender, RoutedEventArgs e)
        {
            var app = (App)Application.Current;

            PlexPlaylist playlist = null;
            var dataContext = ((FrameworkElement)e.Source).DataContext;

            if (dataContext is PlexPlaylist)
            {
                playlist = (PlexPlaylist)dataContext;
            }
            else if (dataContext is Pages.BrowsePlaylist)
            {
                playlist = ((Pages.BrowsePlaylist)dataContext).Playlist;
            }

            if (playlist == null)
                return;

            try
            {
                var tracks = await app.plexClient.GetTracks(playlist);

                var playQueue = PlayQueueManager.GetInstance();
                var track = playQueue.FromTracks(tracks);

                var playback = PlaybackManager.GetInstance();
                playback.Play(track);

                GoToPlayQueue();
            }
            catch (WebException exception)
            {
                MessageBox.Show("Could not get data from remote server.\n" + exception.Message, "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void PlaylistSidebarItem_AddToPlayQueue_Click(object sender, RoutedEventArgs e)
        {
            var app = (App)Application.Current;

            PlexPlaylist playlist = null;
            var dataContext = ((FrameworkElement)e.Source).DataContext;

            if (dataContext is PlexPlaylist)
            {
                playlist = (PlexPlaylist)dataContext;
            }
            else if (dataContext is Pages.BrowsePlaylist)
            {
                playlist = ((Pages.BrowsePlaylist)dataContext).Playlist;
            }

            if (playlist == null)
                return;

            try
            {
                var tracks = await app.plexClient.GetTracks(playlist);

                var playQueue = PlayQueueManager.GetInstance();
                playQueue.AddRange(tracks);

                FlashPlayQueue();
            }
            catch (WebException exception)
            {
                MessageBox.Show("Could not get data from remote server.\n" + exception.Message, "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void PlaylistSidebarItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            var app = (App)Application.Current;

            PlexPlaylist playlist = null;
            var dataContext = ((FrameworkElement)e.Source).DataContext;

            if (dataContext is PlexPlaylist)
            {
                playlist = (PlexPlaylist)dataContext;
            }
            else if (dataContext is Pages.BrowsePlaylist)
            {
                playlist = ((Pages.BrowsePlaylist)dataContext).Playlist;
            }

            if (playlist == null)
                return;

            if (MessageBox.Show("Do you really want to delete this playlist?", "PlexFlux", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                await app.plexClient.DeletePlaylist(playlist);
            }
            catch (WebException exception)
            {
                MessageBox.Show("Could not get data from remote server.\n" + exception.Message, "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                await FetchPlaylists();
            }
        }

        // commands
        private void MediaCommands_Play_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var playback = PlaybackManager.GetInstance();

            if (playback.PlaybackState == NAudio.Wave.PlaybackState.Playing)
                e.CanExecute = false;

            var page = frame.Content;
            var playQueue = PlayQueueManager.GetInstance();

            if (page.GetType() == typeof(Pages.BrowsePlaylist))
            {
                var browsePlaylist = (Pages.BrowsePlaylist)page;
                e.CanExecute = playback.PlaybackState != NAudio.Wave.PlaybackState.Playing && !browsePlaylist.IsLoading && browsePlaylist.Tracks.Count > 0;
            }
            else
            {
                e.CanExecute = playback.PlaybackState != NAudio.Wave.PlaybackState.Playing && playQueue.HasTrack;
            }
        }

        private void MediaCommands_Play_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var playbackManager = PlaybackManager.GetInstance();
            PlexTrack track = null;

            if (playbackManager.PlaybackState == NAudio.Wave.PlaybackState.Paused)
            {
                playbackManager.Resume();
            }
            else if (playbackManager.PlaybackState == NAudio.Wave.PlaybackState.Stopped)
            {
                var page = frame.Content;
                var playQueue = PlayQueueManager.GetInstance();

                if (page.GetType() == typeof(Pages.BrowsePlaylist))
                {
                    var browsePlaylist = (Pages.BrowsePlaylist)page;
                    track = playQueue.FromTracks(browsePlaylist.Tracks.ToArray());

                    var mainWindow = MainWindow.GetInstance();
                    mainWindow.GoToPlayQueue();
                }
                else
                {
                    track = playQueue.CurrentTrack;
                }
            }

            if (track == null)
                return;

            playbackManager.Play(track);
        }

        private void MediaCommands_Pause_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var playback = PlaybackManager.GetInstance();
            e.CanExecute = playback.PlaybackState == NAudio.Wave.PlaybackState.Playing;
        }

        private void MediaCommands_Pause_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var playback = PlaybackManager.GetInstance();
            playback.Pause();
        }

        private void MediaCommands_NextTrack_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var playQueue = PlayQueueManager.GetInstance();
            e.CanExecute = playQueue.HasNext;
        }

        private void MediaCommands_NextTrack_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var playQueue = PlayQueueManager.GetInstance();
            var upcomings = UpcomingManager.GetInstance();

            var track = upcomings.Pop();

            if (track == null)
                track = playQueue.Play(playQueue.Current + 1);

            var playback = PlaybackManager.GetInstance();
            playback.Play(track);
        }

        private void MediaCommands_PreviousTrack_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var playQueue = PlayQueueManager.GetInstance();
            e.CanExecute = playQueue.HasPrevious;
        }

        private void MediaCommands_PreviousTrack_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var playQueue = PlayQueueManager.GetInstance();
            var track = playQueue.Play(playQueue.Current - 1);

            var playback = PlaybackManager.GetInstance();
            playback.Play(track);
        }


    }
}
