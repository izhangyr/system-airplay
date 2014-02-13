using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace SystemAirPlay
{
    public partial class App
    {
        private static NotifyIcon _notifyIcon;

        [STAThread]
        public static void Main()
        {
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            var icon = GetResourceStream(new Uri("pack://application:,,,/system-airplay.ico", UriKind.RelativeOrAbsolute));
            if (icon != null)
            {
                var iconStream = icon.Stream;

                var menuItems = new List<MenuItem>
                    {
                        new MenuItem("Exit", (menuItemSender, args) => Current.Shutdown())
                    }.ToArray();

                var contextMenu = new ContextMenu(menuItems);

                _notifyIcon = new NotifyIcon { Visible = true, Icon = new Icon(iconStream), ContextMenu = contextMenu };
            }
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            _notifyIcon.Visible = false;
            _notifyIcon = null;
        }
    }
}
