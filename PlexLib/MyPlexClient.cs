using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
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
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://plex.tv/pms/servers.xml?X-Plex-Token=" + HttpUtility.UrlEncode(AuthenticationToken));
            request.UserAgent = deviceInfo.UserAgent;

            var response = await request.GetResponseAsync();
            var responseStream = response.GetResponseStream();

            var reader = new StreamReader(responseStream);

            var xml = new XmlDocument();
            xml.LoadXml(reader.ReadToEnd());

            var retval = new List<PlexServer>();

            foreach (XmlNode server in xml.SelectNodes("/MediaContainer/Server"))
            {
                var plexServer = new PlexServer();
                plexServer.Name = server.Attributes["name"].InnerText;
                plexServer.Address = server.Attributes["address"].InnerText;
                plexServer.LocalAddressList = server.Attributes["localAddresses"].InnerText.Split(',');
                plexServer.Port = int.Parse(server.Attributes["port"].InnerText);
                plexServer.Scheme = server.Attributes["scheme"].InnerText;
                plexServer.AccessToken = server.Attributes["accessToken"].InnerText;
                plexServer.MachineIdentifier = server.Attributes["machineIdentifier"].InnerText;

                retval.Add(plexServer);
            }

            return retval.ToArray();
        }
    }
}
