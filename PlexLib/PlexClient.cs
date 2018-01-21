using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Collections.Specialized;
using System.Web;
using System.Net;
using System.Threading.Tasks;

namespace PlexLib
{
    public class PlexClient
    {
        private PlexConnection connection;

        public PlexClient(PlexConnection connection)
        {
            this.connection = connection;
        }

        public async Task<PlexPlaylist[]> GetPlaylists()
        {
            var retval = new List<PlexPlaylist>();

            XmlDocument response = await connection.RequestXml("/playlists");
            XmlNode mediaContainer = response.SelectSingleNode("/MediaContainer");

            foreach (XmlNode playlist in mediaContainer.SelectNodes("Playlist"))
                retval.Add(new PlexPlaylist(playlist));

            return retval.ToArray();
        }

        public async Task<PlexLibrary[]> GetLibraries()
        {
            var retval = new List<PlexLibrary>();

            XmlDocument response = await connection.RequestXml("/library/sections");
            XmlNode mediaContainer = response.SelectSingleNode("/MediaContainer");

            foreach (XmlNode directory in mediaContainer.SelectNodes("Directory"))
                retval.Add(new PlexLibrary(directory, false));

            return retval.ToArray();
        }

        public async Task<PlexArtist[]> GetArtists(PlexLibrary library)
        {
            var retval = new List<PlexArtist>();

            XmlDocument response = await connection.RequestXml("/library/sections/" + library.Key + "/all", new NameValueCollection()
            {
                { "type", "8" },
                { "sort", "titleSort" }
            });
            XmlNode mediaContainer = response.SelectSingleNode("/MediaContainer");

            foreach (XmlNode directory in mediaContainer.SelectNodes("Directory"))
                retval.Add(new PlexArtist(mediaContainer, directory));

            return retval.ToArray();
        }

        public async Task<PlexAlbum[]> GetAlbums(PlexArtist artist)
        {
            var retval = new List<PlexAlbum>();

            XmlDocument response = await connection.RequestXml(artist.MetadataUrl);
            XmlNode mediaContainer = response.SelectSingleNode("/MediaContainer");

            foreach (XmlNode directory in mediaContainer.SelectNodes("Directory"))
                retval.Add(new PlexAlbum(mediaContainer, directory));

            return retval.ToArray();
        }

        public async Task<PlexAlbum[]> GetAlbums(PlexLibrary library)
        {
            var retval = new List<PlexAlbum>();

            XmlDocument response = await connection.RequestXml("/library/sections/" + library.Key + "/all", new NameValueCollection()
            {
                { "type", "9" },
                { "sort", "titleSort" }
            });
            XmlNode mediaContainer = response.SelectSingleNode("/MediaContainer");

            foreach (XmlNode directory in mediaContainer.SelectNodes("Directory"))
                retval.Add(new PlexAlbum(mediaContainer, directory));

            return retval.ToArray();
        }

        public async Task<PlexTrack[]> GetTracks(PlexAlbum artist)
        {
            var retval = new List<PlexTrack>();

            XmlDocument response = await connection.RequestXml(artist.MetadataUrl);
            XmlNode mediaContainer = response.SelectSingleNode("/MediaContainer");

            foreach (XmlNode track in mediaContainer.SelectNodes("Track"))
                retval.Add(new PlexTrack(track));

            return retval.ToArray();
        }

        public async Task<PlexTrack[]> GetTracks(PlexLibrary library)
        {
            var retval = new List<PlexTrack>();

            XmlDocument response = await connection.RequestXml("/library/sections/" + library.Key + "/all", new NameValueCollection()
            {
                { "type", "10" },
                { "sort", "titleSort" }
            });
            XmlNode mediaContainer = response.SelectSingleNode("/MediaContainer");

            foreach (XmlNode track in mediaContainer.SelectNodes("Track"))
                retval.Add(new PlexTrack(track));

            return retval.ToArray();
        }

        public async Task<PlexTrack[]> GetTracks(PlexPlaylist playlist)
        {
            var retval = new List<PlexTrack>();

            XmlDocument response = await connection.RequestXml(playlist.MetadataUrl);
            XmlNode mediaContainer = response.SelectSingleNode("/MediaContainer");

            foreach (XmlNode track in mediaContainer.SelectNodes("Track"))
                retval.Add(new PlexTrack(track));

            return retval.ToArray();
        }

