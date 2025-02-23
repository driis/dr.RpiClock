using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace dr.RpiClock.App;

public class BmpPixelRenderer(IOptions<RpiClockOptions> options) : IPixelRenderer
{
    public void Dispose() { }

    public async Task RenderToOutput<TPixel>(Image<TPixel> image, CancellationToken ct) where TPixel : unmanaged, IPixel<TPixel>
    {
        await image.SaveAsBmpAsync(options.Value.OutputFileName, ct);
    }
}