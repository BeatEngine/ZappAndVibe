

namespace MyUnoApp;

public interface IWifiService
{
    void StartBroadcasting(string name, string age);
    void StartDiscovery(Action<string, string> onUserFound);
}








    