using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
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
using CSDeskBand;
using CSDeskBand.Wpf;

namespace PlexFlux.DeskBand
{
    /// <summary>
    /// Interaction logic for DeskBand.xaml
    /// </summary>
    [ComVisible(true)]
    [Guid("ADAE1E11-F046-4726-9B7B-F1B78B183877")]
    [CSDeskBandRegistration(Name = "PlexFlux")]
    public partial class DeskBand
    {
        #region "Singleton"
        private static DeskBand instance = null;

        public static DeskBand GetInstance()
        {
            return instance;
        }
        #endregion

        #region "PInvoke"
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
        #endregion

        public DeskBand()
        {
            // increase max connection here as we cannot set it in app.config
            ServicePointManager.DefaultConnectionLimit = 65535;

            instance = this;

            InitializeComponent();

            Options.TopRow = true;
            Options.NoMargins = true;
            Options.Fixed = true;
            Options.Horizontal.Width = Options.MinHorizontal.Width = Options.MaxHorizontal.Width = 240;
            Options.Horizontal.Height = Options.MinHorizontal.Height = Options.MaxHorizontal.Height = 40;

            Options.Vertical.Width = Options.MinVertical.Width = Options.MaxVertical.Width = 0;
            Options.Vertical.Height = Options.MinVertical.Height = Options.MaxVertical.Height = 0;

            ShowDeskBand(false);
        }

        public IntPtr GetDeskBandHandle()
        {
            deskband.GetWindow(out IntPtr hWnd);
            return hWnd;
        }

        public void ShowDeskBand(bool visible)
        {
            var hWnd = GetDeskBandHandle();
            ShowWindowAsync(hWnd, visible ? SW_SHOW : SW_HIDE);
        }
    }
}
