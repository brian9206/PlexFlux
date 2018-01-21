using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Web;
using System.Threading.Tasks;

namespace PlexLib
{
    public class PlexTrack : IPlexMediaObject
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

        public string Thumb
        {
            get;
            internal set;
        }

        public string Summary
        {
            get
            {
                return Artist.Title;
            }
        }

        // optional
        public int PlaylistItemID
        {
            get;
            internal set;
        }

        // parent
        public PlexAlbum Album
        {
            get;
            internal set;
        }

        // grand parent
        public PlexArtist Artist
        {
            get;
            internal set;
        }
            
        public List<PlexMediaPart> Media
        {
            get;
            internal set;
        }

        /// <summary>
        /// Duration of this track (in seconds)
        /// </summary>
        public long Duration
        {
            get
            {
                var media = Media.FirstOrDefault();

                if (media == null)
                    return 0;

                return media.Duration / 1000;
            }
        }

        public PlexMediaPart FindByFormat(string format)
        {
            return Media.Where(media => media.Format == format).FirstOrDefault();
        }

        internal PlexTrack(XmlNode track)
        {
            MetadataUrl = track.Attributes["key"].InnerText;
            Title = HttpUtility.HtmlDecode(track.Attributes["title"].InnerText);
            Thumb = track.Attributes["thumb"] == null ? null : track.Attributes["thumb"].InnerText;
            PlaylistItemID = track.Attributes["playlistItemID"] == null ? -1 : int.Parse(track.Attributes["playlistItemID"].InnerText);
            Media = new List<PlexMediaPart>();
            Album = new PlexAlbum(track);
            Artist = new PlexArtist(track);

            // parse media part
            foreach (XmlNode media in track.SelectNodes("Media/Part"))
                Media.Add(new PlexMediaPart(media));
        }

        public async Task<PlexLibrary> LookupLibrary(PlexClient plex)
        {
            var album = (await plex.GetAlbums(Artist)).FirstOrDefault();

            if (album == null)
                return null;

            return album.Library;
        }
    }
}
