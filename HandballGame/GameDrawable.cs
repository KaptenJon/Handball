namespace HandballGame;

public class GameDrawable : IDrawable
{
    private readonly GameEngine _engine;

    public GameDrawable(GameEngine engine)
    {
        _engine = engine;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        DrawField(canvas, dirtyRect);
        DrawGoal(canvas);
        DrawGoalkeeper(canvas);

        if (_engine.Ball.IsActive || _engine.Ball.IsSaved || _engine.Ball.IsGoal)
            DrawBall(canvas);

        DrawHUD(canvas, dirtyRect);
        DrawResultFlash(canvas, dirtyRect);
    }

    private void DrawField(ICanvas canvas, RectF rect)
    {
        // Green pitch
        canvas.FillColor = Color.FromArgb("#2E7D32");
        canvas.FillRectangle(rect);

        // Pitch boundary line
        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = 2f;
        float margin = _engine.FieldWidth * 0.05f;
        canvas.DrawRectangle(margin, _engine.GoalBottom + 20f,
            _engine.FieldWidth - margin * 2f, _engine.FieldHeight * 0.62f);

        // 6-metre line arc (penalty area)
        float centreX = _engine.FieldWidth / 2f;
        float lineY = _engine.GoalBottom + 20f;
        canvas.DrawArc(centreX - _engine.FieldWidth * 0.30f,
            lineY - _engine.FieldHeight * 0.04f,
            _engine.FieldWidth * 0.60f,
            _engine.FieldHeight * 0.14f,
            180f, 360f, false, false);

        // Centre circle
        float centreLineY = _engine.FieldHeight * 0.63f;
        canvas.DrawLine(margin, centreLineY, _engine.FieldWidth - margin, centreLineY);
        canvas.DrawCircle(centreX, centreLineY, _engine.FieldWidth * 0.09f);
        canvas.FillColor = Colors.White;
        canvas.FillCircle(centreX, centreLineY, 4f);
    }

    private void DrawGoal(ICanvas canvas)
    {
        float postW = 7f;

        // Net fill
        canvas.FillColor = Color.FromArgb("#FFFFFF20");
        canvas.FillRectangle(_engine.GoalLeft, _engine.GoalTop,
            _engine.GoalRight - _engine.GoalLeft, _engine.GoalBottom - _engine.GoalTop);

        // Net grid lines
        canvas.StrokeColor = Color.FromArgb("#FFFFFF55");
        canvas.StrokeSize = 1f;
        float gw = _engine.GoalRight - _engine.GoalLeft;
        float gh = _engine.GoalBottom - _engine.GoalTop;
        for (int i = 1; i <= 5; i++)
        {
            float x = _engine.GoalLeft + gw * i / 6f;
            canvas.DrawLine(x, _engine.GoalTop, x, _engine.GoalBottom);
        }
        for (int i = 1; i <= 3; i++)
        {
            float y = _engine.GoalTop + gh * i / 4f;
            canvas.DrawLine(_engine.GoalLeft, y, _engine.GoalRight, y);
        }

        // Posts (drawn on top of net)
        canvas.FillColor = Colors.White;
        // Left post
        canvas.FillRectangle(_engine.GoalLeft - postW / 2f, _engine.GoalTop,
            postW, gh);
        // Right post
        canvas.FillRectangle(_engine.GoalRight - postW / 2f, _engine.GoalTop,
            postW, gh);
        // Crossbar
        canvas.FillRectangle(_engine.GoalLeft - postW / 2f, _engine.GoalTop,
            gw + postW, postW);

        // Post highlight (3-D effect)
        canvas.FillColor = Color.FromArgb("#FFFF0000");
        canvas.FillRectangle(_engine.GoalLeft - postW / 2f, _engine.GoalTop, postW / 3f, gh);
        canvas.FillRectangle(_engine.GoalRight - postW / 6f, _engine.GoalTop, postW / 3f, gh);
    }

    private void DrawBall(ICanvas canvas)
    {
        var b = _engine.Ball;

        // Shadow
        canvas.FillColor = Color.FromArgb("#00000060");
        canvas.FillEllipse(b.X - b.Radius * 0.8f + 4f, b.Y - b.Radius * 0.4f + 4f,
            b.Radius * 1.6f, b.Radius * 0.8f);

        // Ball body
        canvas.FillColor = Color.FromArgb("#F5E8D0");
        canvas.FillCircle(b.X, b.Y, b.Radius);

        // Seams
        canvas.StrokeColor = Color.FromArgb("#4A90D9");
        canvas.StrokeSize = 1.5f;
        canvas.DrawCircle(b.X, b.Y, b.Radius);
        canvas.DrawLine(b.X - b.Radius, b.Y, b.X + b.Radius, b.Y);
        canvas.DrawArc(b.X - b.Radius * 0.5f, b.Y - b.Radius,
            b.Radius, b.Radius * 2f, 0f, 180f, false, false);

        // Highlight
        canvas.FillColor = Color.FromArgb("#FFFFFF80");
        canvas.FillCircle(b.X - b.Radius * 0.3f, b.Y - b.Radius * 0.35f, b.Radius * 0.25f);
    }

    private void DrawGoalkeeper(ICanvas canvas)
    {
        var gk = _engine.Goalkeeper;
        float left = gk.X - gk.Width / 2f;
        float top = gk.Y - gk.Height / 2f;

        // Body shadow
        canvas.FillColor = Color.FromArgb("#00000050");
        canvas.FillRoundedRectangle(left + 3f, top + 3f, gk.Width, gk.Height, 8f);

        // Jersey
        canvas.FillColor = Color.FromArgb("#F9A825");
        canvas.FillRoundedRectangle(left, top, gk.Width, gk.Height, 8f);

        // Jersey stripe
        canvas.FillColor = Color.FromArgb("#E65100");
        canvas.FillRectangle(gk.X - 4f, top, 8f, gk.Height);

        // Jersey outline
        canvas.StrokeColor = Color.FromArgb("#BF360C");
        canvas.StrokeSize = 2f;
        canvas.DrawRoundedRectangle(left, top, gk.Width, gk.Height, 8f);

        // Gloves
        canvas.FillColor = Color.FromArgb("#FF7043");
        canvas.FillCircle(left - 4f, gk.Y, 7f);
        canvas.FillCircle(left + gk.Width + 4f, gk.Y, 7f);

        // Number
        canvas.FontColor = Colors.White;
        canvas.FontSize = 14f;
        canvas.DrawString("1", gk.X, top + gk.Height * 0.55f, HorizontalAlignment.Center);
    }

    private void DrawHUD(ICanvas canvas, RectF dirtyRect)
    {
        // HUD background band
        canvas.FillColor = Color.FromArgb("#CC1A237E");
        canvas.FillRectangle(0f, 0f, dirtyRect.Width, _engine.GoalTop - 4f);

        // Saves
        canvas.FontColor = Colors.White;
        canvas.FontSize = 18f;
        canvas.DrawString($"SAVES: {_engine.SavedShots}",
            12f, _engine.GoalTop * 0.55f, HorizontalAlignment.Left);

        // Level
        int level = _engine.SavedShots / 5 + 1;
        canvas.FontColor = Color.FromArgb("#FFD54F");
        canvas.FontSize = 14f;
        canvas.DrawString($"LVL {level}",
            dirtyRect.Width / 2f, _engine.GoalTop * 0.55f, HorizontalAlignment.Center);

        // Lives
        canvas.FontColor = Colors.White;
        canvas.FontSize = 18f;
        int lives = _engine.MaxLives - _engine.GoalsConceded;
        string livesStr = new string('o', lives) + new string('x', _engine.GoalsConceded);
        canvas.DrawString($"LIVES: {livesStr}",
            dirtyRect.Width - 12f, _engine.GoalTop * 0.55f, HorizontalAlignment.Right);
    }

    private void DrawResultFlash(ICanvas canvas, RectF dirtyRect)
    {
        if (_engine.Ball.IsSaved)
        {
            canvas.FontColor = Colors.LimeGreen;
            canvas.FontSize = 44f;
            canvas.DrawString("SAVED!", dirtyRect.Width / 2f,
                dirtyRect.Height * 0.45f, HorizontalAlignment.Center);
        }
        else if (_engine.Ball.IsGoal)
        {
            canvas.FontColor = Colors.OrangeRed;
            canvas.FontSize = 44f;
            canvas.DrawString("GOAL!", dirtyRect.Width / 2f,
                dirtyRect.Height * 0.45f, HorizontalAlignment.Center);
        }
    }
}
