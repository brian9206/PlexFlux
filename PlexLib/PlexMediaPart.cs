using System;
using System.Xml;

namespace PlexLib
{
    public class PlexMediaPart
    {
        public string Url
        {
            get;
            internal set;
        }

        public long Duration
        {
            get;
            internal set;
        }

        public long Size
        {
            get;
            internal set;
        }

        public string Format
        {
            get;
            internal set;
        }

        internal PlexMediaPart(XmlNode media)
        {
            Url = media.Attributes["key"].InnerText;
            Duration = long.Parse(media.Attributes["duration"].InnerText);
            Size = long.Parse(media.Attributes["size"].InnerText);
            Format = media.Attributes["container"].InnerText;
        }
    }
}
