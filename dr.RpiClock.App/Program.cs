
var services = Configuration.CreateServiceProvider(args);
var logger = services.GetRequiredService<ILogger<Program>>();
try
{
    var options  = services.GetRequiredService<IOptions<RpiClockOptions>>();
    logger.LogInformation("Preparing to run with configuration: {configuration}", options.Value);
}
catch (ArgumentException e)
{
    logger.LogError("Encountered configuration errors. Please fix:\n{ConfigurationError}",e.Message);
    return;
}

CancellationTokenSource cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) => cts.Cancel();
var renderer = services.GetRequiredService<RenderService>();
await renderer.Run(cts.Token);