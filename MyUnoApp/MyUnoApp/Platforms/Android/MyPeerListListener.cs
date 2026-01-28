using Android.Net.Wifi.P2p;
using System.Collections.Generic;
using System.Diagnostics;

namespace MyUnoApp.Droid;

public class MyPeerListListener : Java.Lang.Object, WifiP2pManager.IPeerListListener
{
    private readonly Action<IEnumerable<WifiP2pDevice>> _onPeersFound;

    public MyPeerListListener(Action<IEnumerable<WifiP2pDevice>> onPeersFound)
    {
        _onPeersFound = onPeersFound;
    }

    public void OnPeersAvailable(WifiP2pDeviceList? peers)
    {
        if (peers != null && peers.DeviceList.Count > 0)
        {
            Debug.WriteLine($"[P2P] {peers.DeviceList.Count} Geräte gefunden.");
            _onPeersFound?.Invoke(peers.DeviceList);
        }
        else
        {
            Debug.WriteLine("[P2P] Keine Geräte in der Nähe gefunden.");
        }
    }
}
