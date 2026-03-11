using System.Numerics;
using SixLabors.Fonts;

namespace dr.RpiClock.App;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO;

public class RenderService(IOptions<RpiClockOptions> options, IPixelRenderer renderer, WeatherService weatherService, ILogger<RenderService> logger)
{
    public RpiClockOptions Options => options.Value;

    static readonly Font _font = SystemFonts.CreateFont("JetBrains Mono", 48, FontStyle.Regular);
    static readonly Font _fontSmall = SystemFonts.CreateFont("JetBrains Mono", 24, FontStyle.Regular);
    static readonly Font _fontLarge = SystemFonts.CreateFont("JetBrains Mono", 72, FontStyle.Bold);
    static readonly Font _fontTemp = SystemFonts.CreateFont("JetBrains Mono", 56, FontStyle.Bold);
    static readonly Font _fontWeatherDesc = SystemFonts.CreateFont("JetBrains Mono", 20, FontStyle.Regular);

    public async Task Run(CancellationToken ct)
    {
        logger.LogInformation("Entering render loop");

        var weather = await weatherService.GetCurrentWeatherAsync(ct);

        using var dial = Image.Load<Bgra32>("assets/dial.png");
        do
        {
            weather = await weatherService.GetCurrentWeatherAsync(ct);

            var image = dial.Clone();
            image.Mutate(ctx => DrawAll(ctx, weather));
            await renderer.RenderToOutput(image, ct);
        } while (Options.Continuous && !ct.IsCancellationRequested);
    }

    void DrawAll(IImageProcessingContext image, WeatherData? weather)
    {
        DrawAnalogClock(image);
        DrawDateTimeText(image);
        if (weather is not null)
            DrawWeather(image, weather);
    }

    void DrawAnalogClock(IImageProcessingContext image)
    {
        int width = 480, height = 480;
        int centerX = width / 2;
        int centerY = height / 2;
        int radius = Math.Min(centerX, centerY) - 10;

        DrawTickMarks(image, centerX, centerY, radius);

        DateTime now = DateTime.Now;
        var seconds = now.Second + now.Millisecond / 1000.0;
        double hourAngle = now.Hour % 12 * 30 + now.Minute * 0.5;
        double minuteAngle = now.Minute * 6 + seconds / 10;
        double secondAngle = seconds * 6;

        // Glow layers
        DrawTaperedHandGlow(image, hourAngle, radius * 0.55, 20, 7, centerX, centerY, Colors.HandGlow);
        DrawTaperedHandGlow(image, minuteAngle, radius * 0.75, 14, 5, centerX, centerY, Colors.HandGlow);

        // Tapered hour hand
        DrawTaperedHand(image, hourAngle, radius * 0.55, 20, 7, centerX, centerY, Colors.HandFill, Colors.HandOutline);

        // Tapered minute hand
        DrawTaperedHand(image, minuteAngle, radius * 0.75, 14, 5, centerX, centerY, Colors.HandFill, Colors.HandOutline);

        // Second hand
        DrawSecondHand(image, secondAngle, radius * 0.85, centerX, centerY);

        // Center hub
        DrawCenterHub(image, centerX, centerY);
    }

    void DrawTickMarks(IImageProcessingContext image, int centerX, int centerY, int radius)
    {
        for (int i = 0; i < 60; i++)
        {
            double angle = i * Math.PI / 30;
            bool isHour = i % 5 == 0;
            float innerFactor = isHour ? 0.88f : 0.93f;
            float outerFactor = 0.97f;
            float thickness = isHour ? 3f : 1.2f;
            Color color = isHour ? Colors.TickHour : Colors.TickMinute;

            float x1 = centerX + (float)(radius * innerFactor * Math.Cos(angle));
            float y1 = centerY + (float)(radius * innerFactor * Math.Sin(angle));
            float x2 = centerX + (float)(radius * outerFactor * Math.Cos(angle));
            float y2 = centerY + (float)(radius * outerFactor * Math.Sin(angle));
            image.DrawLine(color, thickness, new PointF(x1, y1), new PointF(x2, y2));
        }
    }

