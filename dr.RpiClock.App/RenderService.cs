namespace dr.RpiClock.App;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO;

public class RenderService(IOptions<RpiClockOptions> options, IPixelRenderer renderer, ILogger<RenderService> logger)
{
    public async Task Run(CancellationToken ct)
    {
        var opt = options.Value;
        var (width,height) = (opt.Width, opt.Height);
        using var image = new Image<Rgba32>(width, height);

        logger.LogInformation("Entering render loop");
        do
        {
           DrawAnalogClock(image);
           await renderer.RenderToOutput(image, ct);
        } while (opt.Continuous && !ct.IsCancellationRequested);
    }
    void DrawAnalogClock<TPixel>(Image<TPixel> image) where TPixel : unmanaged, IPixel<TPixel>
    {
        int width = 480, height = 480;
        int centerX = width / 2, centerY = height / 2;
        int radius = Math.Min(centerX, centerY) - 10;
    
        var black = Color.Black;
        var white = Color.White;
        var red = Color.Red;

        image.Mutate(ctx => ctx.Fill(black));
        image.Mutate(ctx => ctx.Draw(white, 5, new EllipsePolygon(centerX, centerY, radius)));

        for (int i = 0; i < 12; i++)
        {
            double angle = i * Math.PI / 6;
            int x1 = centerX + (int)(radius * 0.85 * Math.Cos(angle));
            int y1 = centerY + (int)(radius * 0.85 * Math.Sin(angle));
            int x2 = centerX + (int)(radius * Math.Cos(angle));
            int y2 = centerY + (int)(radius * Math.Sin(angle));
            image.Mutate(ctx => ctx.DrawLine(white, 3, new PointF(x1, y1), new PointF(x2, y2)));
        }
    
        DateTime now = DateTime.Now;
        DrawClockHand(image, now.Hour % 12 * 30 + now.Minute * 0.5, radius * 0.5, white, 8, centerX, centerY);
        DrawClockHand(image, now.Minute * 6, radius * 0.7, white, 5, centerX, centerY);
        DrawClockHand(image, now.Second * 6, radius * 0.9, red, 2, centerX, centerY);
    
        image.Mutate(ctx => ctx.Fill(white, new EllipsePolygon(centerX, centerY, 8)));
    }

    void DrawClockHand<TPixel>(Image<TPixel> image, double angleDegrees, double length, Color color, float thickness, int centerX, int centerY) where TPixel : unmanaged, IPixel<TPixel>
    {
        double angleRad = (angleDegrees - 90) * Math.PI / 180;
        int x = centerX + (int)(length * Math.Cos(angleRad));
        int y = centerY + (int)(length * Math.Sin(angleRad));
        image.Mutate(ctx => ctx.DrawLine(color, thickness, new PointF(centerX, centerY), new PointF(x, y)));
    }
}