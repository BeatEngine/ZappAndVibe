using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.OS;
namespace MyUnoApp.Platforms.Android;

public class MyBLEScanCallback : ScanCallback
{

    private Action<string, string> onPeerFound;

    public MyBLEScanCallback(Action<string, string> onFound = null)
    {
        this.onPeerFound = onFound;
    }

    public override void OnScanResult(ScanCallbackType callbackType, ScanResult result)
    {
        var device = result.Device;
        var record = result.ScanRecord;

        if (record == null)
            return;

        // Name
        string name = record.DeviceName;

        // Manufacturer Data
        var manufacturerData = record.GetManufacturerSpecificData(0x1234);
        if (manufacturerData == null)
            return;

        string age = System.Text.UTF8Encoding.UTF8.GetString(manufacturerData);

        Console.WriteLine($"Found: Name={name}, Age={age}");
        this?.onPeerFound.Invoke(name, age);
    }
}

