using System.ComponentModel.DataAnnotations;
using System.Collections.ObjectModel;
using System.Reflection.Metadata;

namespace MyUnoApp;

public sealed partial class MainPage : Page
{
    public MainPage()
    {

        this.InitializeComponent();
        UserList.ItemsSource = new ObservableCollection<UserItem>();
        {
        };
        WifiBridge.initNames();
        WifiBridge.OnUserDiscovered = (name, age) =>
        {
            System.Diagnostics.Debug.WriteLine($"Gefunden: {name}, {age}");
            ((ObservableCollection<UserItem>)UserList.ItemsSource).Add(new UserItem(name, age, "") { IsOnline = true });
        };

    }

    private void OnStartScanClicked(object sender, RoutedEventArgs e)
    {
        WifiBridge.toggleScanning();
        Headline.Text = "Scanning";
    }
}
