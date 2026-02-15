
using Android.Bluetooth.LE;


namespace MyUnoApp.Platforms.Android;


public class MyAdvertiseCallback : AdvertiseCallback
{

    public MyAdvertiseCallback()
    {
        
    }

    public override void OnStartFailure(AdvertiseFailure errorCode)
    {
        // Fehler 1 = Data Too Large!
        Console.WriteLine($"BLE Advertise Failed: {errorCode}");
    }

}
