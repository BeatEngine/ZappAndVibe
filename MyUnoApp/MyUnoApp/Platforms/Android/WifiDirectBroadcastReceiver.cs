using Android.Content;
using Android.Net.Wifi.P2p;

namespace MyUnoApp.Droid;

public class WifiDirectBroadcastReceiver : BroadcastReceiver
{
    private readonly WifiP2pManager _manager;
    private readonly WifiP2pManager.Channel _channel;
    private readonly Action<string, string> onDeviceFound;

    public WifiDirectBroadcastReceiver(WifiP2pManager manager, WifiP2pManager.Channel channel, Action<string, string> onDeviceFound)
    {
        _manager = manager;
        _channel = channel;
        onDeviceFound = onDeviceFound;
    }

    public override void OnReceive(Context? context, Intent? intent)
    {
        string action = intent.Action;

        // Wenn das System meldet, dass sich die Peer-Liste geändert hat
        if (WifiP2pManager.WifiP2pPeersChangedAction == action)
        {
            _manager.RequestPeers(_channel, new MyPeerListListener(devices =>
            {
                foreach (var device in devices)
                {
                    // Wir extrahieren die Daten aus dem Android-Typ 'device'
                    string foundName = device.DeviceName;
                    string foundAddress = device.DeviceAddress; // Oder ein anderer String

                    // Wir geben nur die Strings an das plattformneutrale Interface zurück
                    onDeviceFound?.Invoke(foundName, foundAddress);
                }
            }));
        }
    }
}
