using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUnoApp;

public class BleBridge
{
    public static string name = "test-";
    public static string age = "26";
    public static string image = "";
    public static int userId = Guid.NewGuid().GetHashCode();
    // Die MainPage abonniert dies, um Daten zu empfangen
    public static Action<int, string, string, Dictionary<string, object>>? OnUserDiscovered;

    public static Action<int, byte[]>? OnDataReceived;

    public static Action<int, string>? OnNotificationReceived;

    public static bool active = false;
    public static IAndroidUtil androidUtil { get; set; }

    public static IBleService bleService { get; set; }

    public static bool aktiv = false;
    public static void initNames()
    {
        ValueTuple<string, string> res = androidUtil.initNames();
        name = res.Item1;
        age = res.Item2;
    }

    public static void SetImageData(byte[] data)
    {
        bleService.SetImageData(data);
    }

    public static void SendNotification(int suserId, string jsonNotification)
    {
        bleService.SendNotification(suserId, jsonNotification);
    }
    public static bool toggleScanning()
    {
        if(active)
        {
            bleService.StopBleScan();
            bleService.StopBleAdvertiser();
            active = false;
        }
        else
        {
            initNames();
            if (!androidUtil.checkRequirementsBLEDiscovery())
            {
                return false;
            }
            bleService.StartBleAdvertiser(userId, name, age, OnNotificationReceived);
            bleService.StartBleScan(OnUserDiscovered, OnDataReceived);
            active = true;
            
        }
        return true;
    }

}
