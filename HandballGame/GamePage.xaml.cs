namespace HandballGame;

public partial class GamePage : ContentPage
{
    private readonly HandballEngine _engine = new();
    private IDispatcherTimer? _gameTimer;
    private DateTime _lastUpdate;
    private bool _initialized;

    public GamePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        GameCanvas.Drawable = new GameDrawable(_engine);
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
        // height includes controls bar (~112 dp); canvas gets the rest
        if (width > 0 && height > 0 && !_initialized)
        {
        // Measure control panel height (2 rows × 52 + padding ≈ 116 dp)
        const double ControlPanelHeight = 116;
        const double MinCanvasHeightRatio = 0.78;
        double canvasH = height - ControlPanelHeight;
        if (canvasH < 100) canvasH = height * MinCanvasHeightRatio;
            _engine.Initialize((float)width, (float)canvasH);
            _initialized = true;
        }
    }

    // ── Game loop ────────────────────────────────────────────────────────────

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
        GameCanvas.Invalidate();

        if (_engine.State == MatchState.MatchOver)
        {
            StopGameTimer();
            ShowMatchOver();
        }
    }

    // ── Button handlers ──────────────────────────────────────────────────────

    private void OnPassUpClicked(object? sender, EventArgs e)
    {
        if (!_initialized) return;
        _engine.TryPassUp();
    }

    private void OnPassDownClicked(object? sender, EventArgs e)
    {
        if (!_initialized) return;
        _engine.TryPassDown();
    }

    private void OnForwardClicked(object? sender, EventArgs e)
    {
        if (!_initialized) return;
        _engine.MoveForward();
    }

    private void OnForwardLeftClicked(object? sender, EventArgs e)
    {
        if (!_initialized) return;
        _engine.MoveForwardLeft();
    }

    private void OnForwardRightClicked(object? sender, EventArgs e)
    {
        if (!_initialized) return;
        _engine.MoveForwardRight();
    }

    private void OnShootClicked(object? sender, EventArgs e)
    {
        if (!_initialized) return;
        _engine.TryShoot();
    }

    // ── Overlays ─────────────────────────────────────────────────────────────

    private void ShowMatchOver()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            bool won = _engine.AttackScore >= _engine.MaxScore;
            GameOverTitle.Text      = won ? "YOU WIN! 🏆" : "DEFENSE WINS";
            GameOverTitle.TextColor = won ? Colors.Gold : Colors.OrangeRed;
            FinalScoreLabel.Text    =
                $"{_engine.AttackScore} – {_engine.DefendScore}";
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
