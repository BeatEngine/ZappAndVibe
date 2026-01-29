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

    private WifiDirectBroadcastReceiver receiver;

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

    private Action<string, string> _onUserFound;
    private bool _isLoopRunning = false;

    public void StartDiscovery(Action<string, string> onUserFound, Action<string, string> onDeviceFound)
    {
        if (_manager == null || _channel == null) return;
        
        _onUserFound = onUserFound;

        // 1. Listener setzen (Nutzt deinen MyTxtRecordListener)
        _manager.SetDnsSdResponseListeners(_channel, 
            new MyServiceResponseListener(), 
            new MyTxtRecordListener(_onUserFound)
        );

        // 2. BroadcastReceiver für System-Events (optional, falls du Device-Namen brauchst)
        receiver = new WifiDirectBroadcastReceiver(_manager, _channel, onDeviceFound);
        var intentFilter = new IntentFilter();
        intentFilter.AddAction(WifiP2pManager.WifiP2pPeersChangedAction);
        _context.RegisterReceiver(receiver, intentFilter);

        // 3. Den Loop starten
        if (!_isLoopRunning)
        {
            _isLoopRunning = true;
            RestartDiscoveryLoop();
        }
    }

    private void RestartDiscoveryLoop()
    {
        if (_manager == null || _channel == null || !_isLoopRunning) return;

        // Erst aufräumen, um "Busy" Fehler zu vermeiden
        _manager.ClearServiceRequests(_channel, new SimpleActionListener("ClearReq", () => {
            
            var request = WifiP2pDnsSdServiceRequest.NewInstance();
            
            _manager.AddServiceRequest(_channel, request, new SimpleActionListener("AddReq", () => {
                
                _manager.DiscoverServices(_channel, new SimpleActionListener("DiscServices", () => {
                    // Scan läuft jetzt für ca. 2 Min. 
                    // Wir starten ihn alle 45 Sek neu, um "frisch" zu bleiben.
                    Task.Delay(45000).ContinueWith(_ => {
                        if(_isLoopRunning) RestartDiscoveryLoop();
                    });
                }, () => {
                    // Falls besetzt (Error 2), schnell neu versuchen
                    Task.Delay(5000).ContinueWith(_ => {
                        if(_isLoopRunning) RestartDiscoveryLoop();
                    });
                }));
            }));
        }));
    }

    /*public void StartBroadcasting(string name, string age)
    {
        //StartBeaconMode(name, age);
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
    }*/
    
    

    /*public void StartDiscovery(Action<string, string> onUserFound, Action<string, string> onDeviceFound)
    {

         if (_manager == null || _channel == null) return;

        // Listener für DNS-SD Antworten
        _manager.SetDnsSdResponseListeners(_channel, new MyServiceResponseListener(), new MyTxtRecordListener(onUserFound));

        

        // 1. Definiere, was passieren soll, wenn der Receiver ein Signal bekommt
        receiver = new WifiDirectBroadcastReceiver(_manager, _channel, onDeviceFound);

        // 2. Registriere den Receiver im Android-System
        var intentFilter = new IntentFilter();
        intentFilter.AddAction(WifiP2pManager.WifiP2pPeersChangedAction);
        _context.RegisterReceiver(receiver, intentFilter);

        _manager.DiscoverPeers(_channel, new SimpleActionListener("PeerDiscovery"));

        _manager.AddServiceRequest(_channel, WifiP2pDnsSdServiceRequest.NewInstance(), new SimpleActionListener("DiscoveryActionListenerRequest"));
        _manager.DiscoverServices(_channel, new SimpleActionListener("DiscoveryActionListener")); 
    }*/
}