    void DrawTaperedHand(IImageProcessingContext image, double angleDegrees, double length,
        float baseWidth, float tipWidth, int centerX, int centerY, Color fillColor, Color outlineColor)
    {
        double angleRad = (angleDegrees - 90) * Math.PI / 180;
        double perpRad = angleRad + Math.PI / 2;

        float tipX = centerX + (float)(length * Math.Cos(angleRad));
        float tipY = centerY + (float)(length * Math.Sin(angleRad));

        float baseLeftX = centerX + (float)(baseWidth / 2 * Math.Cos(perpRad));
        float baseLeftY = centerY + (float)(baseWidth / 2 * Math.Sin(perpRad));
        float baseRightX = centerX - (float)(baseWidth / 2 * Math.Cos(perpRad));
        float baseRightY = centerY - (float)(baseWidth / 2 * Math.Sin(perpRad));

        float tipLeftX = tipX + (float)(tipWidth / 2 * Math.Cos(perpRad));
        float tipLeftY = tipY + (float)(tipWidth / 2 * Math.Sin(perpRad));
        float tipRightX = tipX - (float)(tipWidth / 2 * Math.Cos(perpRad));
        float tipRightY = tipY - (float)(tipWidth / 2 * Math.Sin(perpRad));

        // Counter-balance tail
        float tailLength = (float)(length * 0.15);
        float tailX = centerX - (float)(tailLength * Math.Cos(angleRad));
        float tailY = centerY - (float)(tailLength * Math.Sin(angleRad));
        float tailWidth = baseWidth * 0.6f;
        float tailLeftX = tailX + (float)(tailWidth / 2 * Math.Cos(perpRad));
        float tailLeftY = tailY + (float)(tailWidth / 2 * Math.Sin(perpRad));
        float tailRightX = tailX - (float)(tailWidth / 2 * Math.Cos(perpRad));
        float tailRightY = tailY - (float)(tailWidth / 2 * Math.Sin(perpRad));

        var polygon = new Polygon(new LinearLineSegment(
            new PointF(tailLeftX, tailLeftY),
            new PointF(baseLeftX, baseLeftY),
            new PointF(tipLeftX, tipLeftY),
            new PointF(tipRightX, tipRightY),
            new PointF(baseRightX, baseRightY),
            new PointF(tailRightX, tailRightY)
        ));

        image.Fill(fillColor, polygon);
        image.Draw(outlineColor, 1.5f, polygon);
    }

    void DrawTaperedHandGlow(IImageProcessingContext image, double angleDegrees, double length,
        float baseWidth, float tipWidth, int centerX, int centerY, Color glowColor)
    {
        DrawTaperedHand(image, angleDegrees, length * 1.02, baseWidth + 6, tipWidth + 4,
            centerX, centerY, glowColor, Color.Transparent);
    }

    void DrawSecondHand(IImageProcessingContext image, double angleDegrees, double length,
        int centerX, int centerY)
    {
        double angleRad = (angleDegrees - 90) * Math.PI / 180;

        float tipX = centerX + (float)(length * Math.Cos(angleRad));
        float tipY = centerY + (float)(length * Math.Sin(angleRad));

        float tailLength = (float)(length * 0.2);
        float tailX = centerX - (float)(tailLength * Math.Cos(angleRad));
        float tailY = centerY - (float)(tailLength * Math.Sin(angleRad));

        // Shadow
        image.DrawLine(new Color(new Rgba32(0, 0, 0, 60)), 3f,
            new PointF(tailX + 2, tailY + 2), new PointF(tipX + 2, tipY + 2));
        // Main line
        image.DrawLine(Colors.SecondHand, 2.5f,
            new PointF(tailX, tailY), new PointF(tipX, tipY));
        // Tip circle
        image.Fill(Colors.SecondHand, new EllipsePolygon(tipX, tipY, 4));
    }

    void DrawCenterHub(IImageProcessingContext image, int centerX, int centerY)
    {
        image.Fill(Colors.HubOuter, new EllipsePolygon(centerX, centerY, 14));
        image.Fill(Colors.HubMiddle, new EllipsePolygon(centerX, centerY, 10));
        image.Fill(Colors.HubInner, new EllipsePolygon(centerX, centerY, 6));
        image.Fill(Color.White, new EllipsePolygon(centerX - 2, centerY - 2, 2.5f));
    }

