using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace PlexFlux.UI.Component
{
    /// <summary>
    /// Interaction logic for VolumeControl.xaml
    /// </summary>
    public partial class VolumeControl : UserControl
    {
        public VolumeControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
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
            var playbackManager = PlaybackManager.GetInstance();
            playbackManager.Volume = (float)(e.NewValue / 100);
        }

        private void PlaybackManager_VolumeChanged(object sender, EventArgs e)
        {
            var playbackManager = PlaybackManager.GetInstance();
            var newValue = playbackManager.Volume * 100.0;

            sliderVolume.Value = newValue;
            textVolume.Text = ((int)newValue).ToString();
        }
    }
}
