using Android.Content;
using Android.Net.Wifi.P2p;
using Android.Net.Wifi.P2p.Nsd;
using Java.Util;

namespace MyUnoApp.Droid;
public class WifiService : Java.Lang.Object, IWifiService
{
    private WifiP2pManager? _manager;
    private WifiP2pManager.Channel? _channel;
    private Context _context;

    public WifiService(Context context)
    {
        _context = context;
        _manager = (WifiP2pManager?)_context.GetSystemService(Context.WifiP2pService);
        if (_manager == null)
        {
            // Wenn das hier null ist, darf kein weiterer Aufruf folgen!
            System.Diagnostics.Debug.WriteLine("WLAN P2P wird auf diesem Gerät nicht unterstützt.");
            return; 
        }
        if (_manager != null)
        {
            _channel = _manager.Initialize(_context, _context.MainLooper, null);
        }
    }

    public void StartBroadcasting(string name, string age)
    {
        if (_manager == null || _channel == null) return;

        // Daten in TXT-Record verpacken (Java Dictionary nutzen)
        var record = new Dictionary<string, string>();
        record.Add("userName", name);
        record.Add("userAge", age);

        var serviceInfo = WifiP2pDnsSdServiceInfo.NewInstance(
            "ZappVibe_" + Guid.NewGuid().ToString().Substring(0, 5), // Eindeutiger Name
            "_presence._tcp",
            record
        );

        _manager.AddLocalService(_channel, serviceInfo, new SimpleActionListener("SendActionListener"));
    }

    public void StartDiscovery(Action<string, string> onUserFound)
    {
        if (_manager == null || _channel == null) return;

        // Listener für DNS-SD Antworten
        _manager.SetDnsSdResponseListeners(_channel, new MyServiceResponseListener(), new MyTxtRecordListener(onUserFound));

        _manager.AddServiceRequest(_channel, WifiP2pDnsSdServiceRequest.NewInstance(), new SimpleActionListener("DiscoveryActionListenerRequest"));
        _manager.DiscoverServices(_channel, new SimpleActionListener("DiscoveryActionListener"));
    }
}
