using System;
using System.Xml;
using System.Web;
using System.Linq;
using System.Threading.Tasks;

namespace PlexLib
{
    public class PlexLibrary
    {
        public int Key
        {
            get;
            internal set;
        }

        public string UUID
        {
            get;
            internal set;
        }

        public string Type
        {
            get;
            internal set;
        }

        public string Title
        {
            get;
            internal set;
        }

        internal PlexLibrary(XmlNode node, bool isMediaContainer)
        {
            if (isMediaContainer)
            {
                // <MediaContainer />
                Key = int.Parse(node.Attributes["librarySectionID"].InnerText);
                UUID = node.Attributes["librarySectionUUID"].InnerText;
                Title = HttpUtility.HtmlDecode(node.Attributes["librarySectionTitle"].InnerText);
            }
            else
            {
                // <Directory />
                Key = int.Parse(node.Attributes["key"].InnerText);
                UUID = node.Attributes["uuid"].InnerText;
                Title = HttpUtility.HtmlDecode(node.Attributes["title"].InnerText);
                Type = node.Attributes["type"].InnerText;
            }
        }

        public async Task<PlexLibrary> GetDetailed(PlexClient plex)
        {
            return (await plex.GetLibraries())
                .Where(library => library.Key == Key)
                .FirstOrDefault();
        }
    }
}
