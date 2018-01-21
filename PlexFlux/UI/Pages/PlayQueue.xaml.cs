using GongSolutions.Wpf.DragDrop;
using PlexLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PlexFlux.UI.Pages
{
    /// <summary>
    /// Interaction logic for PlayQueue.xaml
    /// </summary>
    public partial class PlayQueue : Page, IDropTarget
    {
        public ObservableCollection<PlexTrack> Tracks
        {
            get;
            private set;
        }

        private ContextMenu contextMenu;
        private bool skipNextCurrentChanged = false;

        public PlayQueue(ContextMenu contextMenu)
        {
            Tracks = new ObservableCollection<PlexTrack>();
            InitializeComponent();
            this.contextMenu = contextMenu;
        }

        #region "IDropTarget implementation"
        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            var sourceItem = dropInfo.Data as PlexTrack;
            var targetItem = dropInfo.TargetItem as PlexTrack;

            if (sourceItem != null && targetItem != null && dropInfo.DragInfo.VisualSource == dropInfo.VisualTarget)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = DragDropEffects.Move;
            }
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            var source = (Component.TrackButton)VisualTreeHelper.GetChild(dropInfo.DragInfo.VisualSourceItem, 0);
            int sourceIdx = ItemsControlHelper.FindIndexByItemChild(panelTracks, source);
            int targetIdx = dropInfo.InsertIndex;

            if (sourceIdx == targetIdx || sourceIdx == -1 || targetIdx == -1)
                return;

            var playQueue = PlayQueueManager.GetInstance();
            playQueue.Rearrange(sourceIdx, targetIdx);
        }
        #endregion

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            var playQueue = PlayQueueManager.GetInstance();
            playQueue.TrackChanged += PlayQueue_TrackChanged;
            playQueue.CurrentChanged += PlayQueue_CurrentChanged;

            PlayQueue_TrackChanged(this, e);
            PlayQueue_CurrentChanged(this, e);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            var playQueue = PlayQueueManager.GetInstance();
            playQueue.TrackChanged -= PlayQueue_TrackChanged;
            playQueue.CurrentChanged -= PlayQueue_CurrentChanged;
        }

        private void buttonMore_Click(object sender, RoutedEventArgs e)
        {
            contextMenu.PlacementTarget = buttonMore;
            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        private void TrackButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Component.TrackButton)sender;

            var index = ItemsControlHelper.FindIndexByItemChild(panelTracks, button);
            if (index == -1)
                return;

            skipNextCurrentChanged = true;

            var playQueue = PlayQueueManager.GetInstance();
            playQueue.Play(index);

            var playback = PlaybackManager.GetInstance();
            playback.Play(button.Track);
        }

        private void TrackButton_DeleteClick(object sender, RoutedEventArgs e)
        {
            var index = ItemsControlHelper.FindIndexByItemChild(panelTracks, sender as DependencyObject);
            if (index == -1)
                return;

            var playQueue = PlayQueueManager.GetInstance();
            playQueue.Remove(index);
        }

        private void PlayQueue_TrackChanged(object sender, EventArgs e)
        {
            var playQueue = PlayQueueManager.GetInstance();
            Tracks.FromArray(playQueue.GetArray());
        }

        private void PlayQueue_CurrentChanged(object sender, EventArgs e)
        {
            if (skipNextCurrentChanged)
            {
                skipNextCurrentChanged = false;
                return;
            }

            var app = (App)Application.Current;

            Task.Factory.StartNew(() =>
            {
                var playQueue = PlayQueueManager.GetInstance();

                if (playQueue.Current < 0)
                    return;

                var scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(panelTracks, 0);
                scrollViewer.ScrollToVerticalOffset(playQueue.Current - 1);

            }, CancellationToken.None, TaskCreationOptions.None, app.uiContext);
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            var button = (Component.TrackButton)((ContextMenu)((MenuItem)e.Source).Parent).PlacementTarget;
            TrackButton_Click(button, e);
        }

        private void PlayAfter_Click(object sender, RoutedEventArgs e)
        {
            var track = (PlexTrack)((MenuItem)e.Source).DataContext;

            var upcoming = UpcomingManager.GetInstance();
            upcoming.Push(track);
        }

        private void RemoveFromPlayQueue_Click(object sender, RoutedEventArgs e)
        {
            var button = (Component.TrackButton)((ContextMenu)((MenuItem)e.Source).Parent).PlacementTarget;
            TrackButton_DeleteClick(button, e);
        }

    }
}
