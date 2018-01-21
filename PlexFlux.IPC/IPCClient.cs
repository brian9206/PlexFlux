using System;
using System.Xml;
using NamedPipeWrapper;

namespace PlexFlux.IPC
{
    public class IPCClient
    {
        #region "Singleton"
        private static IPCClient instance = null;

        public static IPCClient GetInstance()
        {
            if (instance == null)
                instance = new IPCClient();

            return instance;
        }
        #endregion

        public bool Initialized
        {
            get;
            private set;
        }

        private NamedPipeClient<string> client;
        public event EventHandler Disconnected;
        public event EventHandler<XmlNode> MessageReceived;

        private IPCClient()
        {
            client = new NamedPipeClient<string>("PlexFlux_IPC_Pipe");
        }

        public void Init()
        {
            if (Initialized)
                return;

            Initialized = true;

            client.AutoReconnect = true;
            client.Disconnected += Client_Disconnected;
            client.ServerMessage += Client_ServerMessage;
            client.Start();
        }

        private void Client_ServerMessage(NamedPipeConnection<string, string> connection, string rawXml)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(rawXml);

            var messageNode = xml.SelectSingleNode("Message");
            if (messageNode == null)
                return;

            MessageReceived?.Invoke(this, messageNode);
        }

        private void Client_Disconnected(NamedPipeConnection<string, string> connection)
        {
            Disconnected?.Invoke(this, new EventArgs());
        }

        public void Send(XmlDocument xml)
        {
            client.PushMessage(xml.OuterXml);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    client.Disconnected -= Client_Disconnected;
                    client.ServerMessage -= Client_ServerMessage;
                    client.Stop();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
                Initialized = false;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~IPCClient() {
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
