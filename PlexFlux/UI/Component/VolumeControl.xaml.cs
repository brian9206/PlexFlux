using System;
using System.Windows;
using System.Windows.Controls;

namespace PlexFlux.UI.Component
{
    /// <summary>
    /// Interaction logic for VolumeControl.xaml
    /// </summary>
    public partial class VolumeControl : UserControl
    {
        private bool doNotTriggerEvent;

        public VolumeControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!(Application.Current is App))
                return;

            var playbackManager = PlaybackManager.GetInstance();
            playbackManager.VolumeChanged += PlaybackManager_VolumeChanged;

            // update UI
            PlaybackManager_VolumeChanged(playbackManager, new EventArgs());
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            var playbackManager = PlaybackManager.GetInstance();
            playbackManager.VolumeChanged -= PlaybackManager_VolumeChanged;
        }

        private void sliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (doNotTriggerEvent)
            {
                doNotTriggerEvent = false;
                return;
            }

            var playbackManager = PlaybackManager.GetInstance();
            playbackManager.Volume = (float)(e.NewValue / 100.0f);
        }

        private void PlaybackManager_VolumeChanged(object sender, EventArgs e)
        {
            doNotTriggerEvent = true;

            var playbackManager = PlaybackManager.GetInstance();
            var newValue = playbackManager.Volume * 100.0;

            sliderVolume.Value = newValue;
            textVolume.Text = ((int)newValue).ToString();
        }
    }
}
