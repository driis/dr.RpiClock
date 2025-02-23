using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace dr.RpiClock.App;

public interface IPixelRenderer : IDisposable
{
    Task RenderToOutput<TPixel>(Image<TPixel> image, CancellationToken ct) where TPixel : unmanaged, IPixel<TPixel>;
}