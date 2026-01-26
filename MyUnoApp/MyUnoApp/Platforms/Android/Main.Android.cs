using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.UI.Xaml.Media;
using Android.Net.Wifi.P2p;
using Android.Net.Wifi.P2p.Nsd;
using System.Runtime.Intrinsics.X86;
using MyUnoApp.Droid; // Namespace Ihres WifiService

namespace MyUnoApp.Droid;

[global::Android.App.ApplicationAttribute(
    Label = "@string/ApplicationName",
    Icon = "@mipmap/icon",
    LargeHeap = true,
    HardwareAccelerated = true,
    Theme = "@style/Theme.App.Starting"
)]



public class Application : Microsoft.UI.Xaml.NativeApplication
{
    
    private static bool active = false;
    static Application()
    {
        App.InitializeLogging();
    }

    public Application(IntPtr javaReference, JniHandleOwnership transfer)
        : base(() => new App(), javaReference, transfer)
    {

    }

}

