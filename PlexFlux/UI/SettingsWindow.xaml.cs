using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using Microsoft.Win32;
using NAudio.CoreAudioApi;

namespace PlexFlux.UI
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private bool nextTrackApplied;

        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            var runKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
            checkLaunchStartup.IsChecked = runKey.GetValueNames().Contains("PlexFlux");

            var app = (App)Application.Current;
            checkMinimize.IsChecked = app.config.AllowMinimize;

            // device
            foreach (var device in new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                comboDevice.Items.Add(new ComboBoxItem()
                {
                    Tag = device.ID,
                    Content = device.FriendlyName
                });
            }

            comboDevice.SelectedIndex = 0;  // default device

            for (int i = 0; i < comboDevice.Items.Count; i++)
            {
                var deviceID = (string)((ComboBoxItem)comboDevice.Items[i]).Tag;

                if (deviceID == app.config.OutputDeviceID)
                {
                    comboDevice.SelectedIndex = i;
                    break;
                }
            }

            comboOutputMode.SelectedIndex = app.config.IsExclusive ? 1 : 0;

            // bitrate
            for (int i = 0; i < comboBitrate.Items.Count; i++)
            {
                var bitrate = (int)((ComboBoxItem)comboBitrate.Items[i]).Tag;

                if (bitrate == app.config.TranscodeBitrate)
                {
                    comboBitrate.SelectedIndex = i;
                    break;
                }
            }

            UpdateDeskBandInstallationState();

            nextTrackApplied = false;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            using (var runKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (checkLaunchStartup.IsChecked == true)
                    runKey.SetValue("PlexFlux", "\"" + Assembly.GetEntryAssembly().Location + "\" -startup");
                else
                    runKey.DeleteValue("PlexFlux", false);
            }

            var app = (App)Application.Current;
            app.config.AllowMinimize = checkMinimize.IsChecked == true;
            app.config.OutputDeviceID = (string)((ComboBoxItem)comboDevice.SelectedItem).Tag;
            app.config.IsExclusive = comboOutputMode.SelectedIndex == 1;
            app.config.TranscodeBitrate = (int)((ComboBoxItem)comboBitrate.SelectedItem).Tag;
            app.config.Save();

            if (nextTrackApplied)
                MessageBox.Show("Settings will be applied on next track.", "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }

        private void Output_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            nextTrackApplied = true;
        }

        private async void InstallDeskBand_Click(object sender, RoutedEventArgs e)
        {
            var task = new Task(() =>
            {
                var startInfo = new ProcessStartInfo()
                {
                    FileName = Environment.Is64BitOperatingSystem ? "PlexFlux.DeskBand.Installer64.exe" : "PlexFlux.DeskBand.Installer.exe",
                    Arguments = "-install",
                    CreateNoWindow = true
                };

                try
                {
                    var process = Process.Start(startInfo);
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                        throw new Win32Exception("Unknown error");
                }
                catch (Exception exception)
                {
                    MessageBox.Show("Installation failed.\nMessage: " + exception.Message, "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            buttonInstallDeskBand.IsEnabled = false;
            buttonUninstallDeskBand.IsEnabled = false;

            task.Start();
            await task;

            UpdateDeskBandInstallationState();
        }

        private async void UninstallDeskBand_Click(object sender, RoutedEventArgs e)
        {
            var task = new Task(() =>
            {
                var startInfo = new ProcessStartInfo()
                {
                    FileName = Environment.Is64BitOperatingSystem ? "PlexFlux.DeskBand.Installer64.exe" : "PlexFlux.DeskBand.Installer.exe",
                    Arguments = "-uninstall",
                    CreateNoWindow = true
                };

                try
                {
                    var process = Process.Start(startInfo);
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                        throw new Win32Exception("Unknown error");
                }
                catch (Exception exception)
                {
                    MessageBox.Show("Uninstallation failed.\nMessage: " + exception.Message, "PlexFlux", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            buttonInstallDeskBand.IsEnabled = false;
            buttonUninstallDeskBand.IsEnabled = false;

            task.Start();
            await task;

            UpdateDeskBandInstallationState();
        }

        private void UpdateDeskBandInstallationState()
        {
            // deskband installation state
            var installedDeskBand = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32).OpenSubKey("CLSID\\" + new Guid("ADAE1E11-F046-4726-9B7B-F1B78B183877").ToString("B")) != null;
            buttonInstallDeskBand.IsEnabled = !installedDeskBand;
            buttonUninstallDeskBand.IsEnabled = installedDeskBand;
        }
    }
}
