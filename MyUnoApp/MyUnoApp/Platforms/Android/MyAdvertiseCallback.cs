using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.Text.Style;
using Java.Util;

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
