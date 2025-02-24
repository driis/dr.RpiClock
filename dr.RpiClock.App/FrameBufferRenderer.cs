using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace dr.RpiClock.App;

public class FrameBufferRenderer : IPixelRenderer, IAsyncDisposable
{
    private readonly IOptions<RpiClockOptions> _options;
    private readonly byte[] _buffer;
    public const int BytesPerPixel = 4;     // 32 bpp
    private readonly Stream _outputStream;
    public FrameBufferRenderer(IOptions<RpiClockOptions> options)
    {
        _options = options;
        _buffer = new byte[_options.Value.Width * _options.Value.Height * BytesPerPixel];
        _outputStream = File.Open(_options.Value.OutputFileName, FileMode.OpenOrCreate);
    }
    public Task RenderToOutput<TPixel>(Image<TPixel> image, CancellationToken ct) where TPixel : unmanaged, IPixel<TPixel>
    {
        _outputStream.Seek(0, SeekOrigin.Begin);
        image.CopyPixelDataTo(_buffer);
        _outputStream.Write(_buffer);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _outputStream.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _outputStream.DisposeAsync();
    }
}