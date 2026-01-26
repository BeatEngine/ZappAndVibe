namespace MyUnoApp;


public class UserItem
{
    public string Name { get; set; } = string.Empty;
    public string Age { get; set; } = string.Empty;
    public bool IsOnline { get; set; } = false;
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
}
