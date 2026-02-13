using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.System;

namespace MyUnoApp;

public sealed partial class MainPage : Page
{

    private static ListView userListInstance;

    private Task uiUpdateTask;
    private bool uiUpdateTaskRunning;

    private bool setProfile = false;
    private CancellationTokenSource uiUpdateCancle = new CancellationTokenSource();

    private DateTime last = DateTime.MinValue;

    private void initUserListUiUpdates()
    {
        uiUpdateTask = new Task(async () =>
        {
            while (true)
            {
                if (uiUpdateTaskRunning)
                {
                    ObservableCollection<UserItem> list = (ObservableCollection<UserItem>)UserList.ItemsSource;
                    DateTime now = DateTime.Now;
                    foreach (var item in list.Where(x => (now - x.creation).TotalSeconds > 2))
                    {
                        item.SetOffline();
                    }
                }
                await Task.Delay(1500);
            }
        });
        uiUpdateTaskRunning = true;
        uiUpdateTask.Start();

        Window.Current.VisibilityChanged += (s, e) =>
        {
            if (e.Visible)
            {
                if (!uiUpdateTaskRunning)
                {
                    //uiUpdateCancle = new CancellationTokenSource();
                    uiUpdateTaskRunning = true;
                    //uiUpdateTask.Start();
                }
            }
            else
            {
                if (uiUpdateTaskRunning)
                {
                    uiUpdateTaskRunning = false;
                    //uiUpdateCancle.Cancel();
                    //uiUpdateCancle.Dispose();
                }
            }
        };
    }

    public MainPage()
    {

        this.InitializeComponent();
        UserList.ItemsSource = new ObservableCollection<UserItem>();
        {
        };
        initUserListUiUpdates();

        /*Task.Run(async () =>
        {
            byte[] imageData = await GetImageBytesFromAssetsAsync();
            BleBridge.SetImageData(imageData);
        });*/

        userListInstance = UserList;
        BleBridge.OnDataReceived = (userId, imageBytes) =>
        {
            System.Diagnostics.Debug.WriteLine($"Received image for {userId}");
            _ = this.DispatcherQueue.TryEnqueue(() =>
            {
            ObservableCollection < UserItem > list = (ObservableCollection<UserItem>)UserList.ItemsSource;
            UserItem existingItem = list.FirstOrDefault(x => x.UserId == userId);
            if (existingItem != null)
            {
                int index = list.IndexOf(existingItem);
                list[index].Updated();
                list[index].SetImage(imageBytes);
            }

                //DEBUG SAVE
                //SaveImageToDiskAsync(userId, imageBytes);

            });

        };

        BleBridge.OnUserDiscovered = (userId, name, age) =>
        {
            if ((DateTime.Now - last).TotalSeconds > 0)
            {
                last = DateTime.Now;
                //System.Diagnostics.Debug.WriteLine($"Gefunden: {name}, {age}");

                UserItem update = new UserItem(userId, name, age, "") { IsOnline = true };
                ObservableCollection<UserItem> list = (ObservableCollection<UserItem>)UserList.ItemsSource;
                UserItem existingItem = list.FirstOrDefault(x => x.UserId == update.UserId);
                if (existingItem != null)
                {
                    int index = list.IndexOf(existingItem);
                    list[index].Updated();
                    list[index].Name = name;
                    list[index].Age = age;
                }
                else
                {
                    list.Add(update);
                }
                
            }
                    
        };

    }

    private async Task SaveImageToDiskAsync(long userId, byte[] imageBytes)
    {
        try
        {
            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            savePicker.FileTypeChoices.Add("PNG Image", new List<string>() { ".png" });
            savePicker.SuggestedFileName = $"User_{userId}.png";

            // Wichtig für Android: Den Picker mit dem Window assoziieren
            // Uno macht das meist intern, aber sicher ist sicher:
            var file = await savePicker.PickSaveFileAsync();

            if (file != null)
            {
                // Nutze den Stream-Writer statt WriteBytesAsync
                using (var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                {
                    using (var outputStream = stream.GetOutputStreamAt(0))
                    {
                        using (var dataWriter = new Windows.Storage.Streams.DataWriter(outputStream))
                        {
                            dataWriter.WriteBytes(imageBytes);
                            await dataWriter.StoreAsync();
                            await outputStream.FlushAsync(); // WICHTIG: Erzwingt das Schreiben der Bytes
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine("Bild erfolgreich gespeichert!");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Bild Speicherfehler: {ex}");
        }
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

    public async void SetProfilePicture(byte[] imageBytes)
    {
        if (imageBytes == null || imageBytes.Length == 0) return;

       // Wir erstellen das BitmapImage auf dem UI-Thread
        var bitmapImage = new BitmapImage();
        
        // Wir nutzen ein InMemoryRandomAccessStream, das unabhängig vom MemoryStream ist
        var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
        await stream.WriteAsync(imageBytes.AsBuffer());
        stream.Seek(0); // Zurück zum Anfang springen!
        await bitmapImage.SetSourceAsync(stream);
        // 4. Assign to the UI control
        ProfilePicture.Source = bitmapImage;
    }

    private async Task OnStartScanClicked(object sender, RoutedEventArgs e)
    {
        if (!setProfile)
        {
            Task t = new Task(async () => {
                byte[] img = await CameraUtil.TakeSelfieAndResizeAsync(200, 200);
                BleBridge.SetImageData(img);
                this.DispatcherQueue.TryEnqueue(() =>
                {
                SetProfilePicture(img);
                });
            });
            t.Start();
            setProfile = true;
            t.Wait();
            
        }
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
