using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.OS;
using Android.Runtime;
namespace MyUnoApp.Platforms.Android;

public class MyBLEScanCallback : ScanCallback
{

    private Action<int, string, string> onPeerFound;

    public MyBLEScanCallback(Action<int, string, string> onFound = null)
    {
        this.onPeerFound = onFound;
    }

    public MyBLEScanCallback(IntPtr handle, JniHandleOwnership transfer)
        : base(handle, transfer)
    {
        onPeerFound = ((BLEAdvertiser)BLEAdvertiser.GetInstance()).lastAction;
    }

    public override void OnScanResult(ScanCallbackType callbackType, ScanResult result)
    {
        var device = result.Device;
        var record = result.ScanRecord;

        if (record == null)
            return;

        // Name
        string name;

        string age = "null";

        // Manufacturer Data
        var manufacturerData = record.GetManufacturerSpecificData(0x1234);
        if (manufacturerData == null)
            return;

        int userId = BitConverter.ToInt32(manufacturerData, 0);

        string text = System.Text.Encoding.GetEncoding("Windows-1252").GetString(manufacturerData[4..]);
        name = text;
        int i = text.IndexOf('|');
        if (i != -1)
        {
            name = text.Substring(0, i);
            age = text.Substring(i + 1);
        }
        Console.WriteLine($"Found: Name={name}, Age={age}");
        this?.onPeerFound.Invoke(userId, name, age);
    }
}

