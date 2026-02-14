using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.Content;
using Android.Util;

namespace MyUnoApp.Platforms.Android;

public class MyGattServerProvider
{
    private BluetoothGattServer _gattServer;
    public BluetoothGattCharacteristic characteristic{ get; set; }
    public BluetoothGattCharacteristic characteristicJSON { get; set; }
    private BluetoothManager _bluetoothManager;
    private Context _context;
    public int userId{ get; }

    private static MyGattServerProvider instance;
    public static MyGattServerProvider getInstance()
    {
        return instance;
    }
    public byte[] fullImageData { get; set; } = null;


    public Action<int, string> OnNotificationReceived { get; set; } = null;

    public BluetoothGattServer getServer()
    {
        return _gattServer;
    }

    public MyGattServerProvider(Context context, int userId)
    {
        instance = this;
        _context = context;
        this.userId = userId;
        _bluetoothManager = (BluetoothManager)context.GetSystemService(Context.BluetoothService);
    }

    public void StartServer()
    {
        // 1. Server öffnen
        _gattServer = _bluetoothManager.OpenGattServer(_context, new MyGattServerCallback(this));

        // 2. Service erstellen (UUID generieren oder festlegen)
        var serviceUuid = Java.Util.UUID.FromString("43badf3b-e2ed-4e7e-9508-d0943b010ef8");
        var service = new BluetoothGattService(serviceUuid, GattServiceType.Primary);

        // 3. Characteristic für das Bild erstellen
        // Property: Read (zum Abrufen) oder Notify (zum Streamen)
        var charUuid = Java.Util.UUID.FromString("ade72fb5-fca9-45d3-9fce-451ec5da5b55");
        characteristic = new BluetoothGattCharacteristic(
            charUuid,
            GattProperty.Read | GattProperty.Notify,
            GattPermission.Read);

        // Characteristic für die Logik (Notifications)
        // Property: Read (zum Abrufen) oder Notify (zum Streamen)
        var charUuid2 = Java.Util.UUID.FromString("9e509813-bb73-463a-9371-dafacb86f577");
        characteristicJSON = new BluetoothGattCharacteristic(
            charUuid2,
            GattProperty.Read | GattProperty.Notify |GattProperty.Write,
            GattPermission.Read | GattPermission.Write);

        // CCCD Descriptor hinzufügen ---
        var configUuid = Java.Util.UUID.FromString("00002902-0000-1000-8000-00805f9b34fb");
        var configDescriptor = new BluetoothGattDescriptor(
            configUuid,
            GattDescriptorPermission.Write | GattDescriptorPermission.Read);

        // CCCD Descriptor hinzufügen ---
        var configDescriptorJSON = new BluetoothGattDescriptor(
            configUuid,
            GattDescriptorPermission.Write | GattDescriptorPermission.Read);

        characteristic.AddDescriptor(configDescriptor);
        characteristicJSON.AddDescriptor(configDescriptorJSON);


        // 4. Service dem Server hinzufügen
        service.AddCharacteristic(characteristic);
        service.AddCharacteristic(characteristicJSON);
        _gattServer.AddService(service);
    }

    public void StopServer()
    {
        if (_gattServer != null)
        {
            // 1. Alle Services entfernen
            _gattServer.ClearServices();

            // 2. Verbindungen trennen (optional, aber sauber)
            // Hinweis: Android schließt Verbindungen meist automatisch beim Close()

            // 3. Den Server schließen und Ressourcen freigeben
            _gattServer.Close();

            // 4. Referenz löschen
            _gattServer = null;

            Console.WriteLine("GATT Server stopped");
        }
    }

}
