using System;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Security.Principal;
using PlexLib;

namespace PlexFlux
{
    public class AppConfig
    {
        public string ClientIdentifier;
        public string AuthenticationToken;
        public string ServerMachineIdentifier;

        public float Volume;
        public bool IsShuffle;
        public bool IsRepeat;

        public int WindowSizeW, WindowSizeH;
        public int WindowPosX, WindowPosY;

        public AppConfig()
        {
            ClientIdentifier = PlexDeviceInfo.GenerateClientIdentifier();
            AuthenticationToken = string.Empty;
            ServerMachineIdentifier = string.Empty;

            Volume = 1.0f;
            IsShuffle = false;
            IsRepeat = false;

            WindowSizeW = WindowSizeH = int.MinValue;
            WindowPosX = WindowPosY = int.MinValue;
        }

        #region "Save / Load"
        public void Save()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(AppConfig));

            using (var stringWriter = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(stringWriter))
                {
                    serializer.Serialize(writer, this);

                    string rawXml = stringWriter.ToString();
                    string encrypted = SG.Algoritma.Cipher.Encrypt(rawXml, WindowsIdentity.GetCurrent().User.Value);

                    // save to file
                    var configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PlexFlux");

                    if (!Directory.Exists(configDir))
                        Directory.CreateDirectory(configDir);

                    var path = Path.Combine(configDir, "config.dat");
                    File.WriteAllText(path, encrypted);
                }
            }
        }

        public static AppConfig Load()
        {
            // load from file
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PlexFlux", "config.dat");
            var encrypted = File.ReadAllText(path);

            string rawXml = SG.Algoritma.Cipher.Decrypt(encrypted, WindowsIdentity.GetCurrent().User.Value);

            using (var reader = XmlReader.Create(new StringReader(rawXml)))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(AppConfig));
                return serializer.Deserialize(reader) as AppConfig;
            }
        }
        #endregion
    }
}
