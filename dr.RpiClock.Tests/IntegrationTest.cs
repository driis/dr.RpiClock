using dr.RpiClock.App;
using Microsoft.Extensions.DependencyInjection;

namespace dr.RpiClock.Tests;

[TestFixture]
public class IntegrationTest
{
    [Test]
    public async Task CanRenderOutputToBmp()
    {
        var outFile = Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Path.GetRandomFileName(), "bmp"));
        var config = Configuration.CreateServiceProvider([outFile]);
        var renderer = config.GetRequiredService<RenderService>();
        
        await renderer.Run(default);
        
        Assert.That(File.Exists(outFile));
        Console.WriteLine($"Rendered output image to file://{outFile}");
        File.Copy(outFile, "out.bmp", true);
    }
}