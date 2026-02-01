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
    public static Action<int, string, string>? OnUserDiscovered;

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
            bleService.StartBleAdvertiser(userId, name, age);
            bleService.StartBleScan(OnUserDiscovered);
            active = true;
            
        }
        return true;
    }

}
