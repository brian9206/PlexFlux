using System;
using System.Threading;
using System.Net;
using System.IO;
using NAudio.Wave;
using System.Threading.Tasks;

namespace PlexFlux.Streaming
{
    class Mp3StreamingProviderFactory : IDisposable
    {
        private HttpWebRequest request;
        private ReadFullyStream sourceStream;

        private BufferedWaveProvider bufferedWaveProvider;

        private bool fullyDownloaded;

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

        public Mp3StreamingProviderFactory(Uri url, string userAgent = null)
        {
            Aborted = false;
            Error = false;

            fullyDownloaded = false;

            sourceStream = null;
            bufferedWaveProvider = null;

            request = WebRequest.CreateHttp(url);
            request.Timeout = 5 * 1000;

            if (userAgent != null)
                request.UserAgent = userAgent;

            Task.Factory.StartNew(() =>
            {
                int retried = 0;

                while (sourceStream == null && retried < 3)
                {
                    try
                    {
                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
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

                if (sourceStream == null)
                {
                    request.Abort();
                    Error = true;
                    return;
                }

                var buffer = new byte[16384 * 4];
                IMp3FrameDecompressor decompressor = null;

                while (!disposedValue)
                {
                    if (bufferedWaveProvider != null &&
                           bufferedWaveProvider.BufferLength - bufferedWaveProvider.BufferedBytes
                           < bufferedWaveProvider.WaveFormat.AverageBytesPerSecond / 4)
                    {
                        Thread.Sleep(500);
                    }
                    else
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
                            // so if we stop playing music, we can abort the request
                            fullyDownloaded = true;
                            sourceStream.Dispose();
                            break;
                        }
                        catch (Exception)
                        {
                            // caused by .Abort()
                            Error = true;
                            break;
                        }

                        if (decompressor == null)
                        {
                            WaveFormat waveFormat = new Mp3WaveFormat(frame.SampleRate, frame.ChannelMode == ChannelMode.Mono ? 1 : 2, frame.FrameLength, frame.BitRate);
                            decompressor = new AcmMp3FrameDecompressor(waveFormat);

                            bufferedWaveProvider = new BufferedWaveProvider(decompressor.OutputFormat);
                            bufferedWaveProvider.BufferDuration = TimeSpan.FromSeconds(40);
                        }

                        int decompressed = decompressor.DecompressFrame(frame, buffer, 0);
                        bufferedWaveProvider.AddSamples(buffer, 0, decompressed);
                    }
                }

                if (decompressor != null)
                    decompressor.Dispose();
            });
        }

        public Task<IWaveProvider> GetWaveProvider()
        {
            return Task.Run<IWaveProvider>(() =>
            {
                while (bufferedWaveProvider == null)
                {
                    if (disposedValue || Error)
                        return null;

                    Thread.Sleep(500);
                }

                return bufferedWaveProvider;
            });
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (!fullyDownloaded)
                    {
                        Aborted = true;

                        try
                        {
                            request.Abort();
                            sourceStream.Dispose();
                        }
                        catch (Exception)
                        {
                            // maybe it is disposed already
                        }
                    }
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
