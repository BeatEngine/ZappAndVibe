using MyUnoApp.Platforms.iOS;
using UIKit;
using Uno.UI.Hosting;
using UserNotifications;

namespace MyUnoApp.iOS;

public class EntryPoint
{
    // This is the main entry point of the application.
    public static void Main(string[] args)
    {
        App.InitializeLogging();

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseAppleUIKit()
            .Build();

        host.Run();

        Task t = Task.Run(async() =>
        {
            // Request Notification Permission
            var center = UNUserNotificationCenter.Current;
            var (granted, error) = await center.RequestAuthorizationAsync(
                UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound);

            if (granted)
            {
                Console.WriteLine("Notifications granted");
            }
        });
        t.Wait();
        try
        {
            MyUnoApp.BleBridge.bleService = new IosBleService();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Crash Init: {ex}");
        }

    }
}
