using System;
using Android.Net.Wifi.P2p;
using Android.Net.Wifi.P2p.Nsd;
using Java.Lang;

namespace MyUnoApp.Droid;

public class MyServiceResponseListener : Java.Lang.Object, WifiP2pManager.IServiceResponseListener, WifiP2pManager.IDnsSdServiceResponseListener
{
    // In .NET 9/10 ist 'ServiceType' oft direkt im Namespace des Managers oder 
    // als Alias definiert. Wir nutzen hier die vom Compiler geforderte Signatur.
    public void OnServiceAvailable(ServiceType protocolType, byte[]? responseData, WifiP2pDevice? srcDevice)
    {
        System.Diagnostics.Debug.WriteLine("[ZappVibe] Service entdeckt.");
    }

     public void OnDnsSdServiceAvailable(string? instanceName, string? registrationType, WifiP2pDevice? srcDevice)
    {
        // Diese Methode wird aufgerufen, wenn ein DNS-SD Dienst (wie Ihr ZappVibe) physisch gefunden wurde
        System.Diagnostics.Debug.WriteLine($"[ZappVibe] Dienst gefunden: {instanceName} ({registrationType})");
    }



}
