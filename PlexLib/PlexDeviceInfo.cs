using System;
using System.Linq;

namespace PlexLib
{
    public class PlexDeviceInfo
    {
        public string ProductName
        {
            get;
            private set;
        }

        public string ProductVersion
        {
            get;
            private set;
        }

        public string ClientIdentifier
        {
            get;
            private set;
        }

        public string Platform
        {
            get;
            private set;
        }

        public string PlatformVersion
        {
            get;
            private set;
        }

        public string Device
        {
            get;
            private set;
        }

        public string DeviceName
        {
            get;
            private set;
        }

        public string DeviceScreenResolution
        {
            get
            {
                return "1920x1080";
            }
        }

        public string UserAgent
        {
            get
            {
                return ProductName + "/" + ProductVersion;
            }
        }

        public PlexDeviceInfo(string productName, string productVersion, string clientIdentifier = null)
        {
            this.ProductName = productName;
            this.ProductVersion = productVersion;
            this.ClientIdentifier = clientIdentifier != null ? clientIdentifier : GenerateClientIdentifier();
            this.Platform = IsLinux ? "Linux" : "Windows";
            this.PlatformVersion = Environment.OSVersion.VersionString;
            this.Device = IsLinux ? "Linux" : "Windows";
            this.DeviceName = Environment.MachineName;
        }

        private static bool IsLinux
        {
            get
            {
                int p = (int)Environment.OSVersion.Platform;
                return (p == 4) || (p == 6) || (p == 128);
            }
        }

        public static string GenerateClientIdentifier()
        {
            var random = new Random();

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 25)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
