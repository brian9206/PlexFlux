using PlexLib;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace PlexFlux.UI
{
    /// <summary>
    /// Interaction logic for NotificationWindow.xaml
    /// </summary>
    public partial class NotificationWindow : Window
    {
        #region "Manager"
        private static NotificationWindow notificationWindow = null;

        public static void Notify(PlexTrack track)
        {
            var app = (App)System.Windows.Application.Current;

            Task.Factory.StartNew(() =>
            {
                if (notificationWindow != null)
                    notificationWindow.Close();

                // do not show notification if the mini player is present
                if (PlaybackControlWindow.IsInstantiated())
                    return;

                notificationWindow = new NotificationWindow(track);
                notificationWindow.Show();

            }, CancellationToken.None, TaskCreationOptions.None, app.uiContext);
        }

        public static void HideAll()
        {
            var app = (App)System.Windows.Application.Current;

            Task.Factory.StartNew(() =>
            {
                if (notificationWindow != null)
                    notificationWindow.Close();

            }, CancellationToken.None, TaskCreationOptions.None, app.uiContext);
        }
        #endregion

        private CancellationTokenSource tokenSource;

        private NotificationWindow(PlexTrack track)
        {
            InitializeComponent();
            tokenSource = new CancellationTokenSource();

            Title = track.Title + " - " + track.Artist.Title;

            // load artwork
            var app = (App)System.Windows.Application.Current;

            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = app.plexClient.GetPhotoTranscodeUrl(track.Thumb, 50, 50);
            bitmap.CacheOption = BitmapCacheOption.OnDemand;
            bitmap.EndInit();

            imageArtwork.Source = bitmap;

            var playback = PlaybackManager.GetInstance();
            playback.PlaybackTick += Playback_PlaybackTick;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.EnableBlur();

            // animation
            Top = Screen.PrimaryScreen.WorkingArea.Height - Height;
            Left = Screen.PrimaryScreen.WorkingArea.Width;

            BeginAnimation(LeftProperty, new DoubleAnimation()
            {
                From = Left,
                To = Screen.PrimaryScreen.WorkingArea.Width - Width,
                Duration = TimeSpan.FromMilliseconds(150),
                FillBehavior = FillBehavior.HoldEnd,
                EasingFunction = new ExponentialEase()
                {
                    EasingMode = EasingMode.EaseOut
                }
            });

            BeginAnimation(OpacityProperty, new DoubleAnimation()
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(150),
                FillBehavior = FillBehavior.HoldEnd,
                EasingFunction = new ExponentialEase()
                {
                    EasingMode = EasingMode.EaseOut
                }
            });
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            Close();
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // unregister event
            var playback = PlaybackManager.GetInstance();
            playback.PlaybackTick -= Playback_PlaybackTick;

            tokenSource.Cancel();
            notificationWindow = null;
        }

        private void Playback_PlaybackTick(object sender, EventArgs e)
        {
            var app = (App)System.Windows.Application.Current;
            var playback = PlaybackManager.GetInstance();

            Task.Factory.StartNew(() => 
            {
                sliderPosition.Value = playback.Position;
                sliderPosition.Maximum = playback.Track == null ? 1 : playback.Track.Duration;

            }, CancellationToken.None, TaskCreationOptions.None, app.uiContext);
        }

        private void Title_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // stop animation
            textTitle.BeginAnimation(Canvas.LeftProperty, null);

            // WORKAROUND: should be panelTitle.ActualWidth (it returns 247? idk why)
            if (textTitle.ActualWidth > panelTitle.Width)
            {
                var duration = textTitle.ActualWidth / 30;
                var animation = new DoubleAnimation
                {
                    From = panelTitle.Width,
                    To = -textTitle.ActualWidth,
                    FillBehavior = FillBehavior.Stop,
                    Duration = new Duration(TimeSpan.FromSeconds(duration < 10 ? 10 : duration))
                };

                animation.Completed += Animation_Completed;
                textTitle.BeginAnimation(Canvas.LeftProperty, animation);
            }
            else
            {
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(TimeSpan.FromSeconds(10));

                    var app = (App)System.Windows.Application.Current;

                    Task.Factory.StartNew(CloseWithAnimation,
                        CancellationToken.None, TaskCreationOptions.None, app.uiContext);

                }, tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
        }

        private void Animation_Completed(object sender, EventArgs e)
        {
            CloseWithAnimation();
        }

        private void CloseWithAnimation()
        {
            BeginAnimation(LeftProperty, new DoubleAnimation()
            {
                From = Left,
                To = Left + Width,
                Duration = TimeSpan.FromMilliseconds(150),
                FillBehavior = FillBehavior.HoldEnd,
                EasingFunction = new ExponentialEase()
                {
                    EasingMode = EasingMode.EaseOut
                }
            });

            BeginAnimation(OpacityProperty, new DoubleAnimation()
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(150),
                FillBehavior = FillBehavior.HoldEnd,
                EasingFunction = new ExponentialEase()
                {
                    EasingMode = EasingMode.EaseOut
                }
            });

            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(150));

                var app = (App)System.Windows.Application.Current;

                Task.Factory.StartNew(Close,
                    CancellationToken.None, TaskCreationOptions.None, app.uiContext);

            }, tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
    }
}
