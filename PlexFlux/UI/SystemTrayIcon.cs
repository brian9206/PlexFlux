using System;
using System.Windows.Forms;
using System.Reflection;
using System.Drawing;

namespace PlexFlux.UI
{
    class SystemTrayIcon
    {
        private NotifyIcon notifyIcon;

        public event EventHandler Click;
        public event EventHandler DoubleClick;

        public bool Visible
        {
            get => notifyIcon.Visible;
            set => notifyIcon.Visible = value;
        }

        public SystemTrayIcon()
        {
            notifyIcon = new NotifyIcon
            {
                Text = "PlexFlux",
                Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
                Visible = false
            };

            notifyIcon.Click += NotifyIcon_Click;
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
            notifyIcon.ContextMenu = new ContextMenu();

            // Restore
            var menuItem = new MenuItem();
            menuItem.Text = "Restore";
            menuItem.Click += MenuItem_Restore_Click;
            notifyIcon.ContextMenu.MenuItems.Add(menuItem);

            // -
            notifyIcon.ContextMenu.MenuItems.Add("-");

            // Quit
            menuItem = new MenuItem();
            menuItem.Text = "Quit";
            menuItem.Click += MenuItem_Quit_Click;
            notifyIcon.ContextMenu.MenuItems.Add(menuItem);
        }

        public void ShowBalloonTip(string message)
        {
            Visible = true;
            notifyIcon.ShowBalloonTip(1000, "PlexFlux", message, ToolTipIcon.Info);
        }

        public void Dispose()
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
        }

        private void NotifyIcon_Click(object sender, EventArgs e)
        {
            Click?.Invoke(sender, e);
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            DoubleClick?.Invoke(sender, e);
        }

        private void MenuItem_Restore_Click(object sender, EventArgs e)
        {
            Visible = false;

            var mainWindow = MainWindow.GetInstance();
            mainWindow.RestoreFromSystemTray();
        }

        private void MenuItem_Quit_Click(object sender, EventArgs e)
        {
            var app = (App)System.Windows.Application.Current;
            app.Shutdown();
        }
    }
}
