using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using PlexLib;
using PlexFlux.Streaming;

namespace PlexFlux
{
    class PlaybackManager
    {
        #region "Singleton"
        private static PlaybackManager instance = null;

        public static PlaybackManager GetInstance()
        {
            if (instance == null)
                instance = new PlaybackManager();

            return instance;
        }
        #endregion

        private Mp3StreamingProviderFactory sourceProviderFactory = null;

        private IWavePlayer waveOut = null;
        private VolumeWaveProvider16 provider = null;

        private bool initializing = false;
        private bool pauseAfterInit = false;
        private float volume = 1.0f;
        private bool shuffle = false;
        private bool repeat = false;

        public event EventHandler StartPlaying;
        public event EventHandler PlaybackStateChanged;
        public event EventHandler VolumeChanged;
        public event EventHandler ShuffleChanged;
        public event EventHandler RepeatChanged;
        public event EventHandler PlaybackTick;

        private EventWaitHandle initEvent;

        public PlexTrack Track
        {
            get;
            private set;
        }

        public float Volume
        {
            get
            {
                return volume;
            }
            set
            {
                volume = value;

                if (provider != null)
                    provider.Volume = volume;

                // update app config
                var app = (App)Application.Current;
                app.config.Volume = value;

                // invoke event
                VolumeChanged?.Invoke(this, new EventArgs());
            }
        }

        public long Position
        {
            get
            {
                if (initializing)
                    return 0;

                if (waveOut == null)
                    return 0;

                var type = waveOut.GetType();

                if (type == typeof(WaveOut))
                    return (long)(((WaveOut)waveOut).GetPosition() * 1d / ((WaveOut)waveOut).OutputWaveFormat.AverageBytesPerSecond);
                else if (type == typeof(WasapiOut))
                    return (long)(((WasapiOut)waveOut).GetPosition() * 1d / ((WasapiOut)waveOut).OutputWaveFormat.AverageBytesPerSecond);

                return 0;
            }
        }

        public PlaybackState PlaybackState
        {
            get
            {
                if (initializing)
                    return pauseAfterInit ? PlaybackState.Paused : PlaybackState.Playing;

                if (waveOut == null)
                    return PlaybackState.Stopped;

                return waveOut.PlaybackState;
            }
        }

        public bool IsRepeat
        {
            get { return repeat; }
            set
            {
                repeat = value;

                // update app config
                var app = (App)Application.Current;
                app.config.IsRepeat = value;

                RepeatChanged?.Invoke(this, new EventArgs());
            }
        }

        public bool IsShuffle
        {
            get { return shuffle; }
            set
            {
                shuffle = value;

                // update app config
                var app = (App)Application.Current;
                app.config.IsShuffle = value;

                ShuffleChanged?.Invoke(this, new EventArgs());
            }
        }

        private PlaybackManager()
        {
            initEvent = new AutoResetEvent(false);
            Track = null;

            // restore config
            try
            {
                var app = (App)Application.Current;
                Volume = app.config.Volume;
                IsShuffle = app.config.IsShuffle;
                IsRepeat = app.config.IsRepeat;
            }
            catch (InvalidCastException)
            {
                // probably in design mode
            }
        }

        private async Task PlaybackTask()
        {
            var app = (App)Application.Current;

            // make transcode URL
            var url = app.plexClient.GetMusicTranscodeUrl(Track, app.config.TranscodeBitrate < 0 ? 320 : app.config.TranscodeBitrate);

            if (app.config.TranscodeBitrate < 0)
            {
                // try to find mp3 so no transcode is needed if found
                var media = Track.FindByFormat("mp3");

                if (media != null)
                    url = app.plexConnection.BuildRequestUrl(media.Url);
            }

            var factory = new Mp3StreamingProviderFactory(url, app.plexConnection.DeviceInfo.UserAgent);
            sourceProviderFactory = factory;

            var sourceProvider = await factory.GetWaveProvider();

            // check if we have aborted
            if (sourceProvider == null)
            {
                if (!factory.Aborted && factory.Error)
                    PlayNextTrack();

                return;
            }
                
            provider = new VolumeWaveProvider16(sourceProvider);
            provider.Volume = volume;

            // init output
            waveOut.Init(provider);
            waveOut.Play();

            if (pauseAfterInit)
                waveOut.Pause();

            // init completed
            initializing = false;
            pauseAfterInit = false;
            initEvent.Set();

            // stop after playback is complete
            while (waveOut != null && waveOut.PlaybackState != PlaybackState.Stopped)
            {
                // tick
                PlaybackTick?.Invoke(this, new EventArgs());

                if (sourceProviderFactory.Error || Position >= Track.Duration)
                {
                    waveOut.Stop();
                    PlayNextTrack();

                    break;
                }
                    
                await Task.Delay(250);
            }
        }

        public void Reset()
        {
            initializing = false;
            pauseAfterInit = false;

            // clean up previous session
            if (waveOut != null)
            {
                waveOut.Stop();
                waveOut.Dispose();
            }

            if (sourceProviderFactory != null)
                sourceProviderFactory.Dispose();

            var app = (App)Application.Current;

            // create new session
            waveOut = new WasapiOut(app.GetDeviceByID(app.config.OutputDeviceID), app.config.IsExclusive ? AudioClientShareMode.Exclusive : AudioClientShareMode.Shared, false, 100);
            waveOut.PlaybackStopped += WaveOut_PlaybackStopped;
        }

        private void WaveOut_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            PlaybackStateChanged?.Invoke(Track, new EventArgs());
        }

        public void Play(PlexTrack track)
        {
            // wait before the previous unfinished init to be completed first
            if (initializing)
                initEvent.WaitOne();

            // reset!
            Reset();

            // we are initializing
            initializing = true;

            // create new session
            Track = track;

            // start async task
            Task.Factory.StartNew(PlaybackTask);

            // invoke event
            StartPlaying?.Invoke(track, new EventArgs());
            PlaybackStateChanged?.Invoke(track, new EventArgs());
        }

        private void PlayNextTrack()
        {
            var playQueue = PlayQueueManager.GetInstance();
            var upcomings = UpcomingManager.GetInstance();

            PlexTrack track = null;

            // upcomings
            track = upcomings.Pop();

            // play queue
            if (track == null)
            {
                int index = playQueue.GetNextTrackIndex(IsShuffle);

                if (index > 0)
                {
                    track = playQueue.Play(index);
                }
                else if (IsRepeat)
                {
                    playQueue.ResetPlayedIndexes();

                    if (playQueue.Count > 1)
                        playQueue.AddPlayedIndex(playQueue.Current);    // do not repeat the current track

                    index = playQueue.GetNextTrackIndex(IsShuffle);

                    if (index >= 0)
                        track = playQueue.Play(index);
                }
            }

            // handle track
            if (track == null)
            {
                Track = null;
                Reset();
                return;
            }

            Play(track);
        }

        public void Pause()
        {
            if (initializing)
                pauseAfterInit = true;

            if (waveOut == null)
                return;

            waveOut.Pause();

            PlaybackStateChanged?.Invoke(Track, new EventArgs());
        }

        public void Resume()
        {
            if (initializing)
                pauseAfterInit = false;

            if (waveOut == null)
                return;

            waveOut.Play();

            PlaybackStateChanged?.Invoke(Track, new EventArgs());
        }
    }
}
