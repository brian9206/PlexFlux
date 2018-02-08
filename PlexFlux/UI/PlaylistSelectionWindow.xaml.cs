using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using PlexLib;

namespace PlexFlux.UI
{
    /// <summary>
    /// Interaction logic for ServerSelectionWindow.xaml
    /// </summary>
    public partial class PlaylistSelectionWindow : Window
    {
        public ObservableCollection<PlexPlaylist> Playlists
        {
            get;
            private set;
        }

        public PlexPlaylist SelectedPlaylist
        {
            get;
            private set;
        }

        public PlaylistSelectionWindow()
        {
            Playlists = new ObservableCollection<PlexPlaylist>();
            SelectedPlaylist = null;

            InitializeComponent();
            Initialize();
        }

        private async void Initialize()
        {
            // show progress
            IsEnabled = false;
            panelProgress.Visibility = Visibility.Visible;
            buttonRefresh.Visibility = Visibility.Hidden;

            var app = (App)Application.Current;

            PlexPlaylist[] playlists = null;

            try
            {
                playlists = await app.plexClient.GetPlaylists();
            }
            catch
            {
                MessageBox.Show("Failed to fetch data from remote server.", "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();

                return;
            }

            Playlists.FromArray(playlists);

            // hide progress
            IsEnabled = true;
            buttonRefresh.Visibility = Visibility.Visible;
            panelProgress.Visibility = Visibility.Hidden;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            // prevent window is being closed when fetching server list
            if (!IsEnabled)
                e.Cancel = true;
        }

        private void PlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedPlaylist = (PlexPlaylist)((Button)sender).Tag;
            DialogResult = true;
            Close();
        }

        private void NewPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new NewPlaylistWindow();
            if (window.ShowDialog() != true)
                return;

            SelectedPlaylist = window.CreatedPlaylist;
            DialogResult = true;
            Close();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            Initialize();
        }
    }
}
