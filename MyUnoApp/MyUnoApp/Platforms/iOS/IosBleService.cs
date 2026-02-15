using CoreBluetooth;
using CoreFoundation;
using Foundation;
using System.Buffers.Binary;

namespace MyUnoApp.Platforms.iOS;

public class IosBleService : IBleService
{
    private CBCentralManager _centralManager;
    private CBPeripheralManager _peripheralManager;
    private MyCentralDelegate _centralDelegate;
    private MyPeripheralDelegate _peripheralDelegate;
    private readonly CBUUID ServiceUuid = CBUUID.FromString("43badf3b-e2ed-4e7e-9508-d0943b010ef8");

    public Action<int, string, string, Dictionary<string, object>> OnPeerFound;
    public Action<int, byte[]> OnReceivedData;
    public Action<int, string> OnNotificationReceived;

    public byte[] FullImageData { get; set; }
    public int UserId { get; private set; }

    public void StartBleAdvertiser(int userId, string name, string age, Action<int, string> onNotificationReceived)
    {
        this.UserId = userId;
        this.OnNotificationReceived = onNotificationReceived;

        _peripheralDelegate = new MyPeripheralDelegate(this);
        _peripheralManager = new CBPeripheralManager(_peripheralDelegate, DispatchQueue.DefaultGlobalQueue);

        // Map your Manufacturer Data logic to iOS Advertisement
        // iOS limitation: We can't use 0x1234 Manufacturer ID easily in the background. 
        // We use the LocalName to pass the primary handshake.
        string discoveryString = $"{userId}|{name}|{age}";
        if (discoveryString.Length > 25) discoveryString = discoveryString.Substring(0, 25);

        var adData = new StartAdvertisingOptions
        {
            LocalName = discoveryString,
            ServicesUUID = new CBUUID[] { ServiceUuid }
        };

        _peripheralManager.StartAdvertising(adData.Dictionary);
    }

    public void StartBleScan(Action<int, string, string, Dictionary<string, object>> onPeerFound, Action<int, byte[]> onReceivedData)
    {
        this.OnPeerFound = onPeerFound;
        this.OnReceivedData = onReceivedData;

        _centralDelegate = new MyCentralDelegate(this);
        _centralManager = new CBCentralManager(_centralDelegate, DispatchQueue.DefaultGlobalQueue);
    }

    public void StopBleAdvertiser()
    {
        _peripheralManager?.StopAdvertising();
        _peripheralManager?.RemoveAllServices();
    }

    public void StopBleScan() => _centralManager?.StopScan();

    public void SetImageData(byte[] data) => FullImageData = data;

    public void SendNotification(int userId, string jsonNotification)
    {
        _centralDelegate?.AddNotification(userId, jsonNotification);
    }
}
