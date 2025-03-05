using System.Numerics;
using SixLabors.Fonts;

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
        logger.LogInformation("Entering render loop");

        using var dial = Image.Load<Bgra32>("assets/dial.png");
        do
        {
            var image = dial.Clone();
            image.Mutate(DrawAnalogClock);
            await renderer.RenderToOutput(image, ct);
        } while (Options.Continuous && !ct.IsCancellationRequested);
    }

    void DrawAnalogClock(IImageProcessingContext image)
    {
        int width = 480, height = 480;
        int centerX = width / 2;
        int centerY = height / 2;
        int radius = Math.Min(centerX, centerY) - 10;

        // Overlay hour tick marks.
        DrawTickMarks(image, centerX, radius, centerY);

        // Draw clock hands (with drop shadow for depth).
        DateTime now = DateTime.Now;
        DrawClockHand(image, now.Hour % 12 * 30 + now.Minute * 0.5, radius * 0.6, Colors.Hand, 8, centerX, centerY);
        DrawClockHand(image, now.Minute * 6, radius * 0.8, Colors.Hand, 5, centerX, centerY);
        DrawClockHand(image, now.Second * 6, radius * 0.9, Colors.HandSecond, 2, centerX, centerY);

        // Draw the central hub.
        image.Fill(Color.White, new EllipsePolygon(centerX, centerY, 10));
        
        DrawTodaysDate(image);
    }

    void DrawTickMarks(IImageProcessingContext image, int centerX, int radius, int centerY)
    {
        for (int i = 0; i < 12; i++)
        {
            double angle = i * Math.PI / 6;
            int x1 = centerX + (int)(radius * 0.95 * Math.Cos(angle));
            int y1 = centerY + (int)(radius * 0.95 * Math.Sin(angle));
            int x2 = centerX + (int)(radius * Math.Cos(angle));
            int y2 = centerY + (int)(radius * Math.Sin(angle));
            image.DrawLine(Colors.Hand, 3, new PointF(x1, y1), new PointF(x2, y2));
        }
    }

    void DrawClockHand(IImageProcessingContext image, double angleDegrees, double length, Color color, float thickness,
        int centerX, int centerY)
    {
        // Calculate the hand endpoint.
        double angleRad = (angleDegrees - 90) * Math.PI / 180;
        int x = centerX + (int)(length * Math.Cos(angleRad));
        int y = centerY + (int)(length * Math.Sin(angleRad));

        // Draw a drop shadow for depth.
        int shadowOffset = 4;
        
        // Shadow (black offset line)
        image.DrawLine(Color.Black, thickness,
            new PointF(centerX + shadowOffset, centerY + shadowOffset),
            new PointF(x + shadowOffset, y + shadowOffset));
        // Main clock hand.
        image.DrawLine(color, thickness,
            new PointF(centerX, centerY),
            new PointF(x, y));
    }
    void DrawTodaysDate(IImageProcessingContext image) 
    {
        Font font = SystemFonts.CreateFont("JetBrains Mono", 48, FontStyle.Regular);
        string dateText = DateTime.Now.ToString("dd. MMMM");
        var lowerRightCorner = new PointF(780, 460);   
        RichTextOptions dateTextOptions = new RichTextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Origin = lowerRightCorner,
        };
        var dateSize = TextMeasurer.MeasureSize(dateText, new RichTextOptions(font));
        image.DrawText(dateTextOptions, dateText, Colors.Hand);

        string weekDay = DateTime.Now.ToString("dddd");
        var weekDayPos = new PointF(lowerRightCorner.X - dateSize.Width / 2, lowerRightCorner.Y - dateSize.Height - 16);
        image.DrawText(new RichTextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Origin = weekDayPos,
        }, weekDay, Colors.Hand);
    }

    static class Colors
    {
        public static Color Hand { get; } = new(new Bgr24(0xcc,0xcc,0xcc));
        public static Color HandSecond { get; } = Color.Red;
    }
}