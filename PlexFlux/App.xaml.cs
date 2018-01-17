using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Reflection;
using PlexLib;
using System.Net;
using PlexFlux.UI;
using NAudio.Wave;

namespace PlexFlux
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public AppConfig config;
        public PlexDeviceInfo deviceInfo;

        public MyPlexClient myPlexClient;
        public PlexConnection plexConnection;
        public PlexClient plexClient;

        public TaskScheduler uiContext;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            uiContext = TaskScheduler.FromCurrentSynchronizationContext();

            // app config
            try
            {
                config = AppConfig.Load();
            }
            catch (Exception)
            {
                config = new AppConfig();
            }

            // init plex
            deviceInfo = new PlexDeviceInfo("PlexFlux", Assembly.GetExecutingAssembly().GetName().Version.ToString(), config.ClientIdentifier);
            myPlexClient = new MyPlexClient(deviceInfo, config.AuthenticationToken.Length == 0 ? null : config.AuthenticationToken);
            plexConnection = null;
            plexClient = null;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            config.Save();

            Environment.Exit(e.ApplicationExitCode);
        }

        public async Task<PlexServer[]> GetPlexServers()
        {
            PlexServer[] servers = null;

            while (servers == null)
            {
                try
                {
                    servers = await myPlexClient.GetServers();
                }
                catch (WebException)
                {
                    AuthWindow authWindow = new AuthWindow();

                    if (authWindow.ShowDialog() == false)
                        Environment.Exit(0);

                    // update app config
                    config.AuthenticationToken = myPlexClient.AuthenticationToken;
                    config.Save();
                }
                catch (Exception e)
                {
                    MessageBox.Show("An unknown error has occurred.\nPlexFlux will now quit.\n\n" + e.Message, "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(128);
                }
            }

            return servers;
        }

        public PlexServer SelectPlexServer()
        {
            var serverSelectionWindow = new ServerSelectionWindow();
            serverSelectionWindow.ShowDialog();

            var server = serverSelectionWindow.SelectedPlexServer;

            // save to app config
            if (server != null)
            {
                config.ServerMachineIdentifier = server.MachineIdentifier;
                config.Save();
            }

            return server;
        }

        public void ConnectToPlexServer(PlexServer server)
        {
            if (!server.HasSelectedAddress)
                server.SelectAddress();

            plexConnection = new PlexConnection(deviceInfo, server);
            plexClient = new PlexClient(plexConnection);
        }

    }
}
