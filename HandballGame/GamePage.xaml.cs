namespace HandballGame;

public partial class GamePage : ContentPage
{
    private readonly GameEngine _engine = new();
    private IDispatcherTimer? _gameTimer;
    private DateTime _lastUpdate;
    private float _lastPanX;
    private bool _initialized;

    public GamePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        GameCanvas.Drawable = new GameDrawable(_engine);
        SetupGestures();
        _lastUpdate = DateTime.UtcNow;
        StartGameTimer();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopGameTimer();
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width > 0 && height > 0 && !_initialized)
        {
            _engine.Initialize((float)width, (float)height);
            _initialized = true;
        }
    }

    private void SetupGestures()
    {
        GameCanvas.GestureRecognizers.Clear();

        var pan = new PanGestureRecognizer();
        pan.PanUpdated += OnPanUpdated;
        GameCanvas.GestureRecognizers.Add(pan);

        var tap = new TapGestureRecognizer();
        tap.Tapped += OnTapped;
        GameCanvas.GestureRecognizers.Add(tap);
    }

    private void StartGameTimer()
    {
        _gameTimer = Dispatcher.CreateTimer();
        _gameTimer.Interval = TimeSpan.FromMilliseconds(33); // ~30 fps
        _gameTimer.Tick += OnGameTick;
        _gameTimer.Start();
    }

    private void StopGameTimer()
    {
        _gameTimer?.Stop();
        _gameTimer = null;
    }

    private void OnGameTick(object? sender, EventArgs e)
    {
        if (!_initialized) return;

        var now = DateTime.UtcNow;
        float delta = Math.Min((float)(now - _lastUpdate).TotalSeconds, 0.1f);
        _lastUpdate = now;

        _engine.Update(delta);

        if (_engine.IsGameOver)
        {
            StopGameTimer();
            ShowGameOver();
            return;
        }

        GameCanvas.Invalidate();
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (!_initialized) return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _lastPanX = (float)e.TotalX;
                break;
            case GestureStatus.Running:
                float deltaX = (float)e.TotalX - _lastPanX;
                _lastPanX = (float)e.TotalX;
                _engine.MoveGoalkeeper(deltaX);
                break;
        }
    }

    private void OnTapped(object? sender, TappedEventArgs e)
    {
        if (!_initialized) return;
        var pos = e.GetPosition(GameCanvas);
        if (pos.HasValue)
            _engine.SetGoalkeeperX((float)pos.Value.X);
    }

    private void ShowGameOver()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            FinalScoreLabel.Text = $"You saved {_engine.SavedShots} shot{(_engine.SavedShots == 1 ? "" : "s")}!";
            GameOverOverlay.IsVisible = true;
            GameCanvas.Invalidate();
        });
    }

    private void OnPlayAgainClicked(object? sender, EventArgs e)
    {
        GameOverOverlay.IsVisible = false;
        _engine.Reset();
        _lastUpdate = DateTime.UtcNow;
        StartGameTimer();
    }

    private async void OnMainMenuClicked(object? sender, EventArgs e)
    {
        GameOverOverlay.IsVisible = false;
        _initialized = false;
        await Shell.Current.GoToAsync("//main");
    }
}
