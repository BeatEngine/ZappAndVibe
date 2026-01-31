using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Java.Util;
using ScanMode = Android.Bluetooth.LE.ScanMode;
using BluetoothLeAdvertiser = Android.Bluetooth.LE.BluetoothLeAdvertiser;
using BluetoothLeScanner = Android.Bluetooth.LE.BluetoothLeScanner;
using System.Security.Cryptography.X509Certificates;

namespace MyUnoApp.Platforms.Android;

public class BLEAdvertiser : Java.Lang.Object, IBleService
{
    private Context _context;
    private static IBleService instance;

    public Action<string, string> lastAction;
    public static IBleService GetInstance()
    {
        return instance;
    }
    public BLEAdvertiser(Context context)
    {
        _context = context;
        instance = this;
    }

    private BluetoothManager bluetoothManager;
    private BluetoothAdapter adapter;
    private BluetoothLeAdvertiser advertiser;
    private BluetoothLeScanner scanner;
    private MyAdvertiseCallback callback;
    private MyBLEScanCallback scanCallback;

    public void StartBleAdvertiser(string name, string age)
    {
        bluetoothManager = (BluetoothManager)_context.GetSystemService(Context.BluetoothService);
        adapter = bluetoothManager.Adapter;

        //adapter.SetName(name);

        advertiser = adapter.BluetoothLeAdvertiser;

        callback = new MyAdvertiseCallback();

        var settings = new AdvertiseSettings.Builder()
            .SetAdvertiseMode(AdvertiseMode.LowLatency)
            .SetTxPowerLevel(AdvertiseTx.PowerHigh)
            .SetConnectable(true)
            .Build();

        // Manufacturer Data: Age = 25
        string text = name + "|" + age;
        if(text.Length > 24)
        {
            text = text.Substring(0, 24);
        }
        byte[] ageData = System.Text.Encoding.UTF8.GetBytes(text);
        
        var data = new AdvertiseData.Builder()
            .AddManufacturerData(0x1234, ageData) // 0x1234 = eigene App-ID
            .SetIncludeDeviceName(false)
            .Build();

        Console.WriteLine("Starting BLE advertiser");
        advertiser.StartAdvertising(settings, data, callback);
    }

    public void StartBleScan(Action<string, string> onPeerFound)
    {
        scanner = adapter.BluetoothLeScanner;
        lastAction = onPeerFound;
        var settings = new ScanSettings.Builder()
            .SetScanMode(ScanMode.LowLatency)
            .Build();
        scanCallback = new MyBLEScanCallback(lastAction);
        Console.WriteLine("Starting BLE scanner");
        scanner.StartScan(null, settings, scanCallback);
    }

    void IBleService.StopBleAdvertiser()
    {
        if (advertiser == null)
            return;

        advertiser.StopAdvertising(callback);
        callback.Dispose();
        callback = null;
        advertiser = null;
        Console.WriteLine("Stopped BLE advertiser");
    }

    void IBleService.StopBleScan()
    {
        if (scanner == null )
            return;

        scanner.StopScan(scanCallback);

        scanCallback.Dispose();
        scanCallback = null;
        scanner = null;
        Console.WriteLine("Stopped BLE scanner");
    }
}
