using Microsoft.Extensions.Options;
using SixLabors.ImageSharp.PixelFormats;

namespace dr.RpiClock.App;

public static class Configuration
{
    public static IServiceProvider CreateServiceProvider(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Information))
            .AddSingleton<IOptions<RpiClockOptions>, RpiClockOptionParser>(sc => new RpiClockOptionParser(args))
            .AddTransient<RenderService>();

        if (args.Contains("-c"))
            serviceProvider.AddTransient<IPixelRenderer, FrameBufferRenderer>();
        else
            serviceProvider.AddTransient<IPixelRenderer, BmpPixelRenderer>();
            
        return serviceProvider.BuildServiceProvider();
    }

    public class RpiClockOptionParser : IOptions<RpiClockOptions>
    {
        private readonly string[] _options;
        private readonly string[] _arguments;
        public RpiClockOptions Value { get; }
        public RpiClockOptionParser(string[] args)
        {
            _options = args.Where(a => a.StartsWith('-')).Select(a => a.Trim('-')).ToArray();
            _arguments = args.Where(a => !a.StartsWith('-')).ToArray();
            Value = Parse();
        }

        private RpiClockOptions Parse()
        {
            if (_arguments.Length < 1)
                throw new ArgumentException("First argument must be the output file name");
            bool continuous = _options.Any(o => o.StartsWith("c"));
            return new RpiClockOptions(800, 480, _arguments[0], continuous);
        }
    }
}

public record RpiClockOptions(int Width, int Height, string OutputFileName, bool Continuous);