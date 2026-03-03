namespace HandballGame;

public enum PlayerRole { Goalkeeper, LeftWing, RightWing, LeftBack, Center, RightBack, Pivot }
public enum TeamSide { Attacking, Defending }
public enum MatchState { Playing, BallInFlight, GoalCelebration, Intercepted, MatchOver }

public class HandballPlayer
{
    public float X { get; set; }
    public float Y { get; set; }
    public float HomeX { get; set; }
    public float HomeY { get; set; }
    public PlayerRole Role { get; set; }
    public TeamSide Side { get; set; }
    public bool HasBall { get; set; }
    public bool IsRetreating { get; set; }
    public int Number { get; set; }
    public float Radius { get; set; } = 14f;

    public float DistanceTo(float x, float y) =>
        MathF.Sqrt((X - x) * (X - x) + (Y - y) * (Y - y));

    public float DistanceTo(HandballPlayer other) => DistanceTo(other.X, other.Y);
}

public class GameBall
{
    public float X { get; set; }
    public float Y { get; set; }
    public float TargetX { get; set; }
    public float TargetY { get; set; }
    public float Radius { get; set; } = 9f;
    public bool IsMoving { get; set; }
    public HandballPlayer? DestinationPlayer { get; set; }
}

public class HandballEngine
{
    private readonly Random _random = new();

    // Field geometry
    public float FieldWidth { get; private set; }
    public float FieldHeight { get; private set; }
    public float TopGoalLeft { get; private set; }
    public float TopGoalRight { get; private set; }
    public float TopGoalY { get; private set; }
    public float BottomGoalLeft { get; private set; }
    public float BottomGoalRight { get; private set; }
    public float BottomGoalY { get; private set; }
    public float Top6mRadius { get; private set; }
    public float Top9mRadius { get; private set; }
    public float Bottom6mRadius { get; private set; }
    public float CenterY { get; private set; }

    public List<HandballPlayer> AttackingTeam { get; } = [];
    public List<HandballPlayer> DefendingTeam { get; } = [];
    public GameBall Ball { get; } = new();

    public int AttackScore { get; private set; }
    public int DefendScore { get; private set; }
    public int MaxScore { get; } = 7;

    public MatchState State { get; private set; } = MatchState.Playing;
    public string ResultMessage { get; private set; } = "";

    private float _stateTimer;
    private const float CelebrationDuration = 1.5f;
    private const float ResetPauseDuration = 1.2f;

    private const float BallPassSpeed = 420f;
    private const float BallShotSpeed = 520f;
    private const float MoveStep = 38f;
    private const float DefenderReturnSpeed = 55f;
    private const float DefenderPursueSpeed = 62f;
    private const float RetreatingSpeed = 90f;
    private const float GKTrackSpeed = 110f;
    private const float GKReactSpeed = 270f;
    private const float InterceptionRadius = 28f;

    public HandballPlayer? BallCarrier =>
        AttackingTeam.FirstOrDefault(p => p.HasBall);

    public bool IsMatchOver => AttackScore >= MaxScore || DefendScore >= MaxScore;

    // ── Initialise ──────────────────────────────────────────────────────────

    public void Initialize(float width, float height)
    {
        FieldWidth = width;
        FieldHeight = height;

        float goalWidth = width * 0.55f;
        float cx = width / 2f;

        TopGoalLeft  = cx - goalWidth / 2f;
        TopGoalRight = cx + goalWidth / 2f;
        TopGoalY     = height * 0.04f;

        BottomGoalLeft  = cx - goalWidth / 2f;
        BottomGoalRight = cx + goalWidth / 2f;
        BottomGoalY     = height * 0.96f;

        float mY = height / 40f;   // pixels per meter (court is 40 m tall)
        Top6mRadius    = 6f * mY;
        Top9mRadius    = 9f * mY;
        Bottom6mRadius = 6f * mY;
        CenterY = height * 0.50f;

        InitializePlayers();
        GiveBallToCenter();
        State = MatchState.Playing;
        ResultMessage = "";
    }

