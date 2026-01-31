using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using Android.Provider;
using Android.OS;
using MyUnoApp.Droid;
using Java.Util;
using static Java.Util.Jar.Attributes;

namespace MyUnoApp.Platforms.Android;

public class AndroidUtil : Java.Lang.Object, IAndroidUtil
{

    private Context _context;

    public AndroidUtil(Context context)
    {
        _context = context;
    }
    public (string, string) initNames()
    {
        // In deinem Android-Projekt oder via #if __ANDROID__
        var deviceName = Settings.Global.GetString(
            _context.ContentResolver,
            "device_name"
        );

        // Falls das null ist (je nach Hersteller), nimm den Bluetooth-Namen:
        if (string.IsNullOrEmpty(deviceName))
        {
            deviceName = Settings.Secure.GetString(
                _context.ContentResolver,
                "bluetooth_name"
            );
        }
        string name = "";

        if (!string.IsNullOrEmpty(deviceName))
        {
            name = deviceName;
        }
        string age = "Android " + Build.VERSION.Release;
        Console.WriteLine("This name is " + name + " " + age);
        return (name, age);
    }
}
