namespace dr.RpiClock.App;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO;

public class RenderService(IOptions<RpiClockOptions> options, IPixelRenderer renderer, ILogger<RenderService> logger)
{
    public RpiClockOptions Options => options.Value;
    public async Task Run(CancellationToken ct)
    {
        var (width, height) = (Options.Width, Options.Height);
        using var image = new Image<Bgra32>(width, height);

        logger.LogInformation("Entering render loop");
        do
        {
            DrawAnalogClock(image);
            await renderer.RenderToOutput(image, ct);
        } while (Options.Continuous && !ct.IsCancellationRequested);
    }

    void DrawAnalogClock<TPixel>(Image<TPixel> image) where TPixel : unmanaged, IPixel<TPixel>
    {
        int width = 480, height = 480;
        int centerX = width / 2;
        int centerY = height / 2;
        int radius = Math.Min(centerX, centerY) - 10;

        // Load the dial background image, resize it to 480x480, and draw it.
        using (var dial = Image.Load<TPixel>("assets/dial.png"))
        {
            //dial.Mutate(ctx => ctx.Resize(Options.Width, Options.Height));
            image.Mutate(ctx => ctx.DrawImage(dial, new Point(0, 0), 1f));
        }
        
        // Optionally, overlay hour tick marks.
        for (int i = 0; i < 12; i++)
        {
            double angle = i * Math.PI / 6;
            int x1 = centerX + (int)(radius * 0.95 * Math.Cos(angle));
            int y1 = centerY + (int)(radius * 0.95 * Math.Sin(angle));
            int x2 = centerX + (int)(radius * Math.Cos(angle));
            int y2 = centerY + (int)(radius * Math.Sin(angle));
            image.Mutate(ctx => ctx.DrawLine(Colors.Hand, 3, new PointF(x1, y1), new PointF(x2, y2)));
        }

        // Get the current time.
        DateTime now = DateTime.Now;
        // Draw clock hands (with drop shadow for depth).
        DrawClockHand(image, now.Hour % 12 * 30 + now.Minute * 0.5, radius * 0.6, Colors.Hand, 8, centerX, centerY);
        DrawClockHand(image, now.Minute * 6, radius * 0.8, Colors.Hand, 5, centerX, centerY);
        DrawClockHand(image, now.Second * 6, radius * 0.9, Colors.HandSecond, 2, centerX, centerY);

        // Draw the central hub.
        image.Mutate(ctx => ctx.Fill(Color.White, new EllipsePolygon(centerX, centerY, 10)));
    }

    void DrawClockHand<TPixel>(Image<TPixel> image, double angleDegrees, double length, Color color, float thickness,
        int centerX, int centerY)
        where TPixel : unmanaged, IPixel<TPixel>
    {
        // Calculate the hand endpoint.
        double angleRad = (angleDegrees - 90) * Math.PI / 180;
        int x = centerX + (int)(length * Math.Cos(angleRad));
        int y = centerY + (int)(length * Math.Sin(angleRad));

        // Draw a drop shadow for depth.
        int shadowOffset = 4;
        image.Mutate(ctx =>
        {
            // Shadow (black offset line)
            ctx.DrawLine(Color.Black, thickness,
                new PointF(centerX + shadowOffset, centerY + shadowOffset),
                new PointF(x + shadowOffset, y + shadowOffset));
            // Main clock hand.
            ctx.DrawLine(color, thickness,
                new PointF(centerX, centerY),
                new PointF(x, y));
        });
    }

    static class Colors
    {
        public static Color Hand { get; } = Color.WhiteSmoke;
        public static Color HandSecond { get; } = Color.Red;
    }
}