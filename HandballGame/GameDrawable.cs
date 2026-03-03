namespace HandballGame;

public class GameDrawable : IDrawable
{
    private readonly HandballEngine _engine;

    public GameDrawable(HandballEngine engine) => _engine = engine;

    public void Draw(ICanvas canvas, RectF dirty)
    {
        DrawField(canvas, dirty);
        DrawGoal(canvas, top: true);
        DrawGoal(canvas, top: false);
        DrawArcs(canvas);
        DrawCenterLine(canvas);

        // Defending team drawn first (behind attacking)
        foreach (var p in _engine.DefendingTeam)
            DrawPlayer(canvas, p);

        foreach (var p in _engine.AttackingTeam)
            DrawPlayer(canvas, p);

        DrawBall(canvas);
        DrawHUD(canvas, dirty);
        DrawResultMessage(canvas, dirty);
    }

    // ── Field ────────────────────────────────────────────────────────────────

    private void DrawField(ICanvas canvas, RectF dirty)
    {
        // Base green
        canvas.FillColor = Color.FromArgb("#2E7D32");
        canvas.FillRectangle(dirty);

        // Alternating lighter stripes (horizontal bands)
        canvas.FillColor = Color.FromArgb("#FFFFFF08");
        float stripeH = _engine.FieldHeight / 10f;
        for (int i = 0; i < 10; i += 2)
            canvas.FillRectangle(0, i * stripeH, _engine.FieldWidth, stripeH);

        // Boundary lines
        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = 2f;
        float lm = _engine.FieldWidth * 0.02f;
        canvas.DrawRectangle(lm, _engine.TopGoalY + 4f,
            _engine.FieldWidth - lm * 2f,
            _engine.BottomGoalY - _engine.TopGoalY - 8f);
    }

    private void DrawArcs(ICanvas canvas)
    {
        float cx = _engine.FieldWidth / 2f;
        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = 1.5f;

        // --- Top goal arcs ---
        float r6t = _engine.Top6mRadius;
        float r9t = _engine.Top9mRadius;
        float goalY = _engine.TopGoalY;

        // 6 m arc (dashed style via thinner line)
        canvas.DrawArc(cx - r6t, goalY - r6t, r6t * 2f, r6t * 2f,
            0f, 180f, false, false);

        // 9 m arc (free-throw line)
        canvas.StrokeColor = Color.FromArgb("#FFFFFFCC");
        canvas.DrawArc(cx - r9t, goalY - r9t, r9t * 2f, r9t * 2f,
            0f, 180f, false, false);

        // --- Bottom goal arcs ---
        float r6b = _engine.Bottom6mRadius;
        float byY  = _engine.BottomGoalY;

        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = 1.5f;
        canvas.DrawArc(cx - r6b, byY - r6b, r6b * 2f, r6b * 2f,
            180f, 360f, false, false);

        canvas.StrokeColor = Color.FromArgb("#FFFFFFCC");
        canvas.DrawArc(cx - r6b * 1.5f, byY - r6b * 1.5f, r6b * 3f, r6b * 3f,
            180f, 360f, false, false);
    }

    private void DrawCenterLine(ICanvas canvas)
    {
        float lm = _engine.FieldWidth * 0.02f;
        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = 2f;
        canvas.DrawLine(lm, _engine.CenterY, _engine.FieldWidth - lm, _engine.CenterY);

        // Center circle
        float cr = _engine.FieldWidth * 0.08f;
        canvas.DrawCircle(_engine.FieldWidth / 2f, _engine.CenterY, cr);

        // Center dot
        canvas.FillColor = Colors.White;
        canvas.FillCircle(_engine.FieldWidth / 2f, _engine.CenterY, 4f);
    }

    // ── Goals ────────────────────────────────────────────────────────────────

    private void DrawGoal(ICanvas canvas, bool top)
    {
        float left  = top ? _engine.TopGoalLeft  : _engine.BottomGoalLeft;
        float right = top ? _engine.TopGoalRight : _engine.BottomGoalRight;
        float lineY = top ? _engine.TopGoalY     : _engine.BottomGoalY;
        float gw    = right - left;
        float depth = 22f;

        // Goal "box" extends away from the field
        float boxTop    = top ? lineY - depth : lineY;
        float boxBottom = top ? lineY         : lineY + depth;

        // Net fill
        canvas.FillColor = Color.FromArgb("#FFFFFF25");
        canvas.FillRectangle(left, boxTop, gw, depth);

        // Net grid
        canvas.StrokeColor = Color.FromArgb("#FFFFFF55");
        canvas.StrokeSize = 0.8f;
        for (int i = 1; i <= 5; i++)
        {
            float x = left + gw * i / 6f;
            canvas.DrawLine(x, boxTop, x, boxBottom);
        }
        for (int i = 1; i <= 2; i++)
        {
            float y = boxTop + depth * i / 3f;
            canvas.DrawLine(left, y, right, y);
        }

        // Posts
        float postW = 6f;
        canvas.FillColor = Colors.White;
        canvas.FillRectangle(left - postW / 2f, boxTop, postW, depth);
        canvas.FillRectangle(right - postW / 2f, boxTop, postW, depth);

        // Crossbar
        canvas.FillRectangle(left - postW / 2f,
            top ? lineY - postW : lineY,
            gw + postW, postW);

        // Red accent on posts
        canvas.FillColor = Color.FromArgb("#FFCC0000");
        canvas.FillRectangle(left - postW / 2f, boxTop, postW / 3f, depth);
        canvas.FillRectangle(right - postW / 6f, boxTop, postW / 3f, depth);
    }

