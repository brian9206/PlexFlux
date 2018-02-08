using System;
using System.Threading;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using NAudio.Wave;
using System.Windows;

namespace PlexFlux.Streaming
{
    class Mp3Streaming : IDisposable
    {
        private HttpWebRequest request;
        private HttpWebResponse response;
        private ReadFullyStream sourceStream;
        private StreamingWaveProvider waveProvider;
        private ManualResetEvent instantiateWaitHandle;
        private TimeSpan startTime;
        
        public bool Started
        {
            get;
            private set;
        }

        public bool Aborted
        {
            get;
            private set;
        }

        public bool Error
        {
            get;
            private set;
        }

        public TimeSpan Current
        {
            get
            {
                if (waveProvider == null)
                    return startTime;

                return waveProvider.Current;
            }

            set
            {
                if (waveProvider == null)
                {
                    startTime = value;
                    return;
                }

                waveProvider.Current = value;
            }
        }

        public bool IsBuffering
        {
            get =>
                waveProvider == null || (
                    waveProvider != null && (
                        waveProvider.BufferedDuration < waveProvider.MinimumBufferedDuration ||
                        waveProvider.BufferedDuration < Current
                    )
                );
        }

        public Mp3Streaming(HttpWebRequest request)
        {
            this.request = request;
            sourceStream = null;
            waveProvider = null;
            instantiateWaitHandle = new ManualResetEvent(false);
            Current = TimeSpan.Zero;
        }

        public IWaveProvider Start()
        {
            if (!Started)
                Task.Factory.StartNew(StreamFromHttp, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            // wait before the wave provider being instantiated
            instantiateWaitHandle.WaitOne();
            return waveProvider;
        }

        private void StreamFromHttp()
        {
            Started = true;

            // make request to the server with retry mechanism
            int retried = 0;

            while (sourceStream == null && retried < 3)
            {
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                    sourceStream = new ReadFullyStream(response.GetResponseStream());
                }
                catch (Exception)
                {
                    // retry!
                    Thread.Sleep(1000);
                    retried++;
                }

                if (Aborted)
                    return;
            }

            // failed all 3 retries
            if (sourceStream == null)
            {
                request.Abort();
                Error = true;
                return;
            }

            // start mp3 decompression process
            var buffer = new byte[16384 * 4];
            IMp3FrameDecompressor decompressor = null;

            while (!disposedValue)
            {
                Mp3Frame frame = null;

                try
                {
                    frame = Mp3Frame.LoadFromStream(sourceStream);

                    if (frame == null)
                        throw new EndOfStreamException("No more MP3 Frame");
                }
                catch (EndOfStreamException)
                {
                    // mark as fully downloaded
                    waveProvider.FullyDownloaded = true;
                    break;
                }
                catch
                {
                    // caused by .Abort()
                    Error = true;
                    break;
                }

                if (decompressor == null)
                {
                    WaveFormat waveFormat = new Mp3WaveFormat(frame.SampleRate, frame.ChannelMode == ChannelMode.Mono ? 1 : 2, frame.FrameLength, frame.BitRate);
                    decompressor = new AcmMp3FrameDecompressor(waveFormat);

                    var app = (App)Application.Current;
                    ulong waveSize = 0;

                    if (!app.config.DisableDiskCaching)
                    {
                        // calculate decompressed wave size
                        waveSize = (ulong)(waveFormat.SampleRate * 16 * waveFormat.Channels * PlaybackManager.GetInstance().Track.Duration / 8);    // workaround: 16 = waveFormat.BitsPerSample, sometimes waveFormat.BitsPerSample is equals to 0
                    }

                    waveProvider = new StreamingWaveProvider(decompressor.OutputFormat, waveSize)
                    {
                        Current = startTime
                    };

                    // here we are
                    instantiateWaitHandle.Set();
                }

                int decompressed = decompressor.DecompressFrame(frame, buffer, 0);
                waveProvider.AddSamples(buffer, 0, decompressed);
            }

            // dispose decompressor if present
            if (decompressor != null)
                decompressor.Dispose();
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (!Aborted && (waveProvider == null || !waveProvider.FullyDownloaded))
                    {
                        Aborted = true;

                        request.Abort();

                        if (sourceStream != null)
                            sourceStream.Dispose();
                    }

                    if (waveProvider != null)
                    {
                        waveProvider.Dispose();
                        waveProvider = null;
                    }

                    instantiateWaitHandle.Set();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
