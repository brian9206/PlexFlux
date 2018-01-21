using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlexLib
{
    public interface IPlexMediaObject
    {
        string MetadataUrl
        {
            get;
        }

        string Title
        {
            get;
        }

        string Thumb
        {
            get;
        }

        string Summary
        {
            get;
        }

        Task<PlexLibrary> LookupLibrary(PlexClient plex);
    }
}
