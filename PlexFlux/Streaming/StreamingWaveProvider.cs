using System;
using System.Threading;
using System.IO;
using NAudio.Wave;

namespace PlexFlux.Streaming
{
    class StreamingWaveProvider : IWaveProvider, IDisposable
    {
        private readonly MemoryStream stream;
        private long readPosition = 0;
        private long writePosition = 0;

        public long BufferedBytes
        {
            get => writePosition;
        }

        public WaveFormat WaveFormat
        {
            get;
            private set;
        }

        /// <summary>
        /// The number of buffered bytes
        /// </summary>
        public TimeSpan BufferedDuration
        {
            get => TimeSpan.FromSeconds((double)BufferedBytes / WaveFormat.AverageBytesPerSecond);
        }

        public TimeSpan MinimumBufferedDuration
        {
            get;
            set;
        }

        public TimeSpan Current
        {
            get => TimeSpan.FromSeconds((double)readPosition / WaveFormat.AverageBytesPerSecond);
            set => readPosition = (long)(value.TotalSeconds * WaveFormat.AverageBytesPerSecond);
        }

        public bool FullyDownloaded
        {
            get;
            set;
        }

        public StreamingWaveProvider(WaveFormat waveFormat)
        {
            stream = new MemoryStream();
            WaveFormat = waveFormat;
            MinimumBufferedDuration = TimeSpan.FromSeconds(2);  // at least 2s ahead
        }

        public void AddSamples(byte[] buffer, int offset, int count)
        {
            lock (stream)
            {
                stream.Position = writePosition;
                stream.Write(buffer, offset, count);
                writePosition = stream.Position;
            }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            while (!disposedValue && 
                !FullyDownloaded && 
                Current > TimeSpan.FromSeconds(1) && 
                BufferedDuration - Current < MinimumBufferedDuration)
            {
                // workaround: make sure we can abort quickly
                int wait = (int)(MinimumBufferedDuration.TotalMilliseconds);

                while (!disposedValue && wait > 0)
                {
                    Thread.Sleep(100);
                    wait -= 100;
                }
            }

            byte[] readAheadBuffer = new byte[count];
            int readAhead = 0;

            while (!disposedValue && readAhead < count)
            {
                readAhead += ReadSamples(readAheadBuffer, readAhead, count - readAhead);
            }

            if (disposedValue)
                return 0;

            Array.Copy(readAheadBuffer, buffer, count);
            return count;
        }

        private int ReadSamples(byte[] buffer, int offset, int count)
        {
            lock (stream)
            {
                stream.Position = readPosition;
                int read = stream.Read(buffer, offset, count);
                readPosition = stream.Position;

                return read;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    stream.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~FullyBufferedWaveProvider() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
