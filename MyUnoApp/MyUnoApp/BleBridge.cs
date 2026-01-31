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
    // Die MainPage abonniert dies, um Daten zu empfangen
    public static Action<string, string>? OnUserDiscovered;

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
    public static void toggleScanning()
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
            androidUtil.checkRequirementsBLEDiscovery();
            bleService.StartBleAdvertiser(name, age);
            bleService.StartBleScan(OnUserDiscovered);
            active = true;
        }
    }

}
