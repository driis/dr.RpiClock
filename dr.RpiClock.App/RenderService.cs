namespace dr.RpiClock.App;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO;

public class RenderService(IOptions<RpiClockOptions> _options, ILogger<RenderService> _logger)
{
    private readonly int _bufferSize = _options.Value.Height * _options.Value.Width * 2;
    public async Task Run(CancellationToken ct)
    {
        var options = _options.Value;
        var (width,height) = (options.Width, options.Height);
        await using var outFile = File.Open(options.OutputFileName, FileMode.OpenOrCreate);
        using var image = new Image<Bgr565>(width, height);
        var pos = new PointF(width/2, height/2);
        var vec = new PointF(1, -1);
        const int r = 16;
        byte[] buffer = new byte[_bufferSize];
        _logger.LogInformation("Entering render loop");

        do
        {
            image.Mutate(ctx =>
            {
                ctx.Fill(Color.WhiteSmoke);
                var circle = new EllipsePolygon(pos, r);
                ctx.Fill(Color.Red, circle);
            });
            Draw(outFile, image, buffer);
            await Task.Delay(5);
            pos += vec;
            if (pos.X <= 0 || pos.X >= width)
            {
                vec.X *= -1;
            }

            if (pos.Y <= 0 || pos.Y >= height)
            {
                vec.Y *= -1;
            }
        } while (options.Continuous && !ct.IsCancellationRequested);
    }
    
    void Draw(Stream outStream, Image<Bgr565> img, Span<byte> buffer)
    {
        outStream.Seek(0, SeekOrigin.Begin);
        img.CopyPixelDataTo(buffer);
        outStream.Write(buffer);    
    }
}