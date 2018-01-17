using System;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Xml;
using System.Collections.Generic;

namespace PlexLib
{
    public class PlexServer
    {
        private string selectedAddress = null;

        public string Url
        {
            get
            {
                return Scheme + "://" + selectedAddress + ":" + Port;
            }
        }

        public string Name
        {
            get;
            internal set;
        }

        public string Address
        {
            get;
            internal set;
        }

        public string[] LocalAddressList
        {
            get;
            internal set;
        }

        public int Port
        {
            get;
            internal set;
        }

        public string Scheme
        {
            get;
            internal set;
        }

        public string AccessToken
        {
            get;
            internal set;
        }

        public string MachineIdentifier
        {
            get;
            internal set;
        }

        public bool HasSelectedAddress
        {
            get
            {
                return selectedAddress != null;
            }
        }

        public bool SelectAddress()
        {
            // get fastest connection in local address list
            var tasks = new Task<bool>[LocalAddressList.Length];
            var requests = new HttpWebRequest[LocalAddressList.Length];

            for (int i = 0; i < LocalAddressList.Length; i++)
            {
                int idx = i;
                tasks[idx] = Task.Run(() =>
                {
                    requests[idx] = CreateConnectivityRequest(LocalAddressList[idx]);
                    return CheckConnectivity(requests[idx]);
                });
            }
                
            int index = Task.WaitAny(tasks);

            if (tasks[index].Result)
            {
                selectedAddress = LocalAddressList[index];

                // abort all connections
                foreach (var request in requests)
                    request.Abort();
            }
            else
            {
                var request = CreateConnectivityRequest(Address);

                // no host available
                if (!CheckConnectivity(request))
                    return false;

                selectedAddress = Address;
            }

            return true;
        }

        private bool CheckConnectivity(HttpWebRequest request)
        {
            try
            {
                var response = request.GetResponse();
                var streamReader = new StreamReader(response.GetResponseStream());

                var xml = new XmlDocument();
                xml.LoadXml(streamReader.ReadToEnd());

                var serverInfo = xml.SelectSingleNode("/MediaContainer");
                if (serverInfo.Attributes["machineIdentifier"].InnerText != MachineIdentifier)
                    throw new WebException("MachineIdentifier is not match.");

                return true;
            }
            catch (Exception)
            {
                System.Threading.Thread.Sleep(5000);
                return false;
            }
        }

        private HttpWebRequest CreateConnectivityRequest(string address)
        {
            var request = (HttpWebRequest)WebRequest.Create(Scheme + "://" + address + ":" + Port + "/identity");
            request.Timeout = 5 * 1000;

            return request;
        }

        internal PlexServer()
        {
        }

        public override string ToString()
        {
            return Url;
        }
    }
}
