using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Xml;
using System.IO;
using System.Web;
using System.Threading.Tasks;

namespace PlexLib
{
    public class MyPlexClient
    {
        private PlexDeviceInfo deviceInfo;

        public string AuthenticationToken
        {
            get;
            private set;
        }

        public MyPlexClient(PlexDeviceInfo deviceInfo, string authToken = null)
        {
            this.deviceInfo = deviceInfo;
            AuthenticationToken = authToken;
        }

        // 401 = fail
        public async Task SignIn(string username, string password)
        {
            NameValueCollection query = new NameValueCollection()
            {
                { "X-Plex-Product", deviceInfo.ProductName },
                { "X-Plex-Version", deviceInfo.ProductVersion },
                { "X-Plex-Client-Identifier", deviceInfo.ClientIdentifier },
                { "user[login]", username },
                { "user[password]", password },
            };
            byte[] postData = Encoding.UTF8.GetBytes(string.Join("&", query.AllKeys.Select(key => string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(query[key])))));

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://plex.tv/users/sign_in.xml");
            request.UserAgent = deviceInfo.UserAgent;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = postData.Length;

            var requestStream = request.GetRequestStream();
            requestStream.Write(postData, 0, postData.Length);

            var response = await request.GetResponseAsync();
            var responseStream = response.GetResponseStream();

            var reader = new StreamReader(responseStream);

            var xml = new XmlDocument();
            xml.LoadXml(reader.ReadToEnd());

            AuthenticationToken = xml.SelectSingleNode("/user/authentication-token").InnerText;
        }

        public async Task<PlexServer[]> GetServers()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://plex.tv/devices.xml?X-Plex-Token=" + HttpUtility.UrlEncode(AuthenticationToken));
            request.UserAgent = deviceInfo.UserAgent;

            var response = await request.GetResponseAsync();
            var responseStream = response.GetResponseStream();

            var reader = new StreamReader(responseStream);

            var xml = new XmlDocument();
            xml.LoadXml(reader.ReadToEnd());

            var retval = new List<PlexServer>();

            foreach (XmlNode device in xml.SelectNodes("/MediaContainer/Device"))
            {
                // search for PMS only
                if (device.Attributes["product"] != null && device.Attributes["product"].Value != "Plex Media Server")
                    continue;

                // find connection
                string address = null;
                var localAddresses = new List<string>();

                // rfc1918 regex
                var rfc1918 = new Regex(@"(^127\.0\.0\.1)|(^10\.)|(^172\.1[6-9]\.)|(^172\.2[0-9]\.)|(^172\.3[0-1]\.)|(^192\.168\.)");

                foreach (XmlNode connection in device.SelectNodes("Connection"))
                {
                    Uri uri = new Uri(connection.Attributes["uri"].Value);

                    if (uri.AbsolutePath != "/")
                        continue;

                    // check rfc1918
                    if (rfc1918.Match(uri.Host).Success)
                    {
                        localAddresses.Add(uri.ToString());
                    }
                    else if (address == null)
                    {
                        address = uri.ToString();
                    }
                }

                var plexServer = new PlexServer();
                plexServer.Name = device.Attributes["name"].InnerText;
                plexServer.Address = address;
                plexServer.LocalAddressList = localAddresses.ToArray();
                plexServer.AccessToken = device.Attributes["token"].InnerText;
                plexServer.MachineIdentifier = device.Attributes["clientIdentifier"].InnerText;

                retval.Add(plexServer);
            }

            return retval.ToArray();
        }
    }
}
