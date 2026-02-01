using System.ComponentModel;

namespace MyUnoApp;


public class UserItem : INotifyPropertyChanged
{
    public int UserId { get; set; } = 0;
    public string Name { get; set; } = string.Empty;
    public string Age { get; set; } = string.Empty;
    public bool IsOnline { get; set; } = false;
    private readonly Task _offlineTimer;
    private CancellationTokenSource _cts;
    public string ImageUrl { get; set; } = string.Empty;

    public UserItem()
    {

    }

    public UserItem(string Name, string Age, string image)
    {
        this.Name = Name;
        this.Age = Age;
        this.ImageUrl = image;
    }

    public UserItem(int userId, string Name, string Age, string image)
    {
        this.UserId = userId;
        this.Name = Name;
        this.Age = Age;
        this.ImageUrl = image;

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
            catch (TaskCanceledException)
            {
                Console.WriteLine("UI Online");
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
