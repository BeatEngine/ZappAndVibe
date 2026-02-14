using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUnoApp;

public interface IBleService
{
  
    void StartBleAdvertiser(int userId, string name, string age, Action<int, string> onNotificationReceived);
    void StopBleAdvertiser();
    void StartBleScan(Action<int, string, string, Dictionary<string, object>> onPeerFound, Action<int, byte[]> onDataReceived);
    void StopBleScan();

    void SetImageData(byte[] data);

    void SendNotification(int userId, string jsonNotification);
}
