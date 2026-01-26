namespace MyUnoApp;

public sealed partial class MainPage : Page
{
    public MainPage()
    {

        this.InitializeComponent();
        UserList.ItemsSource = new List<UserItem> {
            new UserItem("Test1", "23", "https://via.placeholder.com") { IsOnline = true },
            new UserItem("Test2", "25", "https://via.placeholder.com") { IsOnline = false }
        };
    }
}
