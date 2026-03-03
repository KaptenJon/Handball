namespace HandballGame;

public class Ball
{
    public float X { get; set; }
    public float Y { get; set; }
    public float TargetX { get; set; }
    public float TargetY { get; set; }
    public float Radius { get; set; } = 18f;
    public bool IsActive { get; set; }
    public bool IsSaved { get; set; }
    public bool IsGoal { get; set; }
}

public class Goalkeeper
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; } = 80f;
    public float Height { get; set; } = 40f;
}

public class GameEngine
{
    private readonly Random _random = new();

    public float FieldWidth { get; private set; }
    public float FieldHeight { get; private set; }

    public float GoalLeft { get; private set; }
    public float GoalRight { get; private set; }
    public float GoalTop { get; private set; }
    public float GoalBottom { get; private set; }

    public Ball Ball { get; private set; } = new();
    public Goalkeeper Goalkeeper { get; private set; } = new();

    public int SavedShots { get; private set; }
    public int GoalsConceded { get; private set; }
    public int MaxLives { get; } = 3;
    public bool IsGameOver => GoalsConceded >= MaxLives;

    private float _shotCooldown;
    private const float ShotCooldownDuration = 1.5f;
    private float _resultDisplayTimer;
    private const float ResultDisplayDuration = 0.9f;

    public float BallSpeed { get; private set; } = 280f;

    public void Initialize(float width, float height)
    {
        FieldWidth = width;
        FieldHeight = height;

        float goalWidth = width * 0.62f;
        GoalLeft = (width - goalWidth) / 2f;
        GoalRight = GoalLeft + goalWidth;
        GoalTop = height * 0.07f;
        GoalBottom = height * 0.22f;

        Goalkeeper.X = width / 2f;
        Goalkeeper.Y = GoalBottom - Goalkeeper.Height / 2f - 4f;
        Goalkeeper.Width = goalWidth * 0.30f;

        _shotCooldown = 1.0f;
        Ball = new Ball();
    }

    public void Update(float deltaSeconds)
    {
        if (IsGameOver) return;

        if (Ball.IsActive)
        {
            float dx = Ball.TargetX - Ball.X;
            float dy = Ball.TargetY - Ball.Y;
            float dist = MathF.Sqrt(dx * dx + dy * dy);

            if (dist > 2f)
            {
                float step = BallSpeed * deltaSeconds;
                Ball.X += dx / dist * step;
                Ball.Y += dy / dist * step;
            }
            else
            {
                Ball.X = Ball.TargetX;
                Ball.Y = Ball.TargetY;
                Ball.IsActive = false;

                if (IsBallSaved())
                {
                    Ball.IsSaved = true;
                    SavedShots++;
                    BallSpeed = 280f + (SavedShots / 5) * 70f;
                }
                else
                {
                    Ball.IsGoal = true;
                    GoalsConceded++;
                }

                _resultDisplayTimer = ResultDisplayDuration;
            }
        }
        else
        {
            if (_resultDisplayTimer > 0f)
            {
                _resultDisplayTimer -= deltaSeconds;
                if (_resultDisplayTimer <= 0f)
                {
                    Ball.IsSaved = false;
                    Ball.IsGoal = false;
                    _shotCooldown = ShotCooldownDuration;
                }
            }
            else if (_shotCooldown > 0f)
            {
                _shotCooldown -= deltaSeconds;
                if (_shotCooldown <= 0f)
                    FireBall();
            }
        }
    }

    private void FireBall()
    {
        float startVariance = FieldWidth * 0.15f;
        Ball.X = FieldWidth / 2f + (_random.NextSingle() - 0.5f) * startVariance * 2f;
        Ball.Y = FieldHeight * 0.82f;

        float margin = Ball.Radius + 6f;
        Ball.TargetX = GoalLeft + margin + _random.NextSingle() * (GoalRight - GoalLeft - margin * 2f);
        Ball.TargetY = GoalBottom - 8f;

        Ball.IsActive = true;
        Ball.IsSaved = false;
        Ball.IsGoal = false;
        Ball.Radius = 18f;
    }

    private bool IsBallSaved()
    {
        float gkLeft = Goalkeeper.X - Goalkeeper.Width / 2f;
        float gkRight = Goalkeeper.X + Goalkeeper.Width / 2f;
        float gkTop = Goalkeeper.Y - Goalkeeper.Height / 2f;
        float gkBottom = Goalkeeper.Y + Goalkeeper.Height / 2f;

        return Ball.X + Ball.Radius >= gkLeft &&
               Ball.X - Ball.Radius <= gkRight &&
               Ball.Y + Ball.Radius >= gkTop &&
               Ball.Y - Ball.Radius <= gkBottom;
    }

    public void MoveGoalkeeper(float deltaX)
    {
        Goalkeeper.X += deltaX;
        ClampGoalkeeper();
    }

    public void SetGoalkeeperX(float x)
    {
        Goalkeeper.X = x;
        ClampGoalkeeper();
    }

    private void ClampGoalkeeper()
    {
        float half = Goalkeeper.Width / 2f;
        Goalkeeper.X = Math.Clamp(Goalkeeper.X, GoalLeft + half, GoalRight - half);
    }

    public void Reset()
    {
        SavedShots = 0;
        GoalsConceded = 0;
        BallSpeed = 280f;
        Ball = new Ball();
        Goalkeeper.X = FieldWidth / 2f;
        _shotCooldown = 1.0f;
        _resultDisplayTimer = 0f;
    }
}
