using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.Util;
using System.IO.Hashing;

namespace MyUnoApp.Platforms.Android;

public class MyGattServerCallback : BluetoothGattServerCallback
{
    private MyGattServerProvider provider;
    
    private int _currentMtu = 23; // Standard-Startwert

    public MyGattServerCallback(MyGattServerProvider prov)
    {
        provider = prov;
    }

    public override void OnMtuChanged(BluetoothDevice device, int mtu)
    {
        _currentMtu = mtu;
    }

    // Der Client fordert das Bild an
    public override void OnDescriptorWriteRequest(BluetoothDevice device, int requestId, BluetoothGattDescriptor descriptor, bool preparedWrite, bool responseNeeded, int offset, byte[] value)
    {
        Log.Info(Class.SimpleName, $"Request of {device.Address} for {descriptor.Uuid}");
        if (responseNeeded)
        {
            provider.getServer().SendResponse(device, requestId, GattStatus.Success, offset, value);
        }

        // Wenn der Client Notifications aktiviert hat (EnableNotificationValue = 0x01, 0x00)
        if (value.SequenceEqual(BluetoothGattDescriptor.EnableNotificationValue.ToArray()))
        {

            // Starte den Versand des Bildes in einem Task
            _ = SendImageInChunks(device);
   
        }
        Console.WriteLine("GATT Client requested something");
    }

    private async Task SendImageInChunks(BluetoothDevice device)
    {
        try
        {

            if (provider.fullImageData == null || provider.fullImageData.Length == 0)
            {
                Console.WriteLine("GATT no data to send");
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
                    byte[] lengthHeader = BitConverter.GetBytes(provider.fullImageData.Length);
                    Array.Copy(lengthHeader, 0, packet, 0, 4);
                    byte[] userId = BitConverter.GetBytes(provider.userId);
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
}
