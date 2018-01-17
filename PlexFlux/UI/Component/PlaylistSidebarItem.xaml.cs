using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using PlexLib;

namespace PlexFlux.UI.Component
{
    /// <summary>
    /// Interaction logic for PlaylistSidebarItem.xaml
    /// </summary>
    public partial class PlaylistSidebarItem : UserControl
    {
        public static readonly DependencyProperty PlaylistProperty = 
            DependencyProperty.Register("Playlist", typeof(PlexPlaylist), typeof(PlaylistSidebarItem));

        public PlexPlaylist Playlist
        {
            get
            {
                return (PlexPlaylist)GetValue(PlaylistProperty);
            }
            set
            {
                SetValue(PlaylistProperty, value);
            }
        }

        public bool IsButtonEnabled
        {
            get
            {
                return button.IsEnabled;
            }
            set
            {
                button.IsEnabled = value;
            }
        }

        public PlaylistSidebarItem()
        {
            InitializeComponent();
        }

        public event RoutedEventHandler Click;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, e);
        }
    }
}
