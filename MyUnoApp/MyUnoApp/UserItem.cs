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

    public DateTime creation { get; set; }

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
            System.Diagnostics.Debug.WriteLine($"Image set, Size: {data.Length} Bytes");
        }
        catch (Exception e)
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
        creation = DateTime.Now;
        if(!IsOnline)
        {
            IsOnline = true;
            OnPropertyChanged(nameof(IsOnline));
        }
    }

    public void SetOffline()
    {
        if (IsOnline)
        {
            IsOnline = false;
            Console.WriteLine($"User {UserId} offline");
            OnPropertyChanged(nameof(IsOnline));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

}
