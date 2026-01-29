using System.ComponentModel.DataAnnotations;

namespace MyUnoApp;

public static class WifiBridge
{

    public static string name = "test-";
    public static string age = "26";
    public static string image = "";
    // Die MainPage abonniert dies, um Daten zu empfangen
    public static Action<string, string>? OnUserDiscovered;

    public static bool active = false;

    public static initNames()
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

        if (!string.IsNullOrEmpty(deviceName))
        {
            name = deviceName;
        }
        age = "Android " + Android.OS.Build.VERSION.Release;

    }

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
            },
            (device, mac) =>
            {
                // Leite die Daten vom Android-WLAN-System an den gemeinsamen Code weiter
                WifiBridge.RaiseDeviceFound(device, mac);
            }
            );
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

    public static void RaiseDeviceFound(string devicename, string mac)
    {
        // Wir stellen sicher, dass der Aufruf auf dem UI-Thread landet
        // (Wichtig für Android, da WLAN-Callbacks oft im Hintergrund laufen)
        Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() =>
        {
            OnUserDiscovered?.Invoke(devicename, mac); //##################################todo only for debugging
        });
    }

    public static void startScanning()
    {

    }
}
