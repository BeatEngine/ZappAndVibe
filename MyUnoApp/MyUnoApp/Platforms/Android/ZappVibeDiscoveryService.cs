using Android.App;            // Für [Service], Service-Klasse und Notification
using Android.Content;        // Für Intent und Context
using Android.OS;             // Für IBinder, Build und BuildVersionCodes
using Android.Runtime;        // Falls du Java-Interoperabilität brauchst
using Android.Net.Wifi;       // Für WifiManager und WifiLock


[Service(Enabled = true, ForegroundServiceType = Android.Content.PM.ForegroundService.TypeConnectedDevice)]
public class ZappVibeDiscoveryService : Service
{
    private const int NOTIFICATION_ID = 1001;
    private const string CHANNEL_ID = "discovery_channel";
    
    private WifiManager.WifiLock _wifiLock;

    public static ZappVibeDiscoveryService service;

    public void ActivateBackgroundWIFI()
    {
        if (_wifiLock == null)
        {
            var wifiManager = (WifiManager)GetSystemService(Context.WifiService);

            // "FullHighPerf" stellt sicher, dass die Latenz niedrig und die Karte aktiv bleibt
            _wifiLock = wifiManager.CreateWifiLock(Android.Net.WifiMode.FullHighPerf, "ZappVibe:WifiLock");

        }

        if (!_wifiLock.IsHeld)
        {
            _wifiLock.Acquire();
        }
    }

    public void ReleaseBackgroundWIFI()
    {
        if (_wifiLock != null && _wifiLock.IsHeld)
        {
            _wifiLock.Release();
        }
    }

    public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
    {
        service = this;
        CreateNotificationChannel();

        // Notification für den User (Pflicht für Foreground Services)
        var notification = new Notification.Builder(this, CHANNEL_ID)
            .SetContentTitle("ZappVibe Discovery")
            .SetContentText("Near people can see your profile")
            .SetOngoing(true)
            .Build();

        // Service in den Vordergrund heben
        StartForeground(NOTIFICATION_ID, notification);

        // HIER: Starte deine StartBroadcasting() Logik
        return StartCommandResult.Sticky;
    }

    public override IBinder OnBind(Intent intent) => null;

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(CHANNEL_ID, "ZappVibe Background", NotificationImportance.Low);
            var manager = (NotificationManager)GetSystemService(NotificationService);
            manager.CreateNotificationChannel(channel);
        }
    }
}
