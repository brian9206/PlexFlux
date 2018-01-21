using System;
using System.Windows;
using PlexLib;

namespace PlexFlux.UI
{
    /// <summary>
    /// Interaction logic for NewPlaylistWindow.xaml
    /// </summary>
    public partial class NewPlaylistWindow : Window
    {
        public PlexPlaylist CreatedPlaylist
        {
            get;
            private set;
        }

        public NewPlaylistWindow()
        {
            InitializeComponent();
            CreatedPlaylist = null;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (textName.Text.Length == 0)
            {
                MessageBox.Show("You must enter a new playlist name.", "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            IsEnabled = false;

            try
            {
                var app = (App)Application.Current;
                CreatedPlaylist = await app.plexClient.CreatePlaylist(textName.Text);

                var mainWindow = MainWindow.GetInstance();
                await mainWindow.FetchPlaylists();

                IsEnabled = true;
                DialogResult = true;
                Close();
            }
            catch
            {
                MessageBox.Show("Could not fetch data from remote server.", "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsEnabled = true;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            textName.Focus();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!IsEnabled)
                e.Cancel = true;
        }
    }
}