    private void InitializePlayers()
    {
        AttackingTeam.Clear();
        DefendingTeam.Clear();

        float w = FieldWidth;
        float h = FieldHeight;
        float mY = h / 40f;

        // ── Attacking team (attacks upward toward y=TopGoalY) ──────────────
        // Goalkeeper stays in own (bottom) goal
        Add(AttackingTeam, PlayerRole.Goalkeeper, TeamSide.Attacking, w * 0.50f, BottomGoalY - 6f, 1);
        // Three backs at ~12 m from bottom (28 m from top)
        Add(AttackingTeam, PlayerRole.LeftBack,  TeamSide.Attacking, w * 0.22f, h - 12f * mY, 4);
        Add(AttackingTeam, PlayerRole.Center,    TeamSide.Attacking, w * 0.50f, h - 12f * mY, 10);
        Add(AttackingTeam, PlayerRole.RightBack, TeamSide.Attacking, w * 0.78f, h - 12f * mY, 8);
        // Two wings at ~17 m from bottom
        Add(AttackingTeam, PlayerRole.LeftWing,  TeamSide.Attacking, w * 0.06f, h - 17f * mY, 11);
        Add(AttackingTeam, PlayerRole.RightWing, TeamSide.Attacking, w * 0.94f, h - 17f * mY, 7);
        // Pivot just outside the opponent's 6 m arc (clamped below)
        float pivotY = TopGoalY + Top6mRadius + 18f;
        Add(AttackingTeam, PlayerRole.Pivot, TeamSide.Attacking, w * 0.50f, pivotY, 5);

        // ── Defending team (defends the top goal) ──────────────────────────
        // GK in top goal
        Add(DefendingTeam, PlayerRole.Goalkeeper, TeamSide.Defending, w * 0.50f, TopGoalY + 6f, 1);
        // 3 front defenders just outside their own 6 m line
        float def6Y = TopGoalY + Top6mRadius + 16f;
        Add(DefendingTeam, PlayerRole.LeftWing,  TeamSide.Defending, w * 0.20f, def6Y, 4);
        Add(DefendingTeam, PlayerRole.Center,    TeamSide.Defending, w * 0.50f, def6Y, 10);
        Add(DefendingTeam, PlayerRole.RightWing, TeamSide.Defending, w * 0.80f, def6Y, 7);
        // 3 back defenders at the 9 m line
        float def9Y = TopGoalY + Top9mRadius;
        Add(DefendingTeam, PlayerRole.LeftBack,  TeamSide.Defending, w * 0.15f, def9Y, 11);
        Add(DefendingTeam, PlayerRole.RightBack, TeamSide.Defending, w * 0.85f, def9Y, 8);
        Add(DefendingTeam, PlayerRole.Pivot,     TeamSide.Defending, w * 0.50f, def9Y + 14f, 5);
    }

    private static void Add(List<HandballPlayer> team, PlayerRole role, TeamSide side,
        float x, float y, int number)
    {
        team.Add(new HandballPlayer
        {
            Role = role, Side = side,
            X = x, Y = y, HomeX = x, HomeY = y,
            Number = number
        });
    }

    private void GiveBallToCenter()
    {
        foreach (var p in AttackingTeam) p.HasBall = false;
        var cb = AttackingTeam.First(p => p.Role == PlayerRole.Center);
        cb.HasBall = true;
        Ball.X = cb.X;
        Ball.Y = cb.Y;
        Ball.IsMoving = false;
        Ball.DestinationPlayer = null;
    }

    // ── Game update ─────────────────────────────────────────────────────────

    public void Update(float deltaSeconds)
    {
        if (IsMatchOver) return;

        switch (State)
        {
            case MatchState.Playing:
                UpdateDefendersAI(deltaSeconds);
                UpdateRetreating(deltaSeconds);
                CheckInterception();
                break;

            case MatchState.BallInFlight:
                UpdateBallInFlight(deltaSeconds);
                UpdateDefendersAI(deltaSeconds);
                UpdateRetreating(deltaSeconds);
                break;

            case MatchState.GoalCelebration:
            case MatchState.Intercepted:
                _stateTimer -= deltaSeconds;
                if (_stateTimer <= 0f) ResetRound();
                break;
        }

        if (IsMatchOver)
        {
            State = MatchState.MatchOver;
            ResultMessage = AttackScore >= MaxScore
                ? $"YOU WIN!\n{AttackScore} – {DefendScore}"
                : $"DEFENSE WINS!\n{AttackScore} – {DefendScore}";
        }
    }

    private void UpdateBallInFlight(float deltaSeconds)
    {
        float dx = Ball.TargetX - Ball.X;
        float dy = Ball.TargetY - Ball.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);

        float speed = Ball.DestinationPlayer != null ? BallPassSpeed : BallShotSpeed;
        float step  = speed * deltaSeconds;

        // Move GK toward shot landing while ball is travelling
        if (Ball.DestinationPlayer == null)
        {
            var gk = DefendingTeam.First(p => p.Role == PlayerRole.Goalkeeper);
            MoveTowardX(gk, Ball.TargetX, GKReactSpeed * deltaSeconds);
            ClampGK(gk);
        }

