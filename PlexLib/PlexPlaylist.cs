using System;
using System.ComponentModel;
using System.Xml;
using System.Web;

namespace PlexLib
{
    public class PlexPlaylist
    {
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

        public string Type
        {
            get;
            internal set;
        }

        public int LeafCount
        {
            get;
            internal set;
        }

        public long Duration
        {
            get;
            internal set;
        }

        internal PlexPlaylist(XmlNode playlist)
        {
            MetadataUrl = playlist.Attributes["key"].InnerText;
            Title = HttpUtility.HtmlDecode(playlist.Attributes["title"].InnerText);
            Type = playlist.Attributes["playlistType"].InnerText;
            LeafCount = int.Parse(playlist.Attributes["leafCount"].InnerText);
            Duration = playlist.Attributes["duration"] == null ? 0 : int.Parse(playlist.Attributes["duration"].InnerText);
        }
    }
}
