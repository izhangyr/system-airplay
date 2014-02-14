using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using Bonjour;

namespace SystemAirPlay
{
    public partial class App
    {
        private static NotifyIcon _notifyIcon;
        private static List<MenuItem> _topMenuItems;
        private static List<MenuItem> _bottomMenuItems;

        private Bonjour.DNSSDEventManager _eventManager;
        private Bonjour.DNSSDService _service = null;
        private Bonjour.DNSSDService _browser = null;
        private Bonjour.DNSSDService _resolver = null;

        private static List<AirPlayDevice> _detectedAirPlayDevices;
            
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
            if (icon == null) return;
            var iconStream = icon.Stream;

            _topMenuItems = new List<MenuItem>
            {
                new MenuItem("System AirPlay: OFF") {Enabled = false},
                new MenuItem("-"),
            };
            _bottomMenuItems = new List<MenuItem>
            {
                new MenuItem("-"),
                new MenuItem("&Exit", (menuItemSender, args) => Current.Shutdown())
            };

            var menuItems = new List<MenuItem>();

            menuItems.AddRange(_topMenuItems);
            menuItems.Add(new MenuItem("Searching...") { Enabled = false });
            menuItems.AddRange(_bottomMenuItems);

            var contextMenu = new ContextMenu(menuItems.ToArray());

            _notifyIcon = new NotifyIcon { Visible = true, Icon = new Icon(iconStream), ContextMenu = contextMenu };

            _eventManager = new DNSSDEventManager();
            _eventManager.ServiceFound += new _IDNSSDEvents_ServiceFoundEventHandler(this.ServiceFound);
            _eventManager.ServiceLost += new _IDNSSDEvents_ServiceLostEventHandler(this.ServiceLost);
            _eventManager.ServiceResolved += new _IDNSSDEvents_ServiceResolvedEventHandler(this.ServiceResolved);
            _eventManager.OperationFailed += new _IDNSSDEvents_OperationFailedEventHandler(this.OperationFailed);
            _service = new DNSSDService();

            _browser = _service.Browse(0, 0, "_http._tcp", null, _eventManager);

            _detectedAirPlayDevices = new List<AirPlayDevice>();

        }

        private void OperationFailed(DNSSDService service, DNSSDError error)
        {
            Debug.WriteLine("Operation failed: error code: " + error, "Error");
        }

        private void ServiceResolved(DNSSDService service, DNSSDFlags flags, uint ifindex, string fullname, string hostname, ushort port, TXTRecord record)
        {
            var data = new ResolveData
            {
                InterfaceIndex = ifindex,
                FullName = fullname,
                HostName = hostname,
                Port = port,
                TxtRecord = record
            };

            _resolver.Stop();
            _resolver = null;
        }

        private void UpdateContextMenu()
        {
            var menuItems = new List<MenuItem>();

            menuItems.AddRange(_topMenuItems);
            if (!_detectedAirPlayDevices.Any())
            {
                menuItems.Add(new MenuItem("Searching...") { Enabled = false });                
            }
            else
            {
                menuItems.AddRange(_detectedAirPlayDevices.Select(d => new MenuItem(d.Name)));
            }

            menuItems.AddRange(_bottomMenuItems);

            _notifyIcon.ContextMenu.MenuItems.Clear();
            _notifyIcon.ContextMenu.MenuItems.AddRange(menuItems.ToArray());

        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            _notifyIcon.Visible = false;
            _notifyIcon = null;

            if (_resolver != null)
            {
                _resolver.Stop();
            }

            if (_browser != null)
            {
                _browser.Stop();
            }

            if (_service != null)
            {
                _service.Stop();
            }

            _eventManager.ServiceFound -= new _IDNSSDEvents_ServiceFoundEventHandler(this.ServiceFound);
            _eventManager.ServiceLost -= new _IDNSSDEvents_ServiceLostEventHandler(this.ServiceLost);
            _eventManager.ServiceResolved -= new _IDNSSDEvents_ServiceResolvedEventHandler(this.ServiceResolved);
            _eventManager.OperationFailed -= new _IDNSSDEvents_OperationFailedEventHandler(this.OperationFailed);
        }

        private void ServiceLost(DNSSDService dnssdService, DNSSDFlags flags, uint ifindex, string servicename, string regtype, string domain)
        {
            var service = _detectedAirPlayDevices.SingleOrDefault(x => x.Name == servicename);

            if (service != null)
            {
                service.Refs--;

                if (service.Refs == 0)
                {
                    _detectedAirPlayDevices.Remove(service);
                    UpdateContextMenu();
                }
            }
        }

        private void ServiceFound(DNSSDService dnssdService, DNSSDFlags flags, uint ifindex, string servicename, string regtype, string domain)
        {
            var serviceAlreadyExist = _detectedAirPlayDevices.SingleOrDefault(x => x.Name == servicename);

            if (serviceAlreadyExist == null)
            {
                _detectedAirPlayDevices.Add(new AirPlayDevice()
                {
                    Name = servicename,
                    Domain = domain,
                    InterfaceIndex = ifindex,
                    Type = regtype,
                    Refs = 1
                });
            }
            else
            {
                serviceAlreadyExist.Refs++;
            }

            UpdateContextMenu();
        }
    }

    internal class AirPlayDevice : BrowseData
    {
    }

    public class ResolveData
    {
        public uint InterfaceIndex;
        public String FullName;
        public String HostName;
        public int Port;
        public TXTRecord TxtRecord;

        public override String ToString()
        {
            return FullName;
        }
    };

    internal class BrowseData
    {
        public uint InterfaceIndex;
        public String Name;
        public String Type;
        public String Domain;
        public int Refs;

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    };
}
