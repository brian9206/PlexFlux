using System;
using System.Xml;
using System.Web;
using System.Threading.Tasks;

namespace PlexLib
{
    public class PlexAlbum : IPlexMediaObject
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

        // library
        public PlexLibrary Library
        {
            get;
            internal set;
        }

        internal PlexAlbum(XmlNode mediaContainer, XmlNode directory)
        {
            IsBrief = false;
            MetadataUrl = directory.Attributes["key"].InnerText;
            Title = HttpUtility.HtmlDecode(directory.Attributes["title"].InnerText);
            Summary = HttpUtility.HtmlDecode(directory.Attributes["summary"].InnerText);
            Thumb = directory.Attributes["thumb"] == null ? null : directory.Attributes["thumb"].InnerText;
            Library = new PlexLibrary(mediaContainer, true);
        }

        internal PlexAlbum(XmlNode track)
        {
            IsBrief = true;
            MetadataUrl = track.Attributes["parentKey"].InnerText;
            Title = HttpUtility.HtmlDecode(track.Attributes["parentTitle"].InnerText);
            Thumb = track.Attributes["parentThumb"] == null ? null : track.Attributes["parentThumb"].InnerText;
        }

        public Task<PlexLibrary> LookupLibrary(PlexClient client)
        {
            return Task.FromResult(Library);
        }
    }
}
