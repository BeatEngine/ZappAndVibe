namespace MyUnoApp;

public static class WifiBridge
{

    public static string name = "test1";
    public static string age = "age";
    public static string image = "";
    // Die MainPage abonniert dies, um Daten zu empfangen
    public static Action<string, string>? OnUserDiscovered;

    // Der Android-Teil ruft dies auf, wenn er jemanden findet
    public static void RaiseUserFound(string name, string age)
    {
        // Wir stellen sicher, dass der Aufruf auf dem UI-Thread landet
        // (Wichtig für Android, da WLAN-Callbacks oft im Hintergrund laufen)
        Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() =>
        {
            OnUserDiscovered?.Invoke(name, age);
        });
    }
}
