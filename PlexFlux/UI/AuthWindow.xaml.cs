using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PlexFlux.UI
{
    /// <summary>
    /// Interaction logic for AuthWindow.xaml
    /// </summary>
    public partial class AuthWindow : Window
    {
        public AuthWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            spinner.Visibility = Visibility.Hidden;
            txtUsername.Focus();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // disable closing window if we are processing thing
            if (!IsEnabled)
                e.Cancel = true;
        }

        private async void buttonSignIn_Click(object sender, RoutedEventArgs e)
        {
            if (txtUsername.Text.Length == 0 || txtPassword.Password.Length == 0)
            {
                MessageBox.Show("You must provide both username and password.", "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var app = (App)Application.Current;

            IsEnabled = false;
            spinner.Visibility = Visibility.Visible;

            bool failed = false;

            try
            {
                await app.myPlexClient.SignIn(txtUsername.Text, txtPassword.Password);
            }
            catch (WebException exception)
            {
                if (exception.Status == WebExceptionStatus.ProtocolError && ((HttpWebResponse)exception.Response).StatusCode == HttpStatusCode.Unauthorized)
                {
                    MessageBox.Show("Username or password is invalid.\nPlease try again.", "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                else
                {
                    MessageBox.Show("Plex.tv is currently unavailable.\nPlease try again later.", "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }

                failed = true;
            }
            catch (XmlException)
            {
                MessageBox.Show("Plex.tv is responding invalid information.\nPlease try again later.", "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                failed = true;
            }
            catch (Exception)
            {
                MessageBox.Show("An unknown error has occurred.", "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                failed = true;
            }
            finally
            {
                // reset UI
                IsEnabled = true;
                spinner.Visibility = Visibility.Hidden;

                txtUsername.Text = string.Empty;
                txtPassword.Password = string.Empty;
            }

            if (failed)
            {
                txtUsername.Focus();
                return;
            }

            // succeed
            DialogResult = true;
            Close();
        }

        
    }
}
