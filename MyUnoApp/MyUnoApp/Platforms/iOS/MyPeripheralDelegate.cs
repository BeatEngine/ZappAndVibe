using System.IO.Hashing;
using CoreBluetooth;
using Foundation;
using MyUnoApp.Platforms.iOS;

public class MyPeripheralDelegate : CBPeripheralManagerDelegate
{
    private IosBleService _service;
    private CBMutableCharacteristic _imageCharacteristic;
    private CBMutableCharacteristic _jsonCharacteristic;
    // State tracking for the "Chunking" process
    private byte[] _dataToTransmit;
    private int _transmitIndex;
    private CBCentral _activeSubscriber;
    private bool _isSending = false;
    public MyPeripheralDelegate(IosBleService service) => _service = service;

    public override void StateUpdated(CBPeripheralManager peripheral)
    {
        if (peripheral.State == CBManagerState.PoweredOn)
        {
            var serviceUuid = CBUUID.FromString("43badf3b-e2ed-4e7e-9508-d0943b010ef8");
            var service = new CBMutableService(serviceUuid, true);

            _imageCharacteristic = new CBMutableCharacteristic(
                CBUUID.FromString("ade72fb5-fca9-45d3-9fce-451ec5da5b55"),
                CBCharacteristicProperties.Read | CBCharacteristicProperties.Notify,
                null, CBAttributePermissions.Readable);

            _jsonCharacteristic = new CBMutableCharacteristic(
                CBUUID.FromString("9e509813-bb73-463a-9371-dafacb86f577"),
                CBCharacteristicProperties.Read | CBCharacteristicProperties.Write | CBCharacteristicProperties.Notify,
                null, CBAttributePermissions.Readable | CBAttributePermissions.Writeable);

            service.Characteristics = new CBCharacteristic[] { _imageCharacteristic, _jsonCharacteristic };
            peripheral.AddService(service);
        }
    }

    // This is called when a client enables notifications (CCCD write in Android)
    public override void CharacteristicSubscribed(CBPeripheralManager peripheral, CBCentral central, CBCharacteristic characteristic)
    {
        if (characteristic.UUID.Equals(_imageCharacteristic.UUID))
        {
            Console.WriteLine("Client subscribed. Preparing chunked transfer...");
            _activeSubscriber = central;

            // Prepare the packet exactly like your Android logic: [Length(4) | UserId(4) | Checksum(4) | Data...]
            _dataToTransmit = PrepareFullPayload();
            _transmitIndex = 0;
            _isSending = true;

            SendNextChunks(peripheral);
        }
    }

    // Handles the "Chunking" logic when a client subscribes to notifications
    public override void ReadyToUpdateSubscribers(CBPeripheralManager peripheral)
    {
        if (_isSending)
        {
            SendNextChunks(peripheral);
        }
    }

    private void SendNextChunks(CBPeripheralManager peripheral)
    {
        bool didSend = true;

        while (didSend && _transmitIndex < _dataToTransmit.Length)
        {
            // Calculate remaining bytes
            int amountToSend = _dataToTransmit.Length - _transmitIndex;

            // iOS automatically suggests the best MTU for the current connection
            int mtu = (int)_activeSubscriber.MaximumUpdateValueLength;
            if (amountToSend > mtu) amountToSend = mtu;

            // Slice the data
            byte[] chunk = new byte[amountToSend];
            Array.Copy(_dataToTransmit, _transmitIndex, chunk, 0, amountToSend);

            // Attempt to send
            // UpdateValue returns 'false' if the transmit queue is full
            didSend = peripheral.UpdateValue(NSData.FromArray(chunk), _imageCharacteristic, new CBCentral[] { _activeSubscriber });

            if (didSend)
            {
                _transmitIndex += amountToSend;
            }
        }

        if (_transmitIndex >= _dataToTransmit.Length)
        {
            _isSending = false;
            Console.WriteLine("Transfer complete.");
        }
    }

    private byte[] PrepareFullPayload()
    {
        // Replicating your Android first-packet header logic
        byte[] imageData = _service.FullImageData ?? Array.Empty<byte>();
        byte[] checksum = XxHash32.Hash(imageData);

        byte[] header = new byte[12];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(0, 4), imageData.Length);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(4, 4), _service.UserId);
        checksum.CopyTo(header, 8);

        byte[] fullPayload = new byte[header.Length + imageData.Length];
        header.CopyTo(fullPayload, 0);
        imageData.CopyTo(fullPayload, 12);

        return fullPayload;
    }

    public override void WriteRequestsReceived(CBPeripheralManager peripheral, CBATTRequest[] requests)
    {
        foreach (var request in requests)
        {
            if (request.Characteristic.UUID.Equals(_jsonCharacteristic.UUID))
            {
                // Handle JSON Logic (checksum verification)
                _service.OnNotificationReceived?.Invoke(_service.UserId, "Parsed JSON Here");
                peripheral.RespondToRequest(request, CBATTError.Success);
            }
        }
    }
}
