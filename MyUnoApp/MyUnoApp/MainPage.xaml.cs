using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace MyUnoApp;

public sealed partial class MainPage : Page
{

    private static ListView userListInstance;

    private DateTime last = DateTime.MinValue;
    public MainPage()
    {

        this.InitializeComponent();
        UserList.ItemsSource = new ObservableCollection<UserItem>();
        {
        };
        Task.Run(async () =>
        {
            byte[] imageData = await GetImageBytesFromAssetsAsync();
            BleBridge.SetImageData(imageData);
        });
        userListInstance = UserList;
        BleBridge.OnDataReceived = (userId, imageBytes) =>
        {
            System.Diagnostics.Debug.WriteLine($"Received image for {userId}");
            ObservableCollection<UserItem> list = (ObservableCollection<UserItem>)UserList.ItemsSource;
            UserItem existingItem = list.FirstOrDefault(x => x.UserId == userId);
            if (existingItem != null)
            {
                int index = list.IndexOf(existingItem);
                list[index].Discard();
                list[index].SetImage(imageBytes);
                list[index].Updated();
            }

        };

        BleBridge.OnUserDiscovered = (userId, name, age) =>
        {
            if ((DateTime.Now - last).TotalSeconds > 0)
            {
                last = DateTime.Now;
                System.Diagnostics.Debug.WriteLine($"Gefunden: {name}, {age}");

                UserItem update = new UserItem(userId, name, age, "") { IsOnline = true };
                ObservableCollection<UserItem> list = (ObservableCollection<UserItem>)UserList.ItemsSource;
                UserItem existingItem = list.FirstOrDefault(x => x.UserId == update.UserId);
                if (existingItem != null)
                {
                    int index = list.IndexOf(existingItem);
                    list[index].Discard();
                    list[index].Name = name;
                    list[index].Age = age;
                    list[index].Updated();
                }
                else
                {
                    list.Add(update);
                }
                
            }
                    
        };

    }

    public static ListView GetUserList()
    {
        return userListInstance;
    }

    public async Task<byte[]> GetImageBytesFromAssetsAsync()
    {
        var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/user.png"));
        var buffer = await FileIO.ReadBufferAsync(file);
        return buffer.ToArray();
    }

    private async Task OnStartScanClicked(object sender, RoutedEventArgs e)
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