        public async Task<PlexPlaylist> CreatePlaylist(string title, PlexTrack item = null)
        {
            string uri = "library://";

            if (item != null)
            {
                var library = await item.LookupLibrary(this);

                if (library == null)
                    throw new FileNotFoundException("Could not lookup library.");

                uri += library.UUID + "/item/" + HttpUtility.UrlEncode(item.MetadataUrl);
            }

            XmlDocument response = await connection.RequestXml("/playlists", new NameValueCollection()
            {
                { "type", "audio" },
                { "title", title },
                { "smart", "0" },
                { "uri", uri }
            }, "POST");

            XmlNode mediaContainer = response.SelectSingleNode("/MediaContainer");

            if (mediaContainer.Attributes["size"].InnerText != "1")
                throw new FileNotFoundException("No playlist has been created.");

            return new PlexPlaylist(mediaContainer.SelectNodes("Playlist")[0]);
        }

        public async Task<PlexPlaylist> AddItemToPlaylist(PlexPlaylist playlist, IPlexMediaObject item)
        {
            string uri = "library://";

            var library = await item.LookupLibrary(this);

            if (library == null)
                throw new FileNotFoundException("Could not lookup library.");

            var metadataUrl = item.MetadataUrl;

            uri += library.UUID + "/item/" + HttpUtility.UrlEncode(metadataUrl.EndsWith("/children") ? metadataUrl.Substring(0, metadataUrl.Length - "/children".Length) : metadataUrl);


            XmlDocument response = await connection.RequestXml(playlist.MetadataUrl, new NameValueCollection()
            {
                { "uri", uri }
            }, "PUT");

            XmlNode mediaContainer = response.SelectSingleNode("/MediaContainer");

            if (mediaContainer.Attributes["size"].InnerText != "1")
                throw new FileNotFoundException("No playlist has been created.");

            return new PlexPlaylist(mediaContainer.SelectNodes("Playlist")[0]);
        }

        public async Task DeletePlaylist(PlexPlaylist playlist)
        {
            if (!playlist.MetadataUrl.EndsWith("/items"))
                throw new InvalidOperationException("Invalid playlist object");

            await connection.RequestServer(playlist.MetadataUrl.Replace("/items", ""), null, "DELETE");
        }

        public async Task<PlexPlaylist> DeleteTrackFromPlaylist(PlexTrack track, PlexPlaylist playlist)
        {
            if (track.PlaylistItemID < 0)
                throw new InvalidOperationException("You can only delete track fetched by GetTracks(PlexPlaylist playlist)");

            XmlDocument response = await connection.RequestXml(playlist.MetadataUrl + "/" + track.PlaylistItemID, null, "DELETE");
            XmlNode mediaContainer = response.SelectSingleNode("/MediaContainer");

            if (mediaContainer.Attributes["size"].InnerText != "1")
                throw new FileNotFoundException("No playlist has been created.");

            return new PlexPlaylist(mediaContainer.SelectNodes("Playlist")[0]);
        }

        public async Task MoveTrackInPlaylist(PlexPlaylist playlist, PlexTrack track, PlexTrack after = null)
        {
            if (track.PlaylistItemID < 0 || (after != null && after.PlaylistItemID < 0))
                throw new InvalidOperationException("Track provided must be fetched by GetTracks(PlexPlaylist playlist)");

            NameValueCollection nvc = null;

            if (after != null)
            {
                nvc = new NameValueCollection()
                {
                    { "after", after.PlaylistItemID.ToString() }
                };
            }

            await connection.RequestServer(playlist.MetadataUrl + "/" + track.PlaylistItemID + "/move", nvc, "PUT");
        }

        public Uri GetPhotoTranscodeUrl(string url, int width, int height, bool minSize = true)
        {
            return connection.BuildRequestUrl("/photo/:/transcode", new NameValueCollection()
            {
                { "width", width.ToString() },
                { "height", height.ToString() },
                { "minSize", minSize ? "1" : "0" },
                { "url", url }
            });
        }

        public Uri GetMusicTranscodeUrl(PlexTrack track, int maxAudioBitrate)
        {
            return connection.BuildRequestUrl("/music/:/transcode/universal/start.mp3", new NameValueCollection()
            {
                { "hasMDE", "1" },
                { "path", track.MetadataUrl },
                { "mediaIndex", "0" },
                { "partIndex", "0" },
                { "maxAudioBitrate", maxAudioBitrate.ToString() },
                { "directStream", "0" },
                { "session", connection.DeviceInfo.ClientIdentifier },
                { "protocol", "http" },
                { "directPlay", "0" }
            });
        }
    }
}
