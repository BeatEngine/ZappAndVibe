using Android.Content;
using Android.Net.Wifi.P2p;
using Android.Net.Wifi.P2p.Nsd;
using Java.Util;
using static Java.Util.Jar.Attributes;

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

        // Start Service for unsing WIFI in Background
        Intent intent = new Intent(context, typeof(ZappVibeDiscoveryService));
        context.StartForegroundService(intent);

    }


    public (string, string) initNames()
    {
        // In deinem Android-Projekt oder via #if __ANDROID__
        var deviceName = Android.Provider.Settings.Global.GetString(
            _context.ContentResolver,
            "device_name"
        );

        // Falls das null ist (je nach Hersteller), nimm den Bluetooth-Namen:
        if (string.IsNullOrEmpty(deviceName))
        {
            deviceName = Android.Provider.Settings.Secure.GetString(
                _context.ContentResolver,
                "bluetooth_name"
            );
        }
        string name = "";

        if (!string.IsNullOrEmpty(deviceName))
        {
            name = deviceName;
        }
        string age = "Android " + Android.OS.Build.VERSION.Release;
        Console.WriteLine("This name is " + name + " " + age);
        return (name, age);
    }

    private Action<string, string> _onUserFound;
    private bool _isLoopRunning = false;

    public void StartDiscovery(Action<string, string> onUserFound, Action<string, string> onDeviceFound)
    {
        if (_manager == null || _channel == null) return;
        ZappVibeDiscoveryService.service.ActivateBackgroundWIFI();

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
            Console.WriteLine("Starting discovery");
            _isLoopRunning = true;
            RestartDiscoveryLoop();
        }
    }

    private void RestartDiscoveryLoop()
    {
        if (_manager == null || _channel == null || !_isLoopRunning) return;

        // Erst aufräumen, um "Busy" Fehler zu vermeiden

        Action anyAction = () =>
        {
            var request = WifiP2pDnsSdServiceRequest.NewInstance();

            _manager.AddServiceRequest(_channel, request, new SimpleActionListener("AddReq", () =>
            {
                _manager.DiscoverServices(_channel, new SimpleActionListener("DiscServices", () =>
                {
                    // Scan läuft jetzt für ca. 2 Min. 
                    // Wir starten ihn alle 45 Sek neu, um "frisch" zu bleiben.
                    Console.WriteLine("Discovery started");
                    Task.Delay(30000).ContinueWith(_ =>
                    {
                        if (_isLoopRunning) RestartDiscoveryLoop();
                    });
                }, () =>
                {
                    // Falls besetzt (Error 2), schnell neu versuchen
                    Console.WriteLine("Discovery busy");
                    Task.Delay(3000).ContinueWith(_ =>
                    {
                        if (_isLoopRunning) RestartDiscoveryLoop();
                    });
                }));
            }));
        };

        SimpleActionListener clearReq = new SimpleActionListener("ClearReq", anyAction, anyAction);
        _manager.ClearServiceRequests(_channel, clearReq);
    }

    private bool _isBroadcasting = false;

    public async void StartBroadcastingLoop(string name, string age)
    {
        _isBroadcasting = true;

        if (_isBroadcasting && _manager != null)
        {
            //if (_manager == null || _channel == null) break;

            Action anyAction = () =>
            {
                // 2. Neue Daten verpacken
                var record = new Dictionary<string, string> {
                    { "userName", name },
                    { "userAge", age },
                    { "timestamp", DateTime.Now.Ticks.ToString() } // Optional: Zeitstempel für Frische
                };

                var serviceInfo = WifiP2pDnsSdServiceInfo.NewInstance(
                    "ZappVibe_" + name,
                    "_presence._tcp",
                    record
                );

                // 3. Service neu registrieren
                _manager.AddLocalService(_channel, serviceInfo, new SimpleActionListener("AddLocal", () => { Console.WriteLine("Started broadcast service"); },
                    () => {
                    Task.Delay(3000).ContinueWith(_ =>
                    { if (_isBroadcasting) { StartBroadcastingLoop(name, age); 
                        } 
                    });
                    }
                    ));
            };

            // 1. Alten Service entfernen, um Platz für das Update zu machen
            _manager.ClearLocalServices(_channel, new SimpleActionListener("ClearLocal", anyAction, anyAction));

            // 2 Sekunden warten bis zum nächsten Refresh (falls nötig)
            // Hinweis: 10-30 Sekunden sind für die Batterie & Stabilität besser.
            //await Task.Delay(60000);
        }
    }

    public void StopBroadcasting()
    {
        _isBroadcasting = false;
        _manager?.ClearLocalServices(_channel, new SimpleActionListener("Stop"));
    }
    public void StartBroadcasting(string name, string age)
    {
        if (!_isBroadcasting)
        {
            StartBroadcastingLoop(name, age);
        }
    }
    
    public void StopDiscovery()
    {
        ZappVibeDiscoveryService.service.ReleaseBackgroundWIFI();
        // 1. Loop-Flag setzen, damit keine neuen Tasks gestartet werden
        _isLoopRunning = false;

        if (_manager != null && _channel != null)
        {
            // 2. Alle registrierten Service-Requests entfernen
            _manager.ClearServiceRequests(_channel, new SimpleActionListener("ClearReqs"));
        }

        // 3. BroadcastReceiver abmelden (Wichtig!)
        if (receiver != null)
        {
            try 
            {
                _context.UnregisterReceiver(receiver);
                receiver = null;
            }
            catch (Java.Lang.IllegalArgumentException) 
            {
                // Receiver war bereits abgemeldet oder nie registriert
            }
        }
    }

}
