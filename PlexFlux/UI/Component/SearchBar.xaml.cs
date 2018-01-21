using System;
using System.Windows.Controls;

namespace PlexFlux.UI.Component
{
    /// <summary>
    /// Interaction logic for SearchBar.xaml
    /// </summary>
    public partial class SearchBar : UserControl
    {
        public string Text
        {
            get => textFilter.Text;
            set => textFilter.Text = value;
        }

        public event TextChangedEventHandler TextChanged;

        public SearchBar()
        {
            InitializeComponent();
        }

        private void textFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextChanged?.Invoke(this, e);
        }
    }
}
