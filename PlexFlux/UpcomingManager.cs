using System;
using System.Collections.Generic;
using System.Linq;
using PlexLib;

namespace PlexFlux
{
    class UpcomingManager
    {
        #region "Singleton"
        private static UpcomingManager instance = null;

        public static UpcomingManager GetInstance()
        {
            if (instance == null)
                instance = new UpcomingManager();

            return instance;
        }
        #endregion

        private LinkedList<PlexTrack> tracks;

        public event EventHandler TrackChanged;

        private UpcomingManager()
        {
            tracks = new LinkedList<PlexTrack>();
        }

        public void Push(PlexTrack track)
        {
            lock (tracks)
                tracks.AddFirst(track);

            TrackChanged?.Invoke(this, new EventArgs());
        }

        public PlexTrack Pop()
        {
            if (tracks.Count == 0)
                return null;

            PlexTrack track;

            lock (tracks)
            {
                track = tracks.First.Value;
                tracks.RemoveFirst();
            }

            TrackChanged?.Invoke(this, new EventArgs());

            return track;
        }

        public PlexTrack Remove(int index)
        {
            LinkedListNode<PlexTrack> trackNode = tracks.First;

            lock (tracks)
            {
                for (int i = 0; i < index && trackNode != null; i++)
                    trackNode = trackNode.Next;

                if (trackNode != null)
                    tracks.Remove(trackNode);
            }

            if (trackNode == null)
                return null;

            TrackChanged?.Invoke(this, new EventArgs());
            return trackNode.Value;
        }

        public void Rearrange(int from, int to)
        {
            if (from == to)
                return;

            lock (tracks)
            {
                // remove
                var track = Remove(from);

                // get node by index
                LinkedListNode<PlexTrack> trackNode = tracks.First;

                for (int i = 0; i < to && trackNode != null; i++)
                    trackNode = trackNode.Next;

                if (trackNode == null)
                    tracks.AddLast(track);
                else
                    tracks.AddBefore(trackNode, track);

            }

            TrackChanged?.Invoke(this, new EventArgs());
        }

        public PlexTrack[] GetArray()
        {
            return tracks.ToArray();
        }

    }
}
