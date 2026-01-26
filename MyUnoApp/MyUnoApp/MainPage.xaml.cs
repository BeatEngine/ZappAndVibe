using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;

namespace MyUnoApp;

public sealed partial class MainPage : Page
{
    public MainPage()
    {

        this.InitializeComponent();
        UserList.ItemsSource = new List<UserItem>
        {
        };

        WifiBridge.OnUserDiscovered = (name, age) =>
        {
            System.Diagnostics.Debug.WriteLine($"Gefunden: {name}, {age}");
            ((List<UserItem>)UserList.ItemsSource).Add(new UserItem(name, age, "") { IsOnline = true });
        };

    }

    private void OnStartScanClicked(object sender, RoutedEventArgs e)
    {
        WifiBridge.toggleScanning();
        Headline.Text = "Scanning";
    }
}
