using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
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
using System.Threading;
using PlexLib;
using System.Windows.Media.Animation;
using GongSolutions.Wpf.DragDrop;

namespace PlexFlux.UI.Component
{
    /// <summary>
    /// Interaction logic for NowPlaying.xaml
    /// </summary>
    public partial class NowPlaying : UserControl, IDropTarget
    {
        public static readonly DependencyProperty TrackProperty =
            DependencyProperty.Register("Track", typeof(PlexTrack), typeof(NowPlaying));

        public PlexTrack Track
        {
            get
            {
                return (PlexTrack)GetValue(TrackProperty);
            }
            private set
            {
                SetValue(TrackProperty, value);
            }
        }

        public ObservableCollection<PlexTrack> Upcomings
        {
            get;
            private set;
        }

        private CancellationTokenSource artworkResizeTokenSource;

        public NowPlaying()
        {
            Upcomings = new ObservableCollection<PlexTrack>();
            InitializeComponent();
            artworkResizeTokenSource = new CancellationTokenSource();
        }

        private void LoadArtwork()
        {
            if (Track == null || Track.Thumb == null)
            {
                imageArtwork.Source = null;
                return;
            }

            var app = (App)Application.Current;

            BitmapImage bitmap = new BitmapImage();
            bitmap.DownloadCompleted += Bitmap_DownloadCompleted;

            bitmap.BeginInit();
            bitmap.UriSource = app.plexClient.GetPhotoTranscodeUrl(Track.Thumb, (int)panelArtwork.ActualWidth, (int)panelArtwork.ActualHeight);
            bitmap.CacheOption = BitmapCacheOption.OnDemand;
            bitmap.EndInit();

            imageArtwork.Source = bitmap;
        }

        private void Bitmap_DownloadCompleted(object sender, EventArgs e)
        {
            imageArtwork.BeginStoryboard((Storyboard)FindResource("FadeIn"));
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var playbackControl = PlaybackManager.GetInstance();
            playbackControl.StartPlaying += PlaybackControl_StartPlaying;
            playbackControl.PlaybackTick += PlaybackControl_PlaybackTick;

            var upcomings = UpcomingManager.GetInstance();
            upcomings.TrackChanged += Upcomings_TrackChanged;
            Upcomings_TrackChanged(this, e);

            Track = playbackControl.Track;
            LoadArtwork();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            var playbackControl = PlaybackManager.GetInstance();
            playbackControl.StartPlaying -= PlaybackControl_StartPlaying;

            var upcomings = UpcomingManager.GetInstance();
            upcomings.TrackChanged -= Upcomings_TrackChanged;

            artworkResizeTokenSource.Cancel();
        }

        private void PlaybackControl_PlaybackTick(object sender, EventArgs e)
        {
            var app = (App)Application.Current;

            Task.Factory.StartNew(() =>
            {
                if (Track == null)
                {
                    sliderPosition.Value = 0;
                    sliderPosition.Maximum = 1;
                    textPosition.Text = "00:00";
                    textPositionRemaining.Text = "00:00";
                }
                else
                {
                    var position = PlaybackManager.GetInstance().Position;
                    var duration = Track.Duration;
                    var remaining = duration - position;

                    sliderPosition.Value = position;
                    sliderPosition.Maximum = duration;

                    int positionMinute = (int)Math.Floor(position / 60.0);
                    int positionSecond = (int)(position - positionMinute * 60.0);
                    textPosition.Text = positionMinute.ToString().PadLeft(2, '0') + ":" + positionSecond.ToString().PadLeft(2, '0');

                    int remainingMinute = (int)Math.Floor(remaining / 60.0);
                    int remainingSecond = (int)(remaining - remainingMinute * 60.0);
                    textPositionRemaining.Text = remainingMinute.ToString().PadLeft(2, '0') + ":" + remainingSecond.ToString().PadLeft(2, '0');
                }

            }, CancellationToken.None, TaskCreationOptions.None, app.uiContext);
        }

        private void PlaybackControl_StartPlaying(object sender, EventArgs e)
        {
            var app = (App)Application.Current;
            Task.Factory.StartNew(() =>
            {
                Track = sender as PlexTrack;
                LoadArtwork();

            }, CancellationToken.None, TaskCreationOptions.None, app.uiContext);
        }

