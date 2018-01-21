using System;
using System.Windows;
using System.Windows.Controls;
using PlexLib;

namespace PlexFlux.UI.Component
{
    /// <summary>
    /// Interaction logic for LibrarySidebarItem.xaml
    /// </summary>
    public partial class LibrarySidebarItem : UserControl
    {
        public static readonly DependencyProperty LibraryProperty =
            DependencyProperty.Register("Library", typeof(PlexLibrary), typeof(LibrarySidebarItem));

        public PlexLibrary Library
        {
            get
            {
                return (PlexLibrary)GetValue(LibraryProperty);
            }
            set
            {
                SetValue(LibraryProperty, value);
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

        public LibrarySidebarItem()
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
