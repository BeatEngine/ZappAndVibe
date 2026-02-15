using CoreBluetooth;
using Foundation;
using MyUnoApp.Platforms.iOS;

public class MyCentralDelegate : CBCentralManagerDelegate
{
    private IosBleService _service;
    private Dictionary<int, CBPeripheral> _discoveredDevices = new();

    private readonly CBUUID _serviceUuid = CBUUID.FromString("43badf3b-e2ed-4e7e-9508-d0943b010ef8");

    // Stores JSON messages indexed by UserId
    private Dictionary<int, string> _pendingNotifications = new();
    public MyCentralDelegate(IosBleService service) => _service = service;

    public override void UpdatedState(CBCentralManager central)
    {
        if (central.State == CBManagerState.PoweredOn)
        {
            central.ScanForPeripherals(new CBUUID[] { _serviceUuid }, new PeripheralScanningOptions
            {
                AllowDuplicatesKey = false
            });
        }
    }

    public void AddNotification(int targetUser, string notificationBody)
    {
        if (!_pendingNotifications.ContainsKey(targetUser))
        {
            _pendingNotifications.Add(targetUser, notificationBody);
            Console.WriteLine($"[iOS] Added pending notification for User: {targetUser}");
        }
    }

    public override void DiscoveredPeripheral(CBCentralManager central, CBPeripheral peripheral, NSDictionary advertisementData, NSNumber RSSI)
    {
        // Parse the 'LocalName' we set in the Advertiser
        string localName = advertisementData.ObjectForKey(CBAdvertisement.DataLocalNameKey)?.ToString();
        if (string.IsNullOrEmpty(localName)) return;

        var parts = localName.Split('|');
        if (parts.Length < 3) return;

        int userId = int.Parse(parts[0]);
        string name = parts[1];
        string age = parts[2];

        _discoveredDevices[userId] = peripheral;

        var dict = new Dictionary<string, object> { { "signal", RSSI.Int32Value } };
        _service.OnPeerFound?.Invoke(userId, name, age, dict);

        // Trigger GATT Connection if needed
        central.ConnectPeripheral(peripheral);
    }
}
