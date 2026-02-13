using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
namespace MyUnoApp.Platforms.Android;

public class MyBLEScanCallback : ScanCallback
{

    private Action<int, string, string> onPeerFound;

    private Action<int, byte[]> onPeerReceivedImage;

    private Dictionary<int, BluetoothDevice> userDevices = new Dictionary<int, BluetoothDevice>();

    private static MyBLEScanCallback instance;
    public static MyBLEScanCallback getInstance()
    {
        return instance; 
    }

    public void ResetFoundUserDevice(int userId)
    {
        userDevices.Remove(userId);
    }

    private Context _context;

    private BluetoothGatt gatt;

    public MyBLEScanCallback(Action<int, string, string> onFound, Action<int, byte[]> onReceivedData, Context context)
    {
        this.onPeerFound = onFound;
        this.onPeerReceivedImage = onReceivedData;
        _context = context;
        instance = this;
    }

    public MyBLEScanCallback(IntPtr handle, JniHandleOwnership transfer)
        : base(handle, transfer)
    {
        onPeerFound = ((BLEAdvertiser)BLEAdvertiser.GetInstance()).lastAction;
        onPeerReceivedImage = ((BLEAdvertiser)BLEAdvertiser.GetInstance()).lastActionData;
        _context = ((BLEAdvertiser)BLEAdvertiser.GetInstance()).getContext();
        instance = this;
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

        try
        {
            if (result.Device != null && !MyGattCallback.receiving)
            {
                if (userDevices.TryAdd(userId, result.Device))
                {
                    MyGattCallback.receiving = true;
                    Log.Info(this.Class.SimpleName,$"Request userdata for:{userId}");
                    gatt = device.ConnectGatt(_context, false, new MyGattCallback(onPeerReceivedImage, userId));
                }
            }
        }
        catch (Exception e)
        {
            userDevices.Remove(userId);
            Log.Info(this.Class.SimpleName,$"GATT Connection failed: "+e.Message);
        }

        string text = System.Text.Encoding.GetEncoding("Windows-1252").GetString(manufacturerData[4..]);
        name = text;
        int i = text.IndexOf('|');
        if (i != -1)
        {
            name = text.Substring(0, i);
            age = text.Substring(i + 1);
        }
        //Log.Info(this.Class.SimpleName,$"Found: Name={name}, Age={age}");
        this?.onPeerFound.Invoke(userId, name, age);
    }
}

