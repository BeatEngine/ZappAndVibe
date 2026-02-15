
using System.IO.Hashing;
using Android.Bluetooth;
using Android.Util;

namespace MyUnoApp.Platforms.Android;

public class MyGattServerCallback : BluetoothGattServerCallback
{
    private MyGattServerProvider provider;
    
    private int _currentMtu = 23; // Standard-Startwert

    public static bool sending = false;

    public MyGattServerCallback(MyGattServerProvider prov)
    {
        provider = prov;
    }

    public override void OnMtuChanged(BluetoothDevice device, int mtu)
    {
        _currentMtu = mtu;
    }

    private List<byte> jsonBuffer = new List<byte>();

    // Server Recieve JSON
    public override void OnCharacteristicWriteRequest(BluetoothDevice device, int requestId,
        BluetoothGattCharacteristic characteristic, bool preparedWrite, bool responseNeeded,
        int offset, byte[] value)
    {
        var uuid = characteristic.Uuid.ToString().ToLower();

        if (responseNeeded)
        {
            provider.getServer().SendResponse(device, requestId, GattStatus.Success, offset, value);
        }

        if ("9e509813-bb73-463a-9371-dafacb86f577".Equals(uuid))
        {
            string response;
            int userId = 0;
            // If the incomming data is too short reply not received
            if (value.Length <= 8)
            {
                response = "{\"received\":false}";
                _ = SendChunks(device, System.Text.Encoding.UTF8.GetBytes(response));
                Console.WriteLine("Notification request too short");
                return;
            }
            byte[] checksum = new byte[] { 0, 0, 0, 0 };
            Array.Copy(value, 0, checksum, 0, 4);
            List<byte> bytes = new List<byte>();

            userId = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(value[4..8]);
            bytes.Capacity = value.Length - 8;
            bytes.AddRange(value[8..]);
            byte[] inner = bytes.ToArray();
            // If the hash of value is wrong reply not received
            if (!XxHash32.Hash(inner).SequenceEqual(checksum))
            {
                response = "{\"received\":false}";
                _ = SendChunks(device, System.Text.Encoding.UTF8.GetBytes(response));
                Console.WriteLine("Notification request invalid checksum");
                return;
            }

            response = "{\"received\":true}";
            _ = SendChunks(device, System.Text.Encoding.UTF8.GetBytes(response));
            if (provider.OnNotificationReceived != null)
            {
                provider.OnNotificationReceived.Invoke(userId, System.Text.Encoding.UTF8.GetString(inner));
            }

        }

        
    }

    // Der Client fordert das Bild an
    public override void OnDescriptorWriteRequest(BluetoothDevice device, int requestId, BluetoothGattDescriptor descriptor, bool preparedWrite, bool responseNeeded, int offset, byte[] value)
    {
        sending = true;
        Log.Info(Class.SimpleName, $"Request of {device.Address} for {descriptor.Uuid}");
        if (responseNeeded)
        {
            provider.getServer().SendResponse(device, requestId, GattStatus.Success, offset, value);
        }

        // Wenn der Client Notifications aktiviert hat (EnableNotificationValue = 0x01, 0x00)
        if (value.SequenceEqual(BluetoothGattDescriptor.EnableNotificationValue.ToArray()))
        {
            string charUuid = descriptor.Characteristic.Uuid.ToString().ToLower();
            // Starte den Versand des Bildes in einem Task
            if ("ade72fb5-fca9-45d3-9fce-451ec5da5b55".Equals(charUuid))
            {
                _ = SendImageInChunks(device);
            }
        }
        else
        {
            sending = false;
        }
            Console.WriteLine("GATT Client requested something");
    }


    

