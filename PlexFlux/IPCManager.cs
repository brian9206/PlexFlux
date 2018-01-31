using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using PlexFlux.IPC;
using PlexFlux.UI;

namespace PlexFlux
{
    class IPCManager
    {
        #region "Singleton"
        private static IPCManager instance = null;

        public static IPCManager GetInstance()
        {
            if (instance == null)
                instance = new IPCManager();

            return instance;
        }
        #endregion

        private IPCManager()
        {
        }

        public void Init()
        {
            var server = IPCServer.GetInstance();
            server.Connected += IPCServer_Connected;
            server.MessageReceived += IPCServer_MessageReceived;
            server.Init();

            var playback = PlaybackManager.GetInstance();
            playback.PlaybackStateChanged += PlaybackManager_PlaybackStateChanged;
            playback.PlaybackTick += Playback_PlaybackTick;
            playback.VolumeChanged += PlaybackManager_VolumeChanged;
        }

        private void IPCServer_Connected(object sender, EventArgs e)
        {
            PlaybackManager_PlaybackStateChanged(null, null);
            PlaybackManager_VolumeChanged(null, null);
        }

        private void IPCServer_MessageReceived(object sender, XmlNode messageNode)
        {
            var mainWindow = MainWindow.GetInstance();
            var app = (App)Application.Current;

            switch (messageNode.Attributes["action"].InnerText)
            {
                case "update":
                    IPCServer_Connected(sender, null);
                    break;

                case "play":
                    Task.Factory.StartNew(() =>
                    {
                        if (MediaCommands.Play.CanExecute(null, mainWindow))
                            MediaCommands.Play.Execute(null, mainWindow);

                    }, CancellationToken.None, TaskCreationOptions.None, app.uiContext);

                    break;

                case "pause":
                    Task.Factory.StartNew(() =>
                    {
                        if (MediaCommands.Pause.CanExecute(null, mainWindow))
                            MediaCommands.Pause.Execute(null, mainWindow);

                    }, CancellationToken.None, TaskCreationOptions.None, app.uiContext);

                    break;

                case "previous":
                    Task.Factory.StartNew(() =>
                    {
                        if (MediaCommands.PreviousTrack.CanExecute(null, mainWindow))
                            MediaCommands.PreviousTrack.Execute(null, mainWindow);

                    }, CancellationToken.None, TaskCreationOptions.None, app.uiContext);

                    break;

                case "next":
                    Task.Factory.StartNew(() =>
                    {
                        if (MediaCommands.NextTrack.CanExecute(null, mainWindow))
                            MediaCommands.NextTrack.Execute(null, mainWindow);

                    }, CancellationToken.None, TaskCreationOptions.None, app.uiContext);

                    break;

                case "setVolume":
                    Task.Factory.StartNew(() =>
                    {
                        float vol = int.Parse(messageNode.SelectSingleNode("volume").InnerText) / 100.0f;

                        var playback = PlaybackManager.GetInstance();
                        playback.Volume = vol;

                    }, CancellationToken.None, TaskCreationOptions.None, app.uiContext);
                    
                    break;

                case "setPosition":
                    Task.Factory.StartNew(() =>
                    {
                        long position = long.Parse(messageNode.SelectSingleNode("position").InnerText);

                        var playback = PlaybackManager.GetInstance();
                        playback.Position = position;

                    }, CancellationToken.None, TaskCreationOptions.None, app.uiContext);

                    break;

                case "restoreWindow":
                    Task.Factory.StartNew(() =>
                    {
                        mainWindow.RestoreFromSystemTray();
                        mainWindow.Activate();

                    }, CancellationToken.None, TaskCreationOptions.None, app.uiContext);

                    break;
            }
        }

        private void PlaybackManager_PlaybackStateChanged(object sender, EventArgs e)
        {
            var app = (App)Application.Current;
            var playback = PlaybackManager.GetInstance();

            // construct message
            var factory = new IPCMessageFactory();
            var message = factory.Create("playbackStateChanged", out XmlNode messageNode);

            // <hasTrack />
            var hasTrackNode = message.CreateElement("hasTrack");
            hasTrackNode.InnerText = playback.Track == null ? "false" : "true";
            messageNode.AppendChild(hasTrackNode);

            if (playback.Track != null)
            {
                // <title />
                var titleNode = message.CreateElement("title");
                titleNode.InnerText = playback.Track.Title + " - " + playback.Track.Artist.Title;
                messageNode.AppendChild(titleNode);

                // <artwork />
                var artworkNode = message.CreateElement("artwork");
                artworkNode.InnerText = app.plexClient.GetPhotoTranscodeUrl(playback.Track.Thumb, 50, 50).ToString();
                messageNode.AppendChild(artworkNode);

                // <duration />
                var durationNode = message.CreateElement("duration");
                durationNode.InnerText = playback.Track.Duration.ToString();
                messageNode.AppendChild(durationNode);

                // <position />
                var positionNode = message.CreateElement("position");
                positionNode.InnerText = playback.Position.ToString();
                messageNode.AppendChild(positionNode);

                // <playing />
                var playingNode = message.CreateElement("playing");
                playingNode.InnerText = playback.PlaybackState == NAudio.Wave.PlaybackState.Playing ? "true" : "false";
                messageNode.AppendChild(playingNode);
            }
            
            // send to client
            var server = IPCServer.GetInstance();
            server.Send(message);
        }

        private void Playback_PlaybackTick(object sender, EventArgs e)
        {
            var playback = PlaybackManager.GetInstance();

            // construct message
            var factory = new IPCMessageFactory();
            var message = factory.Create("positionChanged", out XmlNode messageNode);

            // <position />
            var positionNode = message.CreateElement("position");
            positionNode.InnerText = playback.Position.ToString();
            messageNode.AppendChild(positionNode);

            // send to client
            var server = IPCServer.GetInstance();
            server.Send(message);
        }

        private void PlaybackManager_VolumeChanged(object sender, EventArgs e)
        {
            var playback = PlaybackManager.GetInstance();

            // construct message
            var factory = new IPCMessageFactory();
            var message = factory.Create("volumeChanged", out XmlNode messageNode);

            // <volume />
            var volumeNode = message.CreateElement("volume");
            volumeNode.InnerText = ((int)Math.Floor(playback.Volume * 100)).ToString();
            messageNode.AppendChild(volumeNode);

            // send to client
            var server = IPCServer.GetInstance();
            server.Send(message);
        }
    }
}
