using System;
using System.Threading.Tasks;
using System.Windows;
using System.Reflection;
using System.Net;
using System.Threading;
using NAudio.CoreAudioApi;
using PlexLib;
using PlexFlux.UI;
using Octokit;

namespace PlexFlux
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private static readonly Mutex appMutex = new Mutex(true, "{14B7E89A-58A5-48EE-99BD-BAB4CF5C20AF}");

        public AppConfig config;
        public PlexDeviceInfo deviceInfo;

        public MyPlexClient myPlexClient;
        public PlexConnection plexConnection;
        public PlexClient plexClient;

        public TaskScheduler uiContext;

        protected override void OnStartup(StartupEventArgs e)
        {
            if (!appMutex.WaitOne(TimeSpan.Zero, true))
            {
                MessageBox.Show("PlexFlux is already running.\nOnly one instance at a time.", "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                Environment.Exit(128);
                return;
            }

            // As we are using WASAPI, we are not compatible with Vista below. (no point to support shit legacy OS)
            if (Environment.OSVersion.Version.Major < 6)    // vista is 6.0
            {
                MessageBox.Show("PlexFlux is not compatible with your current version of Windows.\nWindows Vista or later is required.", "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(128);
                return;
            }

#if DEBUG
            while (!System.Diagnostics.Debugger.IsAttached)
            {
                Thread.Sleep(100);
            }
#endif

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

            // init IPC
            var ipc = IPCManager.GetInstance();
            ipc.Init();
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
                config.LastPlaylist = null;
                config.Save();
            }

            return server;
        }

        public Task ConnectToPlexServer(PlexServer server)
        {
            return Task.Factory.StartNew(() =>
            {
                if (!server.HasSelectedAddress)
                    server.SelectAddress(); // this, should not block the UI

                plexConnection = new PlexConnection(deviceInfo, server);
                plexClient = new PlexClient(plexConnection);

            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public MMDevice GetDeviceByID(string deviceID)
        {
            var enumerator = new MMDeviceEnumerator();

            foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                if (device.ID == deviceID)
                    return device;
            }

            return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
        }

        public async Task<Version> GetLatestVersion()
        {
            var github = new GitHubClient(new ProductHeaderValue("PlexFlux"));

            Version latestVersion = null;
            Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

            try
            {
                var release = await github.Repository.Release.GetLatest("brian9206", "PlexFlux");
                latestVersion = Version.Parse(release.TagName);
            }
            catch
            {
                // internet connection issue or no releases
                return currentVersion;
            }

            return latestVersion;
        }
    }
}
