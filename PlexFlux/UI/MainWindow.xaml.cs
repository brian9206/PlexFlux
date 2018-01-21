using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Net;
using System.Reflection;
using System.Diagnostics;
using PlexLib;
using System.Threading;

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

        public Frame Frame
        {
            get => frame;
        }

        private SystemTrayIcon systemTrayIcon;

        public MainWindow()
        {
            instance = this;

            Reset();
            InitializeComponent();

            CommandManager.RequerySuggested += CommandManager_RequerySuggested;

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

            // system tray
            systemTrayIcon = new SystemTrayIcon();
            systemTrayIcon.DoubleClick += SystemTrayIcon_DoubleClick;

            // -startup will result in default minimized
            var args = Environment.GetCommandLineArgs();
            if (args.Length >= 2 && args[1] == "-startup")
            {
                WindowState = WindowState.Minimized;
                Window_StateChanged(this, new EventArgs());
            }
        }

        private void SystemTrayIcon_DoubleClick(object sender, EventArgs e)
        {
            RestoreFromSystemTray();
        }

        private void CommandManager_RequerySuggested(object sender, EventArgs e)
        {
            // workaround for background thumb button always disabled issue
            thumbButtonPlay.IsEnabled = MediaCommands.Play.CanExecute(null, this);
            thumbButtonPause.IsEnabled = MediaCommands.Pause.CanExecute(null, this);
            thumbButtonPrevious.IsEnabled = MediaCommands.PreviousTrack.CanExecute(null, this);
            thumbButtonNext.IsEnabled = MediaCommands.NextTrack.CanExecute(null, this);

            thumbButtonPause.Visibility = thumbButtonPause.IsEnabled ? Visibility.Visible : Visibility.Collapsed;
            thumbButtonPlay.Visibility = (thumbButtonPause.IsEnabled && !thumbButtonPlay.IsEnabled) ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ThumbButtonInfo_Click(object sender, EventArgs e)
        {
            if (sender == thumbButtonPlay)
                MediaCommands.Play.Execute(null, this);

            else if (sender == thumbButtonPause)
                MediaCommands.Pause.Execute(null, this);

            else if (sender == thumbButtonPrevious)
                MediaCommands.PreviousTrack.Execute(null, this);

            else if (sender == thumbButtonNext)
                MediaCommands.NextTrack.Execute(null, this);
        }

        public void Reset()
        {
            Server = null;
            Playlists = new ObservableCollection<PlexPlaylist>();
            Libraries = new ObservableCollection<PlexLibrary>();
            PlaybackManager.GetInstance().Reset();
        }

        public void RestoreFromSystemTray()
        {
            systemTrayIcon.Visible = false;

            Visibility = Visibility.Visible;
            WindowState = WindowState.Normal;

            SystemCommands.RestoreWindow(this);
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

        public void FlashPlaylist(PlexPlaylist playlist)
        {
            for (int i = 0; i < panelPlaylists.Items.Count; i++)
            {
                var item = (Component.PlaylistSidebarItem)ItemsControlHelper.GetItemChildByIndex(panelPlaylists, i);
                
                if (item.Playlist.MetadataUrl == playlist.MetadataUrl)
                {
                    item.BeginStoryboard((Storyboard)FindResource("NotifyFlash"));
                    break;
                }
            }
        }

        private void SelectSidebarItem(object sender)
        {
            buttonPlayQueue.IsEnabled = sender != buttonPlayQueue;

            for (int i = 0; i < panelPlaylists.Items.Count; i++)
            {
                var item = (Component.PlaylistSidebarItem)ItemsControlHelper.GetItemChildByIndex(panelPlaylists, i);
                item.IsButtonEnabled = sender != item;
            }

            for (int i = 0; i < panelLibraries.Items.Count; i++)
            {
                var item = (Component.LibrarySidebarItem)ItemsControlHelper.GetItemChildByIndex(panelLibraries, i);
                item.IsButtonEnabled = sender != item;
            }
        }

        public async Task Refresh()
        {
            var tasks = new Task[]
            {
                FetchPlaylists(),
                FetchLibraries()
            };

            await Task.WhenAll(tasks);
        }

        public async Task FetchPlaylists()
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

        public async Task FetchLibraries()
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
            var app = (App)Application.Current;

            var playQueue = PlayQueueManager.GetInstance();
            playQueue.TrackChanged += PlayQueue_TrackChanged;

            // load play queue
            buttonPlayQueue_Click(buttonPlayQueue, e);

            IsLoading = true;
            IsVolumeControlVisible = false;

            var servers = await app.GetPlexServers();
            var server = servers.Where(srv => srv.MachineIdentifier == app.config.ServerMachineIdentifier).FirstOrDefault();

            if (server == null)
                server = app.SelectPlexServer();
            else
                app.ConnectToPlexServer(server);

            Server = server;
            await Refresh();

            IsLoading = false;

            // check update
            await Task.Factory.StartNew(async () =>
            {
                await Task.Delay(10 * 1000);

                // check update
                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                var latestVersion = await app.GetLatestVersion();

                if (latestVersion.Major > currentVersion.Major ||
                    latestVersion.Minor > currentVersion.Minor ||
                    latestVersion.Revision > currentVersion.Revision)
                {
                    await Task.Factory.StartNew(() => systemTrayIcon.ShowBalloonTip("New version is available. It is recommended to upgrade."), CancellationToken.None, TaskCreationOptions.None, app.uiContext);
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState != WindowState.Minimized)
                return;

            var app = (App)Application.Current;
            if (!app.config.AllowMinimize)
                return;

            systemTrayIcon.Visible = true;
            Visibility = Visibility.Hidden;
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
            systemTrayIcon.Dispose();

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

        private void MenuItemSettings_Click(object sender, RoutedEventArgs e)
        {
            var window = new SettingsWindow();
            window.ShowDialog();
        }

        private async void MenuItemCheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            var app = (App)Application.Current;
            var latestVersion = await app.GetLatestVersion();
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

            bool update = false;

            if (latestVersion.Major > currentVersion.Major)
            {
                var result = MessageBox.Show(
                    "Latest version: " + latestVersion.ToString() + "\n" +
                    "Current version: " + currentVersion.ToString() + "\n\n" +
                    "New major version of PlexFlux is released.\n" +
                    "Please visit Github project page for more details.\n\n" +
                    "Do you want to visit release page now?",
                    "PlexFlux", MessageBoxButton.YesNo, MessageBoxImage.Question);

                update = result == MessageBoxResult.Yes;
            }

            else if (latestVersion.Minor > currentVersion.Minor)
            {
                var result = MessageBox.Show(
                    "Latest version: " + latestVersion.ToString() + "\n" +
                    "Current version: " + currentVersion.ToString() + "\n\n" +
                    "New minor version of PlexFlux is released.\n" +
                    "It usually introduce new features and it is recommended to upgrade.\n\n" +
                    "Do you want to visit release page now?",
                    "PlexFlux", MessageBoxButton.YesNo, MessageBoxImage.Question);

                update = result == MessageBoxResult.Yes;
            }

            else if (latestVersion.Minor > currentVersion.Minor)
            {
                var result = MessageBox.Show(
                    "Latest version: " + latestVersion.ToString() + "\n" +
                    "Current version: " + currentVersion.ToString() + "\n\n" +
                    "New revision version of PlexFlux is released.\n" +
                    "It usually fixes bugs and it is recommended to upgrade.\n\n" +
                    "Do you want to visit release page now?",
                    "PlexFlux", MessageBoxButton.YesNo, MessageBoxImage.Question);

                update = result == MessageBoxResult.Yes;
            }

            else
            {
                MessageBox.Show("PlexFlux is on the latest version.\nNo update is currently available.", "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (update)
            {
                Process.Start("explorer.exe", "https://github.com/brian9206/PlexFlux/releases");
            }
        }

        private void MenuItemAbout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("PlexFlux v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + "\nCreated by Brian Choi\nGithub: https://github.com/brian9206/PlexFlux", "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuItemQuit_Click(object sender, RoutedEventArgs e)
        {
            Close();
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

        private void CreatePlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new NewPlaylistWindow();
            window.ShowDialog();
        }

        private async void RefreshPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            IsLoading = true;

            GoToPlayQueue();
            await FetchPlaylists();

            IsLoading = false;
        }

        private async void RefreshLibraryButton_Click(object sender, RoutedEventArgs e)
        {
            IsLoading = true;

            GoToPlayQueue();
            await FetchLibraries();

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

        private void LibrarySidebarItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (Component.LibrarySidebarItem)sender;

            var page = new Pages.BrowseLibrary(((FrameworkElement)sender).ContextMenu, item.Library);
            frame.Navigate(page);

            SelectSidebarItem(sender);
        }

        private async void LibrarySidebarItem_Scan_Click(object sender, RoutedEventArgs e)
        {
            var item = ((FrameworkElement)e.Source).DataContext as PlexLibrary;

            try
            {
                IsLoading = true;

                var app = (App)Application.Current;
                await app.plexClient.ScanLibrary(item);

                bool isComplete = false;

                while (!isComplete)
                {
                    isComplete = true;

                    var libraries = await app.plexClient.GetLibraries();

                    foreach (var library in libraries)
                    {
                        if (library.Key == item.Key && library.IsRefreshing)
                            isComplete = false;
                    }

                    await Task.Delay(1000);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show("Could not get data from remote server.\n" + exception.Message, "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                GoToPlayQueue();
                IsLoading = false;
            }
        }

        // commands
        private void MediaCommands_Play_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var playback = PlaybackManager.GetInstance();

            if (playback.PlaybackState == NAudio.Wave.PlaybackState.Playing)
            {
                e.CanExecute = false;
                return;
            }
               
            if (playback.PlaybackState == NAudio.Wave.PlaybackState.Paused)
            {
                e.CanExecute = true;
                return;
            }

            var page = frame.Content;
            var playQueue = PlayQueueManager.GetInstance();

            if (page is Pages.BrowsePlaylist)
            {
                var browsePlaylist = (Pages.BrowsePlaylist)page;
                e.CanExecute = playback.PlaybackState != NAudio.Wave.PlaybackState.Playing && !browsePlaylist.IsLoading && browsePlaylist.Tracks.Count > 0;
            }
            else if (page is Pages.BrowseAlbum)
            {
                var browseAlbum = (Pages.BrowseAlbum)page;
                e.CanExecute = playback.PlaybackState != NAudio.Wave.PlaybackState.Playing && !browseAlbum.IsLoading && browseAlbum.TracksData.Count > 0;
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
                CommandManager.InvalidateRequerySuggested();
            }
            else if (playbackManager.PlaybackState == NAudio.Wave.PlaybackState.Stopped)
            {
                var page = frame.Content;
                var playQueue = PlayQueueManager.GetInstance();

                if (page is Pages.BrowsePlaylist)
                {
                    var browsePlaylist = (Pages.BrowsePlaylist)page;
                    track = playQueue.FromTracks(browsePlaylist.Tracks.ToArray());

                    GoToPlayQueue();
                }
                else if (page is Pages.BrowseAlbum)
                {
                    var browseAlbum = (Pages.BrowseAlbum)page;
                    track = playQueue.FromTracks(browseAlbum.TracksData.ToArray());

                    GoToPlayQueue();
                }
                else
                {
                    track = playQueue.CurrentTrack;
                }
            }

            if (track == null)
                return;

            playbackManager.Play(track);

            CommandManager.InvalidateRequerySuggested();
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

            CommandManager.InvalidateRequerySuggested();
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

            CommandManager.InvalidateRequerySuggested();
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

            CommandManager.InvalidateRequerySuggested();
        }
    }
}
