using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Java.Util;
using static Android.Hardware.Camera;
using BluetoothLeAdvertiser = Android.Bluetooth.LE.BluetoothLeAdvertiser;
using BluetoothLeScanner = Android.Bluetooth.LE.BluetoothLeScanner;
using ScanMode = Android.Bluetooth.LE.ScanMode;

namespace MyUnoApp.Platforms.Android;

public class BLEAdvertiser : Java.Lang.Object, IBleService
{
    private Context _context;
    private static IBleService instance;

    public Action<int, string, string, Dictionary<string, object>> lastAction;
    public Action<int, byte[]> lastActionData;
    public Action<int, string> lastActionNotification;

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
    private MyGattServerProvider gattProvider;

    private byte[] tmp = null;

    public void StartBleAdvertiser(int userId, string name, string age, Action<int, string> onNotificationReceived)
    {
        lastActionNotification = onNotificationReceived;
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
        if(text.Length > 20)
        {
            text = text.Substring(0, 20);
        }
        byte[] ageData = System.Text.Encoding.GetEncoding("Windows-1252").GetBytes(text);

        Span<byte> userIdSpan = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(userIdSpan, userId);
        byte[] combined = new byte[userIdSpan.Length + ageData.Length];
        userIdSpan.CopyTo(combined.AsSpan());
        ageData.CopyTo(combined.AsSpan(userIdSpan.Length));

        var data = new AdvertiseData.Builder()
            .AddManufacturerData(0x1234, combined) // 0x1234 = eigene App-ID
            .SetIncludeDeviceName(false)
            .Build();

        Console.WriteLine("Starting BLE advertiser");
        advertiser.StartAdvertising(settings, data, callback);
        gattProvider = new MyGattServerProvider(_context, userId);
        gattProvider.OnNotificationReceived = lastActionNotification;
        if (tmp != null)
        {
            gattProvider.fullImageData = tmp;
        }
        gattProvider.StartServer();
    }

    public void SetImageData(byte[] data)
    {
        tmp = data;
        if (gattProvider != null)
        {
            gattProvider.fullImageData = data;
        }
    }

    public void SendNotification(int userId, string jsonNotification)
    {
        if (scanCallback != null)
        {
            scanCallback.AddNotification(userId, jsonNotification);
        }
    }

    public void StartBleScan(Action<int, string, string, Dictionary<string, object>> onPeerFound, Action<int, byte[]> onReceivedData)
    {
        scanner = adapter.BluetoothLeScanner;
        lastAction = onPeerFound;
        lastActionData = onReceivedData;
        var settings = new ScanSettings.Builder()
            .SetScanMode(ScanMode.LowLatency)
            .Build();
        scanCallback = new MyBLEScanCallback(lastAction, lastActionData, _context);
        Console.WriteLine("Starting BLE scanner");
        scanner.StartScan(null, settings, scanCallback);
    }

    public void ReStartBleScan()
    {
        StartBleScan(lastAction, lastActionData);
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
       gattProvider.StopServer();
        gattProvider = null;
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

    public Context? getContext()
    {
        return _context;
    }
}
