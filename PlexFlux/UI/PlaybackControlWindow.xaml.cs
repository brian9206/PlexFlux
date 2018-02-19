using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
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
    /// Interaction logic for MiniPlayerWindow.xaml
    /// </summary>
    public partial class PlaybackControlWindow : Window
    {
        #region "Singleton"
        private static PlaybackControlWindow instance = null;

        public static PlaybackControlWindow GetInstance()
        {
            if (instance == null)
                instance = new PlaybackControlWindow();

            return instance;
        }

        public static bool IsInstantiated()
        {
            return instance != null;
        }
        #endregion

        #region "Win32"
        private enum AccentState
        {
            ACCENT_DISABLED = 1,
            ACCENT_ENABLE_GRADIENT = 0,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_INVALID_STATE = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        private enum WindowCompositionAttribute
        {
            // ...
            WCA_ACCENT_POLICY = 19
            // ...
        }

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
        #endregion

        private bool doNotTriggerEvent;

        private PlaybackControlWindow()
        {
            InitializeComponent();

            // hide all notification
            NotificationWindow.HideAll();

            // event handler
            var playback = PlaybackManager.GetInstance();
            playback.PlaybackStateChanged += Playback_PlaybackStateChanged;
            playback.PlaybackTick += Playback_PlaybackTick;
            playback.VolumeChanged += Playback_VolumeChanged;

            // invoke all event handlers
            Playback_PlaybackStateChanged(this, new EventArgs());
            Playback_PlaybackTick(this, new EventArgs());
            Playback_VolumeChanged(this, new EventArgs());
        }

        // use Loaded event because we need the handle
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.EnableBlur();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            // animation
            Top = Screen.PrimaryScreen.WorkingArea.Height;
            Left = Screen.PrimaryScreen.WorkingArea.Width - Width;

            BeginAnimation(TopProperty, new DoubleAnimation()
            {
                From = Top,
                To = Screen.PrimaryScreen.WorkingArea.Height - Height,
                Duration = TimeSpan.FromMilliseconds(150),
                FillBehavior = FillBehavior.HoldEnd,
                EasingFunction = new ExponentialEase()
                {
                    EasingMode = EasingMode.EaseOut
                }
            });
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // unregister event handler
            var playback = PlaybackManager.GetInstance();
            playback.PlaybackStateChanged -= Playback_PlaybackStateChanged;
            playback.PlaybackTick -= Playback_PlaybackTick;
            playback.VolumeChanged -= Playback_VolumeChanged;

            // .net will destroy this instance
            instance = null;
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);

            // hide window when user clicked something else on their desktop
            Close();
        }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            var mainWindow = MainWindow.GetInstance();
            mainWindow.RestoreFromSystemTray();
        }

        // PlaybackManager event handlers
        private void Playback_PlaybackStateChanged(object sender, EventArgs e)
        {
            var app = (App)System.Windows.Application.Current;

            Task.Factory.StartNew(() =>
            {
                var playback = PlaybackManager.GetInstance();

                if (playback.Track == null)
                {
                    Title = string.Empty;

                    sliderPosition.Value = 0;
                    sliderPosition.Maximum = 1;
                    sliderPosition.IsEnabled = false;

                    imageArtwork.Source = null;

                    imageArtwork.Visibility = Visibility.Collapsed;
                    imageArtworkNone.Visibility = Visibility.Collapsed;
                }
                else
                {
                    Title = playback.Track.Title + " - " + playback.Track.Artist.Title;

                    // load artwork
                    if (playback.Track.Thumb == null)
                    {
                        imageArtwork.Visibility = Visibility.Collapsed;
                        imageArtworkNone.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = app.plexClient.GetPhotoTranscodeUrl(playback.Track.Thumb, 50, 50);
                        bitmap.CacheOption = BitmapCacheOption.OnDemand;
                        bitmap.EndInit();

                        imageArtwork.Source = bitmap;

                        imageArtwork.Visibility = Visibility.Visible;
                        imageArtworkNone.Visibility = Visibility.Collapsed;
                    }

                }

                buttonPlay.Visibility = playback.PlaybackState == NAudio.Wave.PlaybackState.Playing ? Visibility.Collapsed : Visibility.Visible;
                buttonPause.Visibility = playback.PlaybackState == NAudio.Wave.PlaybackState.Playing ? Visibility.Visible : Visibility.Collapsed;

            }, CancellationToken.None, TaskCreationOptions.None, app.uiContext);
        }

        private void Playback_PlaybackTick(object sender, EventArgs e)
        {
            var app = (App)System.Windows.Application.Current;

            Task.Factory.StartNew(() =>
            {
                var playback = PlaybackManager.GetInstance();

                doNotTriggerEvent = true;

                if (playback.Track != null)
                {
                    sliderPosition.Value = playback.Position;
                    sliderPosition.Maximum = playback.Track.Duration;
                    sliderPosition.IsEnabled = true;
                }
                else
                {
                    sliderPosition.Value = 0;
                    sliderPosition.Maximum = 1;
                    sliderPosition.IsEnabled = false;
                }

                doNotTriggerEvent = false;

            }, CancellationToken.None, TaskCreationOptions.None, app.uiContext);
        }

        private void Playback_VolumeChanged(object sender, EventArgs e)
        {
            doNotTriggerEvent = true;
            sliderVolume.Value = Math.Ceiling(PlaybackManager.GetInstance().Volume * 100);
            doNotTriggerEvent = false;
        }

        // controls event handlers
        private void Title_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // stop animation
            textTitle.BeginAnimation(Canvas.LeftProperty, null);

            if (textTitle.ActualWidth > panelTitle.ActualWidth)
            {
                textTitle.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation
                {
                    From = panelTitle.ActualWidth,
                    To = -textTitle.ActualWidth,
                    RepeatBehavior = RepeatBehavior.Forever,
                    FillBehavior = FillBehavior.Stop,
                    Duration = new Duration(TimeSpan.FromSeconds(textTitle.ActualWidth / 15))
                });
            }
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = MainWindow.GetInstance();
            var app = (App)System.Windows.Application.Current;

            Task.Factory.StartNew(() =>
            {
                if (MediaCommands.PreviousTrack.CanExecute(null, mainWindow))
                    MediaCommands.PreviousTrack.Execute(null, mainWindow);

            }, CancellationToken.None, TaskCreationOptions.None, app.uiContext);
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = MainWindow.GetInstance();
            var app = (App)System.Windows.Application.Current;

            Task.Factory.StartNew(() =>
            {
                if (MediaCommands.NextTrack.CanExecute(null, mainWindow))
                    MediaCommands.NextTrack.Execute(null, mainWindow);

            }, CancellationToken.None, TaskCreationOptions.None, app.uiContext);
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = MainWindow.GetInstance();
            var app = (App)System.Windows.Application.Current;

            Task.Factory.StartNew(() =>
            {
                if (MediaCommands.Play.CanExecute(null, mainWindow))
                    MediaCommands.Play.Execute(null, mainWindow);

            }, CancellationToken.None, TaskCreationOptions.None, app.uiContext);
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = MainWindow.GetInstance();
            var app = (App)System.Windows.Application.Current;

            Task.Factory.StartNew(() =>
            {
                if (MediaCommands.Pause.CanExecute(null, mainWindow))
                    MediaCommands.Pause.Execute(null, mainWindow);

            }, CancellationToken.None, TaskCreationOptions.None, app.uiContext);
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = MainWindow.GetInstance();
            var app = (App)System.Windows.Application.Current;

            Task.Factory.StartNew(() =>
            {
                if (MediaCommands.Stop.CanExecute(null, mainWindow))
                    MediaCommands.Stop.Execute(null, mainWindow);

            }, CancellationToken.None, TaskCreationOptions.None, app.uiContext);
        }

        private void Volume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (doNotTriggerEvent)
                return;

            var app = (App)System.Windows.Application.Current;
            var playback = PlaybackManager.GetInstance();

            Task.Factory.StartNew(() =>
            {
                playback.Volume = (float)e.NewValue / 100.0f;

            }, CancellationToken.None, TaskCreationOptions.None, app.uiContext);
        }

        private void Position_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (doNotTriggerEvent)
                return;

            var app = (App)System.Windows.Application.Current;
            var playback = PlaybackManager.GetInstance();

            Task.Factory.StartNew(() =>
            {
                playback.Position = (long)e.NewValue;

            }, CancellationToken.None, TaskCreationOptions.None, app.uiContext);
        }
    }
}
