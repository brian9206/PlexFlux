using System;
using System.Linq;
using System.Collections.Specialized;
using System.Net;
using System.Web;
using System.IO;
using System.Xml;
using System.Threading.Tasks;

namespace PlexLib
{
    public class PlexConnection
    {
        private PlexServer server;
        private PlexDeviceInfo deviceInfo;

        public PlexDeviceInfo DeviceInfo
        {
            get { return deviceInfo; }
        }

        public PlexConnection(PlexDeviceInfo deviceInfo, PlexServer server)
        {
            this.server = server;
            this.deviceInfo = deviceInfo;
        }

        public Uri BuildRequestUrl(string endpoint, NameValueCollection query = null)
        {
            // append default params
            if (query == null)
                query = new NameValueCollection();

            query["X-Plex-Product"] = deviceInfo.ProductName;
            query["X-Plex-Version"] = deviceInfo.ProductVersion;
            query["X-Plex-Client-Identifier"] = deviceInfo.ClientIdentifier;
            query["X-Plex-Platform"] = deviceInfo.Platform;
            query["X-Plex-Platform-Version"] = deviceInfo.PlatformVersion;
            query["X-Plex-Device"] = deviceInfo.Device;
            query["X-Plex-Device-Name"] = deviceInfo.DeviceName;
            query["X-Plex-Device-Screen-Resolution"] = deviceInfo.DeviceScreenResolution;
            query["X-Plex-Token"] = server.AccessToken;

            // build url
            var builder = new UriBuilder(server.Url);
            builder.Path = endpoint;
            builder.Query = string.Join("&", query.AllKeys.Select(key => string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(query[key]))));

            Uri url = new Uri(builder.ToString(), UriKind.Absolute);
            return url;
        }

        public HttpWebRequest CreateRequest(string endpoint, NameValueCollection query = null, string method = "GET")
        {
            return CreateRequest(BuildRequestUrl(endpoint, query), method);
        }

        public HttpWebRequest CreateRequest(Uri endpoint, string method = "GET")
        {
            // request server
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
            request.Method = method;
            request.UserAgent = deviceInfo.UserAgent;
            request.Timeout = 5 * 1000;

            return request;
        }

        public async Task<HttpWebResponse> RequestServer(string endpoint, NameValueCollection query = null, string method = "GET")
        {
            return (HttpWebResponse)(await CreateRequest(endpoint, query, method).GetResponseAsync());
        }

        public async Task<XmlDocument> RequestXml(string endpoint, NameValueCollection query = null, string method = "GET")
        {
            // reqeust server
            var response = await RequestServer(endpoint, query, method);
            var responseReader = new StreamReader(response.GetResponseStream());

            // parse xml
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(responseReader.ReadToEnd());

            return xml;
        }
    }
}