    // ── Players ──────────────────────────────────────────────────────────────

    private static readonly Color GoalkeeperColor = Color.FromArgb("#F9A825");
    private static readonly Color AttackingColor  = Color.FromArgb("#1565C0");
    private static readonly Color DefendingColor  = Color.FromArgb("#C62828");

    private static Color PlayerFillColor(HandballPlayer p) =>
        p.Role == PlayerRole.Goalkeeper ? GoalkeeperColor :
        p.Side == TeamSide.Attacking    ? AttackingColor  : DefendingColor;

    private void DrawPlayer(ICanvas canvas, HandballPlayer p)
    {
        float r = p.Radius;

        // Shadow
        canvas.FillColor = Color.FromArgb("#00000040");
        canvas.FillCircle(p.X + 2f, p.Y + 3f, r);

        // Glow ring for ball carrier
        if (p.HasBall)
        {
            canvas.StrokeColor = Colors.Yellow;
            canvas.StrokeSize = 3f;
            canvas.DrawCircle(p.X, p.Y, r + 3f);
        }

        // Body fill
        canvas.FillColor = PlayerFillColor(p);
        canvas.FillCircle(p.X, p.Y, r);

        // Outline
        canvas.StrokeColor = p.HasBall ? Colors.Yellow : Color.FromArgb("#FFFFFF80");
        canvas.StrokeSize = p.HasBall ? 2f : 1f;
        canvas.DrawCircle(p.X, p.Y, r);

        // Number
        canvas.FontColor = Colors.White;
        canvas.FontSize = 11f;
        canvas.DrawString(p.Number.ToString(), p.X, p.Y + 1f, HorizontalAlignment.Center);
    }

    // ── Ball ─────────────────────────────────────────────────────────────────

    private void DrawBall(ICanvas canvas)
    {
        var b = _engine.Ball;

        // Shadow
        canvas.FillColor = Color.FromArgb("#00000055");
        canvas.FillEllipse(b.X - b.Radius * 0.8f + 3f, b.Y - b.Radius * 0.4f + 3f,
            b.Radius * 1.6f, b.Radius * 0.8f);

        // Body
        canvas.FillColor = Color.FromArgb("#F5E8D0");
        canvas.FillCircle(b.X, b.Y, b.Radius);

        // Seams
        canvas.StrokeColor = Color.FromArgb("#4A90D9");
        canvas.StrokeSize = 1.2f;
        canvas.DrawCircle(b.X, b.Y, b.Radius);
        canvas.DrawLine(b.X - b.Radius, b.Y, b.X + b.Radius, b.Y);
        canvas.DrawArc(b.X - b.Radius * 0.5f, b.Y - b.Radius,
            b.Radius, b.Radius * 2f, 0f, 180f, false, false);

        // Highlight
        canvas.FillColor = Color.FromArgb("#FFFFFF90");
        canvas.FillCircle(b.X - b.Radius * 0.3f, b.Y - b.Radius * 0.35f, b.Radius * 0.28f);
    }

    // ── HUD ──────────────────────────────────────────────────────────────────

    private void DrawHUD(ICanvas canvas, RectF dirty)
    {
        // Top score banner
        float bandH = _engine.TopGoalY - 2f;
        if (bandH < 10f) bandH = 10f;

        canvas.FillColor = Color.FromArgb("#CC1A237E");
        canvas.FillRectangle(0f, 0f, dirty.Width, bandH);

        canvas.FontColor = Colors.White;
        canvas.FontSize = 15f;
        canvas.DrawString($"YOU  {_engine.AttackScore}",
            8f, bandH * 0.65f, HorizontalAlignment.Left);

        canvas.FontColor = Color.FromArgb("#FFD54F");
        canvas.FontSize = 13f;
        canvas.DrawString($"First to {_engine.MaxScore}",
            dirty.Width / 2f, bandH * 0.65f, HorizontalAlignment.Center);

        canvas.FontColor = Colors.White;
        canvas.FontSize = 15f;
        canvas.DrawString($"{_engine.DefendScore}  DEF",
            dirty.Width - 8f, bandH * 0.65f, HorizontalAlignment.Right);

        // Carrier hint
        var carrier = _engine.BallCarrier;
        if (carrier != null && _engine.State == MatchState.Playing)
        {
            canvas.FontColor = Color.FromArgb("#A5D6A7");
            canvas.FontSize = 11f;
            canvas.DrawString($"#{carrier.Number} has ball",
                dirty.Width / 2f, dirty.Height - 2f, HorizontalAlignment.Center);
        }
    }

    // ── Result flash ─────────────────────────────────────────────────────────

    private void DrawResultMessage(ICanvas canvas, RectF dirty)
    {
        if (string.IsNullOrEmpty(_engine.ResultMessage)) return;

        canvas.FontSize = 46f;
        canvas.FontColor = _engine.State == MatchState.GoalCelebration
            ? Colors.LimeGreen
            : Colors.OrangeRed;

        canvas.DrawString(_engine.ResultMessage,
            dirty.Width / 2f, dirty.Height * 0.44f,
            HorizontalAlignment.Center);
    }
}