    private async Task SendImageInChunks(BluetoothDevice device)
    {
        try
        {
            DateTime a = DateTime.Now;
            while((provider.fullImageData == null || MyGattCallback.receiving) && (DateTime.Now-a).TotalSeconds > 2)
            {
                await Task.Delay(100);
            }


            if (provider.fullImageData == null || provider.fullImageData.Length == 0)
            {
                //Send this so the client can make another request.
                Console.WriteLine("GATT no data to send");
                byte[] noData = new byte[] { 1, 2, 4, 8 };
                provider.characteristic.SetValue(noData);
                bool notified = provider.getServer().NotifyCharacteristicChanged(device, provider.characteristic, false);
                sending = false;
                return;
            }
            // Wir berechnen die maximale Datengröße pro Paket
            // MTU minus 3 Bytes GATT-Header
            int maxChunkSize = _currentMtu - 3;
            maxChunkSize = Math.Min(maxChunkSize, 512 - 3);
            int sentBytes = 0;
            bool isFirstPacket = true;
            byte[] checksum = XxHash32.Hash(provider.fullImageData);
            await Task.Delay(30);
            Console.WriteLine($"GATT server sending to {device.Address} chunk-size {maxChunkSize}");
            while (sentBytes < provider.fullImageData.Length)
            {
                byte[] packet;

                if (isFirstPacket)
                {
                    // Das erste Paket enthält: 12 Bytes Länge + Bilddaten
                    int dataInFirstPacket = Math.Min(maxChunkSize - 12, provider.fullImageData.Length);
                    packet = new byte[12 + dataInFirstPacket];

                    // Länge als Int32 (Little Endian)
                    byte[] lengthHeader = new byte[4];
                    System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(lengthHeader, provider.fullImageData.Length);
                    Array.Copy(lengthHeader, 0, packet, 0, 4);
                    byte[] userId = new byte[4];
                    System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(userId, provider.userId);
                    Array.Copy(userId, 0, packet, 4, 4);
                    Array.Copy(checksum, 0, packet, 8, 4);
                    Array.Copy(provider.fullImageData, 0, packet, 12, dataInFirstPacket);

                    sentBytes += dataInFirstPacket;
                    isFirstPacket = false;
                }
                else
                {
                    // Folgepakete
                    int remaining = provider.fullImageData.Length - sentBytes;
                    int size = Math.Min(maxChunkSize, remaining);
                    packet = new byte[size];

                    Array.Copy(provider.fullImageData, sentBytes, packet, 0, size);
                    sentBytes += size;
                }

                // Den Wert in die Characteristic schreiben und pushen
                provider.characteristic.SetValue(packet);
                bool notified = provider.getServer().NotifyCharacteristicChanged(device, provider.characteristic, false);

                if (!notified)
                {
                    Console.WriteLine("GATT Notification failed ");
                    await Task.Delay(50); // Längere Pause bei Fehler
                }

                // Kurze Pause
                await Task.Delay(30);
            }
            Console.WriteLine("GATT Server sendet: " + sentBytes / 1000 + " Kb");

        }catch(Exception e)
        {
            Console.WriteLine("Sending error: " + e.Message);
        }
    }

    private async Task SendChunks(BluetoothDevice device, byte[] response)
    {
        try
        {

            if (response == null || response.Length == 0)
            {
                //Send this so the client can make another request.
                Console.WriteLine("GATT notification-reply no data to send");
                byte[] noData = new byte[] { 1, 2, 4, 8 };
                provider.characteristicJSON.SetValue(noData);
                bool notified = provider.getServer().NotifyCharacteristicChanged(device, provider.characteristicJSON, false);
                sending = false;
                return;
            }
            // Wir berechnen die maximale Datengröße pro Paket
            // MTU minus 3 Bytes GATT-Header
            int maxChunkSize = _currentMtu - 3;
            maxChunkSize = Math.Min(maxChunkSize, 512 - 3);
            int sentBytes = 0;
            bool isFirstPacket = true;
            byte[] checksum = XxHash32.Hash(response);
            await Task.Delay(30);
            Console.WriteLine($"GATT server sending to {device.Address} chunk-size {maxChunkSize}");
            while (sentBytes < response.Length)
            {
                byte[] packet;

                if (isFirstPacket)
                {
                    // Das erste Paket enthält: 12 Bytes Länge + Bilddaten
                    int dataInFirstPacket = Math.Min(maxChunkSize - 12, response.Length);
                    packet = new byte[12 + dataInFirstPacket];

                    // Länge als Int32 (Little Endian)
                    byte[] lengthHeader = new byte[4];
                    System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(lengthHeader, response.Length);
                    Array.Copy(lengthHeader, 0, packet, 0, 4);
                    byte[] userId = new byte[4];
                    System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(userId, provider.userId);
                    Array.Copy(userId, 0, packet, 4, 4);
                    Array.Copy(checksum, 0, packet, 8, 4);
                    Array.Copy(response, 0, packet, 12, dataInFirstPacket);

                    sentBytes += dataInFirstPacket;
                    isFirstPacket = false;
                }
                else
                {
                    // Folgepakete
                    int remaining = response.Length - sentBytes;
                    int size = Math.Min(maxChunkSize, remaining);
                    packet = new byte[size];

                    Array.Copy(response, sentBytes, packet, 0, size);
                    sentBytes += size;
                }

                // Den Wert in die Characteristic schreiben und pushen
                provider.characteristicJSON.SetValue(packet);
                bool notified = provider.getServer().NotifyCharacteristicChanged(device, provider.characteristicJSON, false);

                if (!notified)
                {
                    Console.WriteLine("GATT Notification-reply failed ");
                    await Task.Delay(50); // Längere Pause bei Fehler
                }

                // Kurze Pause
                await Task.Delay(30);
            }
            Console.WriteLine("GATT notification sent");

        }
        catch (Exception e)
        {
            Console.WriteLine("Sending error: " + e.Message);
        }
    }

}
