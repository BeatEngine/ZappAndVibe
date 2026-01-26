using System;
using Android.Net.Wifi.P2p; 
using Java.Lang;           

namespace MyUnoApp.Droid;

// Die Klasse implementiert das Interface IActionListener

public class SimpleActionListener : Java.Lang.Object, WifiP2pManager.IActionListener
{
    private readonly string _actionName;

    public SimpleActionListener(string actionName)
    {
        _actionName = actionName;
    }

    // Erfolg-Callback
    public void OnSuccess()
    {
        System.Diagnostics.Debug.WriteLine($"[ZappVibe] {_actionName} erfolgreich.");
    }

    // Fehler-Callback: WICHTIG - Der Typ muss WifiP2pFailureReason sein!
    public void OnFailure(WifiP2pFailureReason reason)
    {
        System.Diagnostics.Debug.WriteLine($"[ZappVibe] {_actionName} fehlgeschlagen: {reason}");
    }
}
