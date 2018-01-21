using System;
using System.Windows;
using System.Windows.Controls;
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
