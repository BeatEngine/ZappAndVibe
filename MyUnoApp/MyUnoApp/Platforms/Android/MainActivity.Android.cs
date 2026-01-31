using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using MyUnoApp.Platforms.Android;

namespace MyUnoApp.Droid;

[Activity(
    MainLauncher = true,
    ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
    WindowSoftInputMode = SoftInput.AdjustNothing | SoftInput.StateHidden
)]
public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
{
    static WifiService wifiService;
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        global::AndroidX.Core.SplashScreen.SplashScreen.InstallSplashScreen(this);

        base.OnCreate(savedInstanceState);



        // Berechtigungen zur Laufzeit anfordern
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
        {
            RequestPermissions(new string[] {
                "android.permission.NEARBY_WIFI_DEVICES",
                "android.permission.ACCESS_FINE_LOCATION",
                "android.permission.POST_NOTIFICATIONS",
                "android.permission.BLUETOOTH_SCAN",
                "android.permission.BLUETOOTH_CONNECT",
                "android.permission.BLUETOOTH_ADVERTISE",
                "android.permission.BLUETOOTH_ADMIN"
            }, 123);
        }

        
        
        try 
        {
            //wifiService = new WifiService(this);
            // Jetzt klappt die Zuweisung, da WifiService das Interface implementiert
            //MyUnoApp.WifiBridge.wifiService = wifiService;
            MyUnoApp.BleBridge.bleService = new BLEAdvertiser(this);
            MyUnoApp.BleBridge.androidUtil = new AndroidUtil(this);
        }
        catch (Exception ex)
        {
            // Verhindert den sofortigen Absturz, falls WLAN nicht verfügbar ist
            System.Diagnostics.Debug.WriteLine($"Crash Init: {ex}");
        }

    }

    protected void OnResume()
    {
        base.OnResume();
        if (BleBridge.active)
        {
            IBleService s = BLEAdvertiser.GetInstance();
            s.StartBleScan(((BLEAdvertiser)s).lastAction);
        }
    }

    protected void OnPause()
    {
        if (BleBridge.active)
        {
            BLEAdvertiser.GetInstance().StopBleScan();
        }
        base.OnPause();
    }

}
