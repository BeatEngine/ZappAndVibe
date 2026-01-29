

namespace MyUnoApp;

public interface IWifiService
{
    void StartDiscovery(Action<string, string> onUserFound, Action<string, string> onDeviceFound);

    (string, string) initNames();
}








    