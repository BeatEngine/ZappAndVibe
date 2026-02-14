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
using Java.Util;
namespace MyUnoApp.Platforms.Android;

public class MyBLEScanCallback : ScanCallback
{

    private Action<int, string, string, Dictionary<string, object>> onPeerFound;

    private Action<int, byte[]> onPeerReceivedImage;

    private Dictionary<int, BluetoothDevice> userDevices = new Dictionary<int, BluetoothDevice>();

    private Dictionary<int, string> PendingUserNotifications = new Dictionary<int, string>();
    private HashSet<int> PendingUserPictures = new HashSet<int>();

    private static MyBLEScanCallback instance;
    public static MyBLEScanCallback getInstance()
    {
        return instance; 
    }

    public void AddNotification(int targetUser,  string notificationBody)
    {
        if(PendingUserNotifications.TryAdd(targetUser, notificationBody))
        {
            Console.WriteLine($"Added notification for {targetUser} {notificationBody}");
        }
    }

    private void ResetFoundUserDevice(int userId)
    {
        userDevices.Remove(userId);
    }

    public void CommitReceivedPicture(int userId)
    {
        PendingUserPictures.Remove(userId);
    }

    private Context _context;

    private BluetoothGatt gatt;

    public MyBLEScanCallback(Action<int, string, string, Dictionary<string, object>> onFound, Action<int, byte[]> onReceivedData, Context context)
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
        int signalStrength = result.Rssi;
        // Name
        string name;

        string age = "null";

        // Manufacturer Data
        var manufacturerData = record.GetManufacturerSpecificData(0x1234);
        if (manufacturerData == null)
            return;

        // Little-Endian
        int userId = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(manufacturerData);

        if (result.Device != null && userDevices.TryAdd(userId, result.Device))
        {
            PendingUserPictures.Add(userId);
        }

        try
        {
            if (result.Device != null && !MyGattCallback.receiving && PendingUserPictures.Contains(userId))
            {
                MyGattCallback.receiving = true;
                Log.Info(this.Class.SimpleName,$"Request userdata for:{userId}");
                gatt = device.ConnectGatt(_context, false, new MyGattCallback((uid, img) =>
                {
                    MyBLEScanCallback.getInstance().CommitReceivedPicture(userId);
                    onPeerReceivedImage.Invoke(uid, img);

                }, userId));
            }
        }
        catch (Exception e)
        {
            userDevices.Remove(userId);
            Log.Info(this.Class.SimpleName,$"GATT Connection failed: "+e.Message);
        }

        if(result.Device != null && PendingUserNotifications.ContainsKey(userId) && !MyGattCallback.receiving)
        {
            try
            {
                // Sent pending notifications (e.g. ice-breakers)
                int thisUserId = MyGattServerProvider.getInstance().userId;
                string body = PendingUserNotifications[userId];
            MyGattCallback.receiving = true;
            Log.Info(this.Class.SimpleName, $"Notification for:{userId}");
            gatt = device.ConnectGatt(_context, false, new MyGattCallback(null, thisUserId, body, (userId) =>
                {
                    Console.WriteLine($"Successfully notificated {userId}");
                    //If successfull sent remove pending notification
                    MyBLEScanCallback.getInstance().PendingUserNotifications.Remove(userId);
                }
            ));
            }
            catch (Exception e)
            {
                Log.Info(this.Class.SimpleName, $"GATT Connection notification failed: " + e.Message);
            }
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
        Dictionary<string, object> parameters = new Dictionary<string, object>();
        parameters.TryAdd("signal", signalStrength);
        this?.onPeerFound.Invoke(userId, name, age, parameters);
    }
}

