namespace HandballGame;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("game", typeof(GamePage));
    }
}

