using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using Android.Provider;
using Android.OS;
using Android.Locations;
using MyUnoApp.Droid;
using Java.Util;
using static Java.Util.Jar.Attributes;
using Android.Bluetooth;

namespace MyUnoApp.Platforms.Android;

public class AndroidUtil : Java.Lang.Object, IAndroidUtil
{

    private Context _context;

    public AndroidUtil(Context context)
    {
        _context = context;
    }

    public bool IsLocationServiceEnabled()
    {
        var locationManager = (LocationManager)_context.GetSystemService(Context.LocationService);

        // Prüft, ob GPS oder Netzwerk-Standort aktiv ist
        return locationManager.IsProviderEnabled(LocationManager.GpsProvider) ||
               locationManager.IsProviderEnabled(LocationManager.NetworkProvider);
    }

    public bool IsBluethoothServiceEnabled()
    {
        var bluetoothManager = (BluetoothManager)_context.GetSystemService(Context.BluetoothService);
        var adapter = bluetoothManager?.Adapter;

        // Prüfen, ob der Adapter existiert und eingeschaltet ist
        return adapter != null && adapter.IsEnabled;
    }

    public void OpenLocationSettings()
    {
        var intent = new Intent(Settings.ActionLocationSourceSettings);
        intent.SetFlags(ActivityFlags.NewTask);
        _context.StartActivity(intent);
    }

    public void OpenBluethoothSettings()
    {
        Intent enableBtIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
        enableBtIntent.SetFlags(ActivityFlags.NewTask);
        _context.StartActivity(enableBtIntent);
    }

    public bool checkRequirementsBLEDiscovery()
    {
        if(!IsLocationServiceEnabled())
        {
            OpenLocationSettings();
            return false;
        }
        if (!IsBluethoothServiceEnabled())
        {
            OpenBluethoothSettings();
            return false;
        }

        return true;
    }
    public (string, string) initNames()
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
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
        string age = "" + Build.VERSION.Release;
        Console.WriteLine("This name is " + name + " " + age);
        return (name, age);
    }
}
