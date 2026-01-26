// Diese Klasse verarbeitet die eigentlichen Daten (Name/Alter)
using Android.Net.Wifi.P2p;

namespace MyUnoApp.Droid;

public class MyTxtRecordListener : Java.Lang.Object, WifiP2pManager.IDnsSdTxtRecordListener
{
    private readonly Action<string, string> _onUserFound;
    public MyTxtRecordListener(Action<string, string> onUserFound) => _onUserFound = onUserFound;

    public void OnDnsSdTxtRecordAvailable(string fullDomainName, System.Collections.Generic.IDictionary<string, string>? txtRecordMap, Android.Net.Wifi.P2p.WifiP2pDevice? srcDevice)
    {
        if (txtRecordMap != null && txtRecordMap.ContainsKey("userName"))
        {
            var name = txtRecordMap["userName"];
            var age = txtRecordMap.ContainsKey("userAge") ? txtRecordMap["userAge"] : "Unbekannt";
            _onUserFound?.Invoke(name, age);
        }
    }
}

