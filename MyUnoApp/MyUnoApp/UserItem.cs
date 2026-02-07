using System.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;

namespace MyUnoApp;

[Microsoft.UI.Xaml.Data.Bindable]
public class UserItem : INotifyPropertyChanged
{
    public int UserId { get; set; } = 0;
    public string Name { get; set; } = string.Empty;
    public string Age { get; set; } = string.Empty;
    public bool IsOnline { get; set; } = false;
    private Task _offlineTimer;
    private CancellationTokenSource _cts;
    public ImageSource Image { get; set; }

    public async void SetImage(byte[] data)
    {
        try
        {
            var bitmap = new BitmapImage();
            using (var ms = new MemoryStream(data))
            {
                await bitmap.SetSourceAsync(ms.AsRandomAccessStream());
            }
            Image = bitmap;
            OnPropertyChanged(nameof(Image));
        }catch (Exception e)
        {
            Console.WriteLine("SetImage error: " + e.Message);
        }
    }

    public UserItem()
    {

    }

    public UserItem(string Name, string Age, string image)
    {
        this.Name = Name;
        this.Age = Age;
    }

    public UserItem(int userId, string Name, string Age, string image)
    {
        this.UserId = userId;
        this.Name = Name;
        this.Age = Age;

        Updated();

    }

    public void Updated()
    {
        _cts = new CancellationTokenSource();
        _offlineTimer = new Task(async () => {
            try
            {
                await Task.Delay(3000, _cts.Token);
                var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread()
                         ?? MainPage.GetUserList().DispatcherQueue; // Falls GetForCurrentThread null ist

                dispatcher.TryEnqueue(() =>
                {
                    Console.WriteLine("UI Offline");
                    IsOnline = false;
                    OnPropertyChanged(nameof(IsOnline));
                });
            }
            catch (Exception e)
            {
                //Console.WriteLine("UI Online");
            }
        });
        _offlineTimer.Start();
    }

    public void Discard()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

}
