using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace MyUnoApp;

public sealed partial class MainPage : Page
{

    private static ListView userListInstance;
    public MainPage()
    {

        this.InitializeComponent();
        UserList.ItemsSource = new ObservableCollection<UserItem>();
        {
        };
        userListInstance = UserList;
        BleBridge.OnUserDiscovered = (userId, name, age) =>
        {
            System.Diagnostics.Debug.WriteLine($"Gefunden: {name}, {age}");

            UserItem update = new UserItem(userId, name, age, "") { IsOnline = true };
            ObservableCollection<UserItem> list = (ObservableCollection<UserItem>)UserList.ItemsSource;
            UserItem existingItem = list.FirstOrDefault(x => x.UserId == update.UserId);
            if (existingItem != null)
            {
                int index = list.IndexOf(existingItem);
                list[index].Discard();
                list[index] = update;
            }
            else
            {
                list.Add(update);
            }


                    
        };

    }

    public static ListView GetUserList()
    {
        return userListInstance;
    }

    private void OnStartScanClicked(object sender, RoutedEventArgs e)
    {
        if (BleBridge.toggleScanning())
        {
            Headline.Text = "Scanning";
        }
        else
        {
            if (sender is ToggleSwitch sw)
            {
                sw.IsOn = false;
            }
            Headline.Text = "Try again";
        }
    }
}
