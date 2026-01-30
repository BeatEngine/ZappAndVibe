

namespace MyUnoApp;

public interface IWifiService
{
    void StartDiscovery(Action<string, string> onUserFound, Action<string, string> onDeviceFound);
    void StopDiscovery();
    void StartBroadcasting(string name, string age);

    void StopBroadcasting();

    (string, string) initNames();
}








    