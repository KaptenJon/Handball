namespace HandballGame;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnPlayClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("game");
    }
}

