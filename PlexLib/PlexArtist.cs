using System;
using System.Xml;
using System.Web;

namespace PlexLib
{
    public class PlexArtist
    {
        public bool IsBrief
        {
            get;
            private set;
        }

        public string MetadataUrl
        {
            get;
            internal set;
        }

        public string Title
        {
            get;
            internal set;
        }

        public string Summary
        {
            get;
            internal set;
        }

        public string Thumb
        {
            get;
            internal set;
        }

        public PlexLibrary Library
        {
            get;
            private set;
        }

        internal PlexArtist(XmlNode mediaContainer, XmlNode directory)
        {
            IsBrief = false;
            MetadataUrl = directory.Attributes["key"].InnerText;
            Title = HttpUtility.HtmlDecode(directory.Attributes["title"].InnerText);
            Summary = HttpUtility.HtmlDecode(directory.Attributes["summary"].InnerText);
            Thumb = directory.Attributes["thumb"] == null ? null : directory.Attributes["thumb"].InnerText;
            Library = new PlexLibrary(mediaContainer, true);
        }

        internal PlexArtist(XmlNode track)
        {
            IsBrief = true;
            MetadataUrl = track.Attributes["grandparentKey"].InnerText;
            Title = HttpUtility.HtmlDecode(track.Attributes["grandparentTitle"].InnerText);
            Thumb = track.Attributes["grandparentThumb"] == null ? null : track.Attributes["grandparentThumb"].InnerText;
        }
    }
}
