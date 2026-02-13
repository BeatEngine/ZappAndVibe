using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.Runtime;
using System.IO.Hashing;

namespace MyUnoApp.Platforms.Android;

using System;
using System.Collections.Generic;
using Android;
using global::Android.Bluetooth;
using global::Android.Util;

public class MyGattCallback : BluetoothGattCallback
{
    private readonly Action<int, byte[]> _onImageReceived;
    private readonly List<byte> _imageBuffer = new List<byte>();

    private int _expectedSize = -1;
    private int _userId;
    private byte[] _checksum;
    private int _expectedUser;

    public static bool receiving = false;
    public MyGattCallback(Action<int, byte[]> onImageReceived, int originalUserId)
    {
        _onImageReceived = onImageReceived;
        _expectedUser = originalUserId;
    }

    public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState)
    {
        if (status != GattStatus.Success)
        {
            receiving = false;
            Console.WriteLine("GATT closed");
            gatt.Close();
            return;
        }
        if (newState == ProfileState.Connected)
        {
            //BLEAdvertiser.GetInstance().StopBleScan();
            // 1. WICHTIG: MTU erhöhen für Bilddaten
            gatt.RequestMtu(517);
        }
        if (newState == ProfileState.Disconnected)
        {
            receiving = false;
            Console.WriteLine("GATT disconnected and closing...");
            gatt.Close();
            //((BLEAdvertiser)BLEAdvertiser.GetInstance()).ReStartBleScan();
        }
    }

    public override void OnMtuChanged(BluetoothGatt gatt, int mtu, GattStatus status)
    {
        // 2. Erst wenn MTU bestätigt wurde, Services suchen
        if (status == GattStatus.Success)
        {
            Task.Delay(50).ContinueWith(_ => gatt.DiscoverServices());
        }
    }

    public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status)
    {
        if (status == GattStatus.Success)
        {
            var serviceUuid = Java.Util.UUID.FromString("43badf3b-e2ed-4e7e-9508-d0943b010ef8");
            var charUuid = Java.Util.UUID.FromString("ade72fb5-fca9-45d3-9fce-451ec5da5b55");

            var characteristic = gatt.GetService(serviceUuid)?.GetCharacteristic(charUuid);

            if (characteristic != null)
            {
                // 1. Lokal am Android-Stack die Benachrichtigung erlauben
                gatt.SetCharacteristicNotification(characteristic, true);

                // 2. Den CCCD-Descriptor beschreiben (dem Server sagen: "Sende jetzt!")
                // Das ist der Standard-UUID für Konfigurationen
                var configUuid = Java.Util.UUID.FromString("00002902-0000-1000-8000-00805f9b34fb");
                var descriptor = characteristic.GetDescriptor(configUuid);

                if (descriptor != null)
                {
                    descriptor.SetValue(BluetoothGattDescriptor.EnableNotificationValue.ToArray());
                    gatt.WriteDescriptor(descriptor);
                }
            }
        }
    }

    public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
    {
        receiving = true;
        byte[] data = characteristic.GetValue();
        if (data == null || data.Length == 0)
        {
            Console.WriteLine($"GATT OnCharacteristicChanged {data}");
            MyBLEScanCallback.getInstance().ResetFoundUserDevice(_expectedUser);
            gatt.Disconnect();
            receiving = false;
            return;
        }
        bool offset = false;

        if(_expectedSize == -1 && data.Length < 12)
        {
            Console.WriteLine("GATT First packet too small: " + data.Length);
            receiving = false;
            gatt.Disconnect();
            MyBLEScanCallback.getInstance().ResetFoundUserDevice(_expectedUser);
        }
        
        if (_expectedSize == -1 && data.Length >= 12)
        {
            _expectedSize = BitConverter.ToInt32(data, 0);
            _userId = BitConverter.ToInt32(data[4..], 0);
            _checksum = new byte[] { 0, 0, 0, 0 };
            Array.Copy(data, 8, _checksum, 0, 4);
            offset = true;
            if (_expectedSize > 0)
            {
                _imageBuffer.Capacity = _expectedSize;
            }
        }

        if (_expectedSize > 0)
        {

            if (offset)
            {
                _imageBuffer.AddRange(data[12..]);
            }
            else
            {
                _imageBuffer.AddRange(data);
            }

            // Prüfen, ob das Bild vollständig ist
            if (IsImageComplete())
            {
                byte[] finalImage = _imageBuffer.ToArray();
                _imageBuffer.Clear();

                byte[] checksum = XxHash32.Hash(finalImage);
                // Checking data integrity of whole message
                if (checksum.SequenceEqual(_checksum))
                {
                    receiving = false;
                    _onImageReceived?.Invoke(_userId, finalImage);
                    Console.WriteLine("GATT Disconnecting after receiving complete");
                }
                else
                {
                    receiving = false;
                    // Trigger the resend when the userId is found next time
                    Console.WriteLine("GATT Disconnecting after receiving incomplete");
                    MyBLEScanCallback.getInstance().ResetFoundUserDevice(_userId);
                }
                gatt.Disconnect();
                _expectedSize = -1;
                
            }
        }
        else
        {
            Console.WriteLine("GATT receiving size 0 ");
            receiving = false;
            gatt.Disconnect();
            MyBLEScanCallback.getInstance().ResetFoundUserDevice(_userId);
        }
    }

    private bool IsImageComplete()
    {
        return _imageBuffer.Count >= _expectedSize;
    }
}
