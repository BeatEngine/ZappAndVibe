using System.ComponentModel.DataAnnotations;

namespace MyUnoApp;

public static class WifiBridge
{

    public static string name = "test1";
    public static string age = "age";
    public static string image = "";
    // Die MainPage abonniert dies, um Daten zu empfangen
    public static Action<string, string>? OnUserDiscovered;

    public static bool active = false;

    public static IWifiService wifiService { get; set; }

    public static void toggleScanning()
    {
        if (active)
        {

        }
        else
        {
            wifiService.StartDiscovery((name, age) =>
            {
                // Leite die Daten vom Android-WLAN-System an den gemeinsamen Code weiter
                WifiBridge.RaiseUserFound(name, age);
            });
            wifiService.StartBroadcasting(name, age);
            active = true;
        }

    }

    // Der Android-Teil ruft dies auf, wenn er jemanden findet
    public static void RaiseUserFound(string name, string age)
    {
        // Wir stellen sicher, dass der Aufruf auf dem UI-Thread landet
        // (Wichtig für Android, da WLAN-Callbacks oft im Hintergrund laufen)
        Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() =>
        {
            OnUserDiscovered?.Invoke(name, age);
        });
    }

    public static void startScanning()
    {

    }
}
