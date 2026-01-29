using System;
using Android.Net.Wifi.P2p; 
using Java.Lang;           

namespace MyUnoApp.Droid;

// Die Klasse implementiert das Interface IActionListener

public class SimpleActionListener : Java.Lang.Object, WifiP2pManager.IActionListener
{
    private readonly string _actionName;
    private Action onSuccess;
    private Action onFail;

    public SimpleActionListener(string actionName, Action success = null, Action fail = null)
    {
        _actionName = actionName;
        onSuccess = success;
        onFail = fail;
    }

    // Erfolg-Callback
    public void OnSuccess()
    {
        System.Diagnostics.Debug.WriteLine($"[ZappVibe] {_actionName} erfolgreich.");
        onSuccess?.Invoke();
    }

    // Fehler-Callback: WICHTIG - Der Typ muss WifiP2pFailureReason sein!
    public void OnFailure(WifiP2pFailureReason reason)
    {
        System.Diagnostics.Debug.WriteLine($"[ZappVibe] {_actionName} fehlgeschlagen: {reason}");
        onFail?.Invoke();
    }
}
