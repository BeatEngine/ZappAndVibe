using CoreBluetooth;
using Foundation;
using System.IO.Hashing;
using System.Buffers.Binary;

namespace MyUnoApp.Platforms.iOS;

public class IosGattDelegate : CBPeripheralDelegate
{
    private readonly Action<int, byte[]> _onImageReceived;
    private readonly Action<int> _onNotificationSucceeded;
    private readonly List<byte> _imageBuffer = new();

    private int _expectedSize = -1;
    private int _userId;
    private byte[] _checksum;
    private readonly int _expectedUser;

    private byte[] _notificationRequest;
    private byte[] _notificationRequestChecksum;

    public static bool Receiving = false;

    private readonly CBUUID ServiceUuid = CBUUID.FromString("43badf3b-e2ed-4e7e-9508-d0943b010ef8");
    private readonly CBUUID ImageCharUuid = CBUUID.FromString("ade72fb5-fca9-45d3-9fce-451ec5da5b55");
    private readonly CBUUID JsonCharUuid = CBUUID.FromString("9e509813-bb73-463a-9371-dafacb86f577");

    public IosGattDelegate(Action<int, byte[]> onImageReceived, int originalUserId, string notificationRequests = null, Action<int> onNotificationSucceeded = null)
    {
        _onImageReceived = onImageReceived;
        _expectedUser = originalUserId;
        _onNotificationSucceeded = onNotificationSucceeded;

        if (notificationRequests != null)
        {
            _notificationRequest = System.Text.Encoding.UTF8.GetBytes(notificationRequests);
            _notificationRequestChecksum = XxHash32.Hash(_notificationRequest);
        }
    }

    // Equivalent to OnServicesDiscovered
    public override void DiscoveredService(CBPeripheral peripheral, NSError error)
    {
        if (error != null) return;

        foreach (var service in peripheral.Services)
        {
            if (service.UUID.Equals(ServiceUuid))
            {
                // iOS requires explicit characteristic discovery
                peripheral.DiscoverCharacteristics(new CBUUID[] { ImageCharUuid, JsonCharUuid }, service);
            }
        }
    }

    // Corrected method name for iOS CoreBluetooth
    public override void DiscoveredCharacteristics(CBPeripheral peripheral, CBService service, NSError? error)
    {
        if (error != null)
        {
            Console.WriteLine($"Error discovering characteristics: {error.LocalizedDescription}");
            return;
        }

        foreach (var characteristic in service.Characteristics)
        {
            // Logic for Image Request (UUID: ade72fb5-fca9-45d3-9fce-451ec5da5b55)
            if (characteristic.UUID.Equals(ImageCharUuid) && _notificationRequest == null)
            {
                // Equivalent to gatt.SetCharacteristicNotification + writing CCCD in Android
                peripheral.SetNotifyValue(true, characteristic);
            }

            // Logic for JSON Notification Transfer (UUID: 9e509813-bb73-463a-9371-dafacb86f577)
            else if (characteristic.UUID.Equals(JsonCharUuid) && _notificationRequest != null)
            {
                // Start listening for the 'received: true' response
                peripheral.SetNotifyValue(true, characteristic);

                // Construct the payload
                var payload = new List<byte>();
                byte[] uid = new byte[4];
                System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(uid, _expectedUser);

                payload.AddRange(_notificationRequestChecksum);
                payload.AddRange(uid);
                payload.AddRange(_notificationRequest);

                var data = NSData.FromArray(payload.ToArray());

                // Write the JSON to the peripheral
                peripheral.WriteValue(data, characteristic, CBCharacteristicWriteType.WithResponse);
            }
        }
    }

    // Equivalent to OnCharacteristicChanged
    public override void UpdatedCharacterteristicValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSError error)
    {
        if (error != null) return;

        byte[] data = characteristic.Value.ToArray();

        if (characteristic.UUID.Equals(ImageCharUuid))
        {
            ProcessIncomingData(peripheral, data, false);
        }
        else if (characteristic.UUID.Equals(JsonCharUuid))
        {
            ProcessIncomingData(peripheral, data, true);
        }
    }

    private void ProcessIncomingData(CBPeripheral peripheral, byte[] data, bool isJson)
    {
        Receiving = true;
        bool isFirstPacket = (_expectedSize == -1);

        if (isFirstPacket)
        {
            if (data.Length < 12) { Cleanup(peripheral); return; }

            _expectedSize = BinaryPrimitives.ReadInt32LittleEndian(data[0..4]);
            _userId = BinaryPrimitives.ReadInt32LittleEndian(data[4..8]);
            _checksum = data[8..12];

            _imageBuffer.Clear();
            if (_expectedSize > 0)
            {
                _imageBuffer.AddRange(data[12..]);
            }
        }
        else
        {
            _imageBuffer.AddRange(data);
        }

        if (_imageBuffer.Count >= _expectedSize && _expectedSize > 0)
        {
            byte[] finalData = _imageBuffer.ToArray();
            byte[] actualHash = XxHash32.Hash(finalData);

            if (actualHash.SequenceEqual(_checksum))
            {
                if (isJson)
                {
                    string response = System.Text.Encoding.UTF8.GetString(finalData);
                    if (response.Contains("\"received\":true"))
                    {
                        _onNotificationSucceeded?.Invoke(_expectedUser);
                    }
                }
                else
                {
                    _onImageReceived?.Invoke(_userId, finalData);
                }
            }

            Cleanup(peripheral);
        }
    }

    private void Cleanup(CBPeripheral peripheral)
    {
        _expectedSize = -1;
        Receiving = false;
        // In iOS, the CentralManager handles the disconnection
        // You would typically call CentralManager.CancelPeripheralConnection(peripheral) here
    }
}
