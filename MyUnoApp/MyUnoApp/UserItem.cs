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
    public bool HasIcebreaker { get; set; } = false;

    public float Distance { get; set; } = 100.0f;

    public string DistanceForm { get; set; } = "";

    private Task _offlineTimer;
    private CancellationTokenSource _cts;
    public ImageSource Image { get; set; }

    public DateTime creation { get; set; }


    public async void SetIcebreakerReceived()
    {
        Console.WriteLine($"Set Ice-breaker from {Name} ({UserId})");
        HasIcebreaker = true;
        OnPropertyChanged(nameof(HasIcebreaker));
    }
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

    public string GetFormatedDistance(float dist)
    {
        return $"{Distance:N2}";
    }

    public void UpdateDist(float dist)
    {
        if (Distance != dist)
        {
            string DistanceFormOld = $"{Distance:N2}";
            string DistanceFormNew = $"{dist:N2}";
            Distance = dist;
            if (!DistanceFormNew.Equals(DistanceFormOld))
            {
                DistanceForm = DistanceFormNew;
                OnPropertyChanged(nameof(DistanceForm));
            }
        }
    }

    public void UpdateByItem(in UserItem other)
    {
        UserId = other.UserId;
        Name = other.Name;
        Age = other.Age;
        this.creation = other.creation;
        bool ud = false;
        if(Distance != other.Distance)
        {
            Distance = other.Distance;
            OnPropertyChanged(nameof(Distance));
        }
        if (Image != other.Image)
        {
            Image = other.Image;
            OnPropertyChanged(nameof(Image));
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