        if (dist <= step + 1f)
        {
            Ball.X = Ball.TargetX;
            Ball.Y = Ball.TargetY;
            Ball.IsMoving = false;

            if (Ball.DestinationPlayer != null)
            {
                Ball.DestinationPlayer.HasBall = true;
                Ball.DestinationPlayer = null;
                State = MatchState.Playing;
            }
            else
            {
                ResolveShot();
            }
        }
        else
        {
            Ball.X += dx / dist * step;
            Ball.Y += dy / dist * step;
        }
    }

    private void UpdateDefendersAI(float deltaSeconds)
    {
        var carrier = BallCarrier;
        float targetX = carrier?.X ?? FieldWidth / 2f;
        float targetY = carrier?.Y ?? CenterY;

        // GK tracks ball carrier X during open play
        var gk = DefendingTeam.First(p => p.Role == PlayerRole.Goalkeeper);
        if (State == MatchState.Playing)
        {
            MoveTowardX(gk, targetX, GKTrackSpeed * deltaSeconds);
            ClampGK(gk);
        }

        // Outfield defenders: closest one pursues, others return home
        var outfield = DefendingTeam.Where(p => p.Role != PlayerRole.Goalkeeper).ToList();

        HandballPlayer? pursuer = null;
        float minDist = float.MaxValue;
        foreach (var d in outfield)
        {
            float dist = d.DistanceTo(targetX, targetY);
            if (dist < minDist) { minDist = dist; pursuer = d; }
        }

        foreach (var d in outfield)
        {
            if (d == pursuer && carrier != null)
                MoveToward(d, targetX, targetY, DefenderPursueSpeed * deltaSeconds);
            else
                MoveToward(d, d.HomeX, d.HomeY, DefenderReturnSpeed * deltaSeconds);
        }
    }

    private void UpdateRetreating(float deltaSeconds)
    {
        foreach (var p in AttackingTeam.Where(p => p.IsRetreating))
        {
            MoveToward(p, p.HomeX, p.HomeY, RetreatingSpeed * deltaSeconds);
            if (p.DistanceTo(p.HomeX, p.HomeY) < 4f)
            {
                p.X = p.HomeX;
                p.Y = p.HomeY;
                p.IsRetreating = false;
            }
        }
    }

    private void CheckInterception()
    {
        var carrier = BallCarrier;
        if (carrier == null) return;

        foreach (var d in DefendingTeam.Where(p => p.Role != PlayerRole.Goalkeeper))
        {
            if (d.DistanceTo(carrier) < InterceptionRadius)
            {
                carrier.HasBall = false;
                Ball.X = d.X;
                Ball.Y = d.Y;
                DefendScore++;
                ResultMessage = "INTERCEPTED!";
                State = MatchState.Intercepted;
                _stateTimer = ResetPauseDuration;
                return;
            }
        }
    }

    private void ResolveShot()
    {
        var gk = DefendingTeam.First(p => p.Role == PlayerRole.Goalkeeper);
        float saveHalf = gk.Radius + 12f;

        if (Math.Abs(gk.X - Ball.TargetX) <= saveHalf)
        {
            DefendScore++;
            ResultMessage = "SAVED!";
            State = MatchState.Intercepted;
            _stateTimer = ResetPauseDuration;
        }
        else
        {
            AttackScore++;
            ResultMessage = "GOAL!";
            State = MatchState.GoalCelebration;
            _stateTimer = CelebrationDuration;
        }
    }

    private void ResetRound()
    {
        foreach (var p in AttackingTeam.Concat(DefendingTeam))
        {
            p.X = p.HomeX;
            p.Y = p.HomeY;
            p.HasBall = false;
            p.IsRetreating = false;
        }
        GiveBallToCenter();
        State = MatchState.Playing;
        ResultMessage = "";
    }

    // ── Player actions (called by UI) ────────────────────────────────────────

    public void MoveForward()      => ApplyStep(0f,  -1f);
    public void MoveForwardLeft()  => ApplyStep(-1f, -1f);
    public void MoveForwardRight() => ApplyStep(1f,  -1f);

    private void ApplyStep(float dirX, float dirY)
    {
        if (State != MatchState.Playing) return;
        var carrier = BallCarrier;
        if (carrier == null || carrier.Role == PlayerRole.Goalkeeper) return;

        float len = MathF.Sqrt(dirX * dirX + dirY * dirY);
        carrier.X += dirX / len * MoveStep;
        carrier.Y += dirY / len * MoveStep;
        ClampToField(carrier);
        Ball.X = carrier.X;
        Ball.Y = carrier.Y;
    }

    public void TryPassUp()
    {
        if (State != MatchState.Playing) return;
        var carrier = BallCarrier;
        if (carrier == null) return;

        var candidates = AttackingTeam
            .Where(p => p != carrier && p.Role != PlayerRole.Goalkeeper)
            .ToList();

        // Prefer the player furthest toward the goal (smallest Y)
        var target = candidates.Where(p => p.Y < carrier.Y)
                         .OrderBy(p => p.Y)
                         .FirstOrDefault()
                     ?? candidates.OrderBy(p => p.DistanceTo(carrier)).FirstOrDefault();
        if (target != null) FirePass(carrier, target);
    }

    public void TryPassDown()
    {
        if (State != MatchState.Playing) return;
        var carrier = BallCarrier;
        if (carrier == null) return;

        var candidates = AttackingTeam
            .Where(p => p != carrier && p.Role != PlayerRole.Goalkeeper)
            .ToList();

        // Prefer the player furthest from the goal (largest Y)
        var target = candidates.Where(p => p.Y > carrier.Y)
                         .OrderByDescending(p => p.Y)
                         .FirstOrDefault()
                     ?? candidates.OrderBy(p => p.DistanceTo(carrier)).FirstOrDefault();
        if (target != null) FirePass(carrier, target);
    }

    private void FirePass(HandballPlayer from, HandballPlayer to)
    {
        from.HasBall = false;
        from.IsRetreating = true;
        Ball.TargetX = to.X;
        Ball.TargetY = to.Y;
        Ball.IsMoving = true;
        Ball.DestinationPlayer = to;
        State = MatchState.BallInFlight;
    }

    public void TryShoot()
    {
        if (State != MatchState.Playing) return;
        var carrier = BallCarrier;
        if (carrier == null || carrier.Role == PlayerRole.Goalkeeper) return;

        // Aim for the side furthest from the GK
        var gk = DefendingTeam.First(p => p.Role == PlayerRole.Goalkeeper);
        float goalWidth = TopGoalRight - TopGoalLeft;
        float margin = 16f;
        float gkOffset = gk.X - (TopGoalLeft + goalWidth / 2f);

        float aimX;
        if (Math.Abs(gkOffset) > 18f)
        {
            aimX = gkOffset > 0
                ? TopGoalLeft + margin + _random.NextSingle() * goalWidth * 0.38f
                : TopGoalRight - margin - _random.NextSingle() * goalWidth * 0.38f;
        }
        else
        {
            aimX = TopGoalLeft + margin + _random.NextSingle() * (goalWidth - margin * 2f);
        }

        carrier.HasBall = false;
        carrier.IsRetreating = true;
        Ball.TargetX = aimX;
        Ball.TargetY = TopGoalY + 8f;
        Ball.IsMoving = true;
        Ball.DestinationPlayer = null;
        State = MatchState.BallInFlight;
    }

    public void Reset()
    {
        AttackScore = 0;
        DefendScore = 0;
        State = MatchState.Playing;
        ResultMessage = "";
        _stateTimer = 0f;
        InitializePlayers();
        GiveBallToCenter();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private void ClampToField(HandballPlayer p)
    {
        float r = p.Radius;
        p.X = Math.Clamp(p.X, r, FieldWidth - r);
        // Cannot enter the opponent's 6 m zone; clamped to just outside the arc
        float minY = TopGoalY + Top6mRadius + r;
        float maxY = BottomGoalY - r;
        p.Y = Math.Clamp(p.Y, minY, maxY);
    }

    private void ClampGK(HandballPlayer gk)
    {
        float half = gk.Radius + 10f;
        gk.X = Math.Clamp(gk.X, TopGoalLeft + half, TopGoalRight - half);
        gk.Y = gk.HomeY;
    }

    private static void MoveToward(HandballPlayer p, float tx, float ty, float maxStep)
    {
        float dx = tx - p.X;
        float dy = ty - p.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        if (dist < 0.5f) return;
        float step = Math.Min(dist, maxStep);
        p.X += dx / dist * step;
        p.Y += dy / dist * step;
    }

    private static void MoveTowardX(HandballPlayer p, float tx, float maxStep)
    {
        float dx = tx - p.X;
        float absDx = Math.Abs(dx);
        if (absDx < 0.5f) return;
        p.X += (dx > 0 ? 1f : -1f) * Math.Min(absDx, maxStep);
    }
}