    void DrawDateTimeText(IImageProcessingContext image)
    {
        DateTime now = DateTime.Now;

        string timeNow = now.ToString("HH\\:mm", Options.RenderCulture);
        float rightCenterX = 640;
        var timePos = new PointF(rightCenterX, 30);

        // Time shadow
        image.DrawText(new RichTextOptions(_fontLarge)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Origin = new PointF(timePos.X + 2, timePos.Y + 2),
        }, timeNow, Colors.TextShadow);
        // Time main
        image.DrawText(new RichTextOptions(_fontLarge)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Origin = timePos,
        }, timeNow, Colors.TextPrimary);

        // Weekday
        string weekDay = now.ToString("dddd", Options.RenderCulture);
        var weekDayPos = new PointF(rightCenterX, 380);
        image.DrawText(new RichTextOptions(_font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Origin = new PointF(weekDayPos.X + 2, weekDayPos.Y + 2),
        }, weekDay, Colors.TextShadow);
        image.DrawText(new RichTextOptions(_font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Origin = weekDayPos,
        }, weekDay, Colors.TextPrimary);

        // Date
        string dateText = now.ToString("dd. MMMM", Options.RenderCulture);
        var datePos = new PointF(rightCenterX, 430);
        image.DrawText(new RichTextOptions(_fontSmall)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Origin = new PointF(datePos.X + 1, datePos.Y + 1),
        }, dateText, Colors.TextShadow);
        image.DrawText(new RichTextOptions(_fontSmall)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Origin = datePos,
        }, dateText, Colors.TextSecondary);
    }

    void DrawWeather(IImageProcessingContext image, WeatherData weather)
    {
        float weatherCenterX = 640;
        float weatherTopY = 130;

        DrawWeatherIcon(image, weather.WeatherCode, weatherCenterX, weatherTopY + 50, 45);

        string tempText = $"{weather.Temperature:F0}\u00b0C";
        var tempPos = new PointF(weatherCenterX, weatherTopY + 115);
        image.DrawText(new RichTextOptions(_fontTemp)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Origin = new PointF(tempPos.X + 2, tempPos.Y + 2),
        }, tempText, Colors.TextShadow);
        image.DrawText(new RichTextOptions(_fontTemp)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Origin = tempPos,
        }, tempText, Colors.TextPrimary);

        string desc = WeatherService.GetWeatherDescription(weather.WeatherCode);
        var descPos = new PointF(weatherCenterX, weatherTopY + 185);
        image.DrawText(new RichTextOptions(_fontWeatherDesc)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Origin = descPos,
        }, desc, Colors.TextSecondary);
    }

    void DrawWeatherIcon(IImageProcessingContext image, int code, float cx, float cy, float size)
    {
        if (code == 0)
            DrawSun(image, cx, cy, size);
        else if (code is 1 or 2 or 3)
        {
            DrawSun(image, cx - size * 0.3f, cy - size * 0.2f, size * 0.6f);
            DrawCloud(image, cx + size * 0.15f, cy + size * 0.1f, size * 0.7f);
        }
        else if (code is 45 or 48)
            DrawFog(image, cx, cy, size);
        else if (code is 51 or 53 or 55 or 61 or 63 or 65 or 66 or 67 or 80 or 81 or 82)
        {
            DrawCloud(image, cx, cy - size * 0.2f, size * 0.8f);
            DrawRainDrops(image, cx, cy + size * 0.3f, size * 0.6f);
        }
        else if (code is 71 or 73 or 75 or 77 or 85 or 86)
        {
            DrawCloud(image, cx, cy - size * 0.2f, size * 0.8f);
            DrawSnowflakes(image, cx, cy + size * 0.35f, size * 0.6f);
        }
        else if (code is 95 or 96 or 99)
        {
            DrawCloud(image, cx, cy - size * 0.2f, size * 0.8f);
            DrawLightning(image, cx, cy + size * 0.15f, size * 0.5f);
        }
        else
            DrawCloud(image, cx, cy, size * 0.8f);
    }

    void DrawSun(IImageProcessingContext image, float cx, float cy, float size)
    {
        image.Fill(Colors.SunGlow, new EllipsePolygon(cx, cy, size * 0.65f));
        image.Fill(Colors.Sun, new EllipsePolygon(cx, cy, size * 0.4f));
        for (int i = 0; i < 8; i++)
        {
            double angle = i * Math.PI / 4;
            float x1 = cx + (float)(size * 0.5f * Math.Cos(angle));
            float y1 = cy + (float)(size * 0.5f * Math.Sin(angle));
            float x2 = cx + (float)(size * 0.75f * Math.Cos(angle));
            float y2 = cy + (float)(size * 0.75f * Math.Sin(angle));
            image.DrawLine(Colors.Sun, 2.5f, new PointF(x1, y1), new PointF(x2, y2));
        }
    }

    void DrawCloud(IImageProcessingContext image, float cx, float cy, float size)
    {
        float so = 2;
        image.Fill(Colors.CloudShadow, new EllipsePolygon(cx - size * 0.25f + so, cy + so, size * 0.35f, size * 0.25f));
        image.Fill(Colors.CloudShadow, new EllipsePolygon(cx + size * 0.15f + so, cy + so, size * 0.35f, size * 0.25f));
        image.Fill(Colors.CloudShadow, new EllipsePolygon(cx - size * 0.05f + so, cy - size * 0.15f + so, size * 0.3f, size * 0.25f));
        image.Fill(Colors.Cloud, new EllipsePolygon(cx - size * 0.25f, cy, size * 0.35f, size * 0.25f));
        image.Fill(Colors.Cloud, new EllipsePolygon(cx + size * 0.15f, cy, size * 0.35f, size * 0.25f));
        image.Fill(Colors.Cloud, new EllipsePolygon(cx - size * 0.05f, cy - size * 0.15f, size * 0.3f, size * 0.25f));
    }

    void DrawRainDrops(IImageProcessingContext image, float cx, float cy, float size)
    {
        float[] offsets = { -0.3f, 0f, 0.3f };
        foreach (var ox in offsets)
        {
            float x = cx + size * ox;
            image.DrawLine(Colors.Rain, 2f,
                new PointF(x, cy),
                new PointF(x - size * 0.05f, cy + size * 0.3f));
        }
        image.DrawLine(Colors.Rain, 1.5f,
            new PointF(cx - size * 0.15f, cy + size * 0.1f),
            new PointF(cx - size * 0.2f, cy + size * 0.35f));
        image.DrawLine(Colors.Rain, 1.5f,
            new PointF(cx + size * 0.15f, cy + size * 0.1f),
            new PointF(cx + size * 0.1f, cy + size * 0.35f));
    }

    void DrawSnowflakes(IImageProcessingContext image, float cx, float cy, float size)
    {
        float[] xOffsets = { -0.3f, 0f, 0.3f, -0.15f, 0.15f };
        float[] yOffsets = { 0f, 0.05f, 0f, 0.25f, 0.2f };
        for (int i = 0; i < xOffsets.Length; i++)
        {
            float x = cx + size * xOffsets[i];
            float y = cy + size * yOffsets[i];
            image.Fill(Colors.Snow, new EllipsePolygon(x, y, 3f));
        }
    }

    void DrawLightning(IImageProcessingContext image, float cx, float cy, float size)
    {
        var points = new PointF[]
        {
            new(cx, cy),
            new(cx - size * 0.15f, cy + size * 0.3f),
            new(cx + size * 0.05f, cy + size * 0.3f),
            new(cx - size * 0.1f, cy + size * 0.65f),
        };
        for (int i = 0; i < points.Length - 1; i++)
            image.DrawLine(Colors.Lightning, 3f, points[i], points[i + 1]);
    }

    void DrawFog(IImageProcessingContext image, float cx, float cy, float size)
    {
        for (int i = 0; i < 4; i++)
        {
            float y = cy - size * 0.3f + i * size * 0.2f;
            float halfW = size * (0.5f - i * 0.03f);
            image.DrawLine(Colors.Fog, 2.5f,
                new PointF(cx - halfW, y), new PointF(cx + halfW, y));
        }
    }

    static class Colors
    {
        public static Color HandFill { get; } = new(new Rgba32(210, 210, 220, 240));
        public static Color HandOutline { get; } = new(new Rgba32(180, 180, 190, 200));
        public static Color HandGlow { get; } = new(new Rgba32(100, 140, 200, 40));
        public static Color SecondHand { get; } = new(new Rgba32(220, 50, 50, 255));

        public static Color TickHour { get; } = new(new Rgba32(200, 200, 210, 230));
        public static Color TickMinute { get; } = new(new Rgba32(150, 150, 170, 150));

        public static Color HubOuter { get; } = new(new Rgba32(80, 80, 100, 255));
        public static Color HubMiddle { get; } = new(new Rgba32(160, 160, 180, 255));
        public static Color HubInner { get; } = new(new Rgba32(220, 220, 230, 255));

        public static Color TextPrimary { get; } = new(new Rgba32(220, 220, 230, 255));
        public static Color TextSecondary { get; } = new(new Rgba32(170, 175, 190, 220));
        public static Color TextShadow { get; } = new(new Rgba32(0, 0, 0, 120));

        public static Color Sun { get; } = new(new Rgba32(255, 200, 50, 255));
        public static Color SunGlow { get; } = new(new Rgba32(255, 200, 50, 50));
        public static Color Cloud { get; } = new(new Rgba32(200, 205, 215, 230));
        public static Color CloudShadow { get; } = new(new Rgba32(80, 85, 95, 100));
        public static Color Rain { get; } = new(new Rgba32(100, 160, 255, 220));
        public static Color Snow { get; } = Color.White;
        public static Color Lightning { get; } = new(new Rgba32(255, 230, 80, 255));
        public static Color Fog { get; } = new(new Rgba32(180, 185, 195, 160));
    }
}