        private void Upcomings_TrackChanged(object sender, EventArgs e)
        {
            var app = (App)Application.Current;
            Task.Factory.StartNew(() =>
            {
                var upcomings = UpcomingManager.GetInstance();
                Upcomings.FromArray(upcomings.GetArray());

                panelTracksContainer.Visibility = Upcomings.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

                var scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(panelTracks, 0);
                scrollViewer.ScrollToVerticalOffset(0);

            }, CancellationToken.None, TaskCreationOptions.None, app.uiContext);
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (imageArtwork.Source == null)
                return;

            artworkResizeTokenSource.Cancel();
            artworkResizeTokenSource = new CancellationTokenSource();

            Task.Factory.StartNew(() =>
            {
                var cancelToken = artworkResizeTokenSource.Token;

                Thread.Sleep(500);

                if (cancelToken.IsCancellationRequested)
                    return;

                // reload artwork in UI thread
                var app = (App)Application.Current;
                Task.Factory.StartNew(LoadArtwork, CancellationToken.None, TaskCreationOptions.None, app.uiContext);

            }, artworkResizeTokenSource.Token);
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset + (e.Delta <= 0 ? 1 : -1));
            e.Handled = true;
        }

        private void TrackButton_DeleteClick(object sender, RoutedEventArgs e)
        {
            var index = -1;

            for (int i = 0; i < panelTracks.Items.Count; i++)
            {
                var dependencyObject = panelTracks.ItemContainerGenerator.ContainerFromIndex(i);
                var item = (Component.TrackButton)VisualTreeHelper.GetChild(dependencyObject, 0);

                if (sender == item)
                {
                    index = i;
                    break;
                }
            }

            var upcomings = UpcomingManager.GetInstance();
            upcomings.Remove(index);
        }

        private void RemoveFromUpcomings_Click(object sender, RoutedEventArgs e)
        {
            var button = (Component.TrackButton)((ContextMenu)((MenuItem)e.Source).Parent).PlacementTarget;
            TrackButton_DeleteClick(button, e);
        }

        private void TrackButton_Click(object sender, RoutedEventArgs e)
        {
            var index = -1;

            for (int i = 0; i < panelTracks.Items.Count; i++)
            {
                var dependencyObject = panelTracks.ItemContainerGenerator.ContainerFromIndex(i);
                var item = (Component.TrackButton)VisualTreeHelper.GetChild(dependencyObject, 0);

                if (sender == item)
                {
                    index = i;
                    break;
                }
            }

            var upcomings = UpcomingManager.GetInstance();
            var track = upcomings.Remove(index);

            var playback = PlaybackManager.GetInstance();
            playback.Play(track);
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            var button = (Component.TrackButton)((ContextMenu)((MenuItem)e.Source).Parent).PlacementTarget;
            TrackButton_Click(button, e);
        }

        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            PlexTrack sourceItem = dropInfo.Data as PlexTrack;
            PlexTrack targetItem = dropInfo.TargetItem as PlexTrack;

            if (sourceItem != null && targetItem != null && dropInfo.DragInfo.VisualSource == dropInfo.VisualTarget)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = DragDropEffects.Move;
            }
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            var source = (Component.TrackButton)VisualTreeHelper.GetChild(dropInfo.DragInfo.VisualSourceItem, 0);
            var target = (Component.TrackButton)VisualTreeHelper.GetChild(dropInfo.VisualTargetItem, 0);

            int sourceIdx = -1;
            int targetIdx = -1;

            for (int i = 0; i < panelTracks.Items.Count; i++)
            {
                var dependencyObject = panelTracks.ItemContainerGenerator.ContainerFromIndex(i);
                var item = (Component.TrackButton)VisualTreeHelper.GetChild(dependencyObject, 0);

                if (source == item)
                    sourceIdx = i;

                if (target == item)
                    targetIdx = i;

                if (sourceIdx >= 0 && targetIdx >= 0)
                    break;
            }

            if (sourceIdx < 0 && targetIdx < 0)
                return;

            var upcomings = UpcomingManager.GetInstance();
            upcomings.Rearrange(sourceIdx, targetIdx);
        }
    }
}
