using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Pipes;
using System.Xml;
using System.Threading;
using NamedPipeWrapper;

namespace PlexFlux.IPC
{
    public class IPCServer : IDisposable
    {
        #region "Singleton"
        private static IPCServer instance = null;

        public static IPCServer GetInstance()
        {
            if (instance == null)
                instance = new IPCServer();

            return instance;
        }
        #endregion

        public bool Initialized
        {
            get;
            private set;
        }

        private NamedPipeServer<string> server;

        public event EventHandler<XmlNode> MessageReceived;
        public event EventHandler Connected;
        public event EventHandler Disconnected;

        private IPCServer()
        {
            server = new NamedPipeServer<string>("PlexFlux_IPC_Pipe");
        }

        public void Init()
        {
            if (Initialized)
                return;

            Initialized = true;

            server.ClientConnected += Server_ClientConnected;
            server.ClientMessage += Server_ClientMessage;
            server.ClientDisconnected += Server_ClientDisconnected;
            server.Start();
        }

        private void Server_ClientConnected(NamedPipeConnection<string, string> connection)
        {
            Connected?.Invoke(this, new EventArgs());
        }

        private void Server_ClientMessage(NamedPipeConnection<string, string> connection, string message)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(message);

            var messageNode = xml.SelectSingleNode("Message");
            if (messageNode == null)
                return;

            MessageReceived?.Invoke(this, messageNode);
        }

        private void Server_ClientDisconnected(NamedPipeConnection<string, string> connection)
        {
            Disconnected?.Invoke(this, new EventArgs());
        }

        public void Send(XmlDocument xml)
        {
            server.PushMessage(xml.OuterXml);
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
                    server.ClientConnected -= Server_ClientConnected;
                    server.ClientMessage -= Server_ClientMessage;
                    server.ClientDisconnected -= Server_ClientDisconnected;
                    server.Stop();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
                Initialized = false;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~IPCServer() {
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

        public void Close()
        {
            Dispose();
        }
    }
}
