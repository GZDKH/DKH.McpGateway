using DKH.Platform;
using DKH.Platform.Logging;

await Platform
    .CreateWeb(args)
    .AddPlatformLogging()
    .Build()
    .RunAsync();
