using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlexLib;

namespace PlexFlux
{
    class PlayQueueManager
    {
        #region "Singleton"
        private static PlayQueueManager instance = null;

        public static PlayQueueManager GetInstance()
        {
            if (instance == null)
                instance = new PlayQueueManager();

            return instance;
        }
        #endregion

        private List<PlexTrack> tracks;
        private LinkedList<int> playedTracksIndex;
        private int current = -1;

        public int Current
        {
            get
            {
                return current;
            }
            set
            {
                if (value >= tracks.Count)
                    throw new IndexOutOfRangeException();

                current = value;

                CurrentChanged?.Invoke(this, new EventArgs());
            }
        }

        // this is untrustable. use PlaybackManager.GetInstance().Track instead
        public PlexTrack CurrentTrack
        {
            get
            {
                if (current >= tracks.Count)
                    return null;

                return tracks[Current];
            }
        }

        public bool HasPrevious
        {
            get
            {
                return (current - 1 < tracks.Count && current - 1 >= 0);
            }
        }

        public bool HasNext
        {
            get
            {
                return (current + 1 < tracks.Count);
            }
        }

        public bool HasTrack
        {
            get
            {
                return tracks.Count > 0;
            }
        }

        public int Count
        {
            get
            {
                return tracks.Count;
            }
        }

        public event EventHandler TrackChanged;
        public event EventHandler CurrentChanged;

        private PlayQueueManager()
        {
            tracks = new List<PlexTrack>();
            playedTracksIndex = new LinkedList<int>();
        }

        public PlexTrack FromTracks(System.ComponentModel.ICollectionView tracks, int current = -1)
        {
            if (current < 0)
                current = 0;

            lock (tracks)
            {
                this.tracks.Clear();

                foreach (var track in tracks)
                    this.tracks.Add(track as PlexTrack);
            }

            ResetPlayedIndexes();
            var currentTrack = Play(current);

            TrackChanged?.Invoke(this, new EventArgs());

            return currentTrack;
        }

        public PlexTrack FromTracks(PlexTrack[] tracks, int current = -1)
        {
            if (current < 0)
                current = 0;

            lock (tracks)
            {
                this.tracks.Clear();

                foreach (var track in tracks)
                    this.tracks.Add(track); 
            }

            ResetPlayedIndexes();
            var currentTrack = Play(current);

            TrackChanged?.Invoke(this, new EventArgs());

            return currentTrack;
        }

        public void Rearrange(int from, int to)
        {
            lock (tracks)
            {
                // rearrange
                var track = tracks[from];
                tracks.RemoveAt(from);
                tracks.Insert(to, track);

                // reset
                ResetPlayedIndexes();

                // fix current
                if (from <= Current || to <= Current)
                {
                    if (from == Current)
                        current = to;
                    else if (to > Current)
                        current--;
                    else if (to <= Current)
                        current++;
                }
            }

            TrackChanged?.Invoke(this, new EventArgs());
        }

        public void Add(PlexTrack track)
        {
            lock (tracks)
                    tracks.Add(track);

            TrackChanged?.Invoke(this, new EventArgs());
        }

        public void AddRange(PlexTrack[] items)
        {
            lock (tracks)
            {
                foreach (var track in items)
                    tracks.Add(track);
            }
                
            TrackChanged?.Invoke(this, new EventArgs());
        }


        public void Remove(int index)
        {
            lock (tracks)
                tracks.RemoveAt(index);

            if (current <= index)
                current--;

            ResetPlayedIndexes();

            TrackChanged?.Invoke(this, new EventArgs());
        }

        public void RemoveAll()
        {
            lock (tracks)
            {
                tracks.Clear();
            }

            Current = -1;

            ResetPlayedIndexes();

            TrackChanged?.Invoke(this, new EventArgs());
        }

        public PlexTrack[] GetArray()
        {
            return tracks.ToArray();
        }

        public void ResetPlayedIndexes()
        {
            lock (playedTracksIndex)
                playedTracksIndex.Clear();
        }

        public void AddPlayedIndex(int index = -1)
        {
            if (index < 0)
                index = Current;

            if (playedTracksIndex.Contains(index))
                return;

            lock (playedTracksIndex)
                playedTracksIndex.AddLast(index);
        }

        public int GetNextTrackIndex(bool isShuffle)
        {
            if (isShuffle)
            {
                // build a list of unplayed index
                var randomList = new List<int>();

                for (int i = 0; i < tracks.Count; i++)
                {
                    if (playedTracksIndex.Contains(i))
                        continue;

                    randomList.Add(i);
                }

                var rng = new Random();

                if (randomList.Count == 0)
                    return -1;

                return randomList[rng.Next(0, randomList.Count - 1)];
            }
            else
            {
                var index = Current + 1;
                if (index >= tracks.Count)
                    return -1;

                return Current + 1;
            }
        }

        public PlexTrack Play(int index)
        {
            Current = index;

            var track = CurrentTrack;

            if (track == null)
                return null;

            AddPlayedIndex();
            
            return track;
        }
    }
}
