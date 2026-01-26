using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;

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
                "android.permission.ACCESS_FINE_LOCATION"
            }, 123);
        }

        
        
        try 
        {
            wifiService = new WifiService(this);
            // Jetzt klappt die Zuweisung, da WifiService das Interface implementiert
            MyUnoApp.WifiBridge.wifiService = wifiService;
        }
        catch (Exception ex)
        {
            // Verhindert den sofortigen Absturz, falls WLAN nicht verfügbar ist
            System.Diagnostics.Debug.WriteLine($"Crash WiFi Init: {ex}");
        }

    }

}
