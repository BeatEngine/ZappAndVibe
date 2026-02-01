using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUnoApp;

public interface IBleService
{
  
    void StartBleAdvertiser(int userId, string name, string age);
    void StopBleAdvertiser();
    void StartBleScan(Action<int, string, string> onPeerFound);
    void StopBleScan();
}
