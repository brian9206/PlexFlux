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
using System.Windows.Shapes;
using PlexLib;

namespace PlexFlux.UI
{
    /// <summary>
    /// Interaction logic for ServerSelectionWindow.xaml
    /// </summary>
    public partial class ServerSelectionWindow : Window
    {
        public ObservableCollection<PlexServer> OnlineServers
        {
            get;
            private set;
        }

        public ObservableCollection<PlexServer> OfflineServers
        {
            get;
            private set;
        }

        public PlexServer SelectedPlexServer
        {
            get;
            private set;
        }

        public ServerSelectionWindow()
        {
            OnlineServers = new ObservableCollection<PlexServer>();
            OfflineServers = new ObservableCollection<PlexServer>();
            SelectedPlexServer = null;

            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // show progress
            IsEnabled = false;
            panelProgress.Visibility = Visibility.Visible;
            buttonRefresh.Visibility = Visibility.Hidden;

            var app = (App)Application.Current;

            // get server list from plex.tv
            var servers = await app.GetPlexServers();
            
            // get server status
            var tasks = new Task[servers.Length];
            var results = new bool[servers.Length];

            for (int i = 0; i < servers.Length; i++)
            {
                int idx = i;
                tasks[i] = Task.Run(() => results[idx] = servers[idx].SelectAddress());
            }

            // wait until all tasks have been done
            await Task.WhenAll(tasks);

            // clear the collections before adding
            OnlineServers.Clear();
            OfflineServers.Clear();

            for (int i = 0; i < servers.Length; i++)
            {
                if (results[i])
                    OnlineServers.Add(servers[i]);
                else
                    OfflineServers.Add(servers[i]);
            }

            // hide progress
            IsEnabled = true;
            buttonRefresh.Visibility = Visibility.Visible;
            panelProgress.Visibility = Visibility.Hidden;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // prevent window is being closed when fetching server list
            if (!IsEnabled)
                e.Cancel = true;
        }

        private void PlexServerButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedPlexServer = (PlexServer)((Button)sender).Tag;
            DialogResult = true;
            Close();
        }
    }
}
