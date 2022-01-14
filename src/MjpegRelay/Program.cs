using MjpegRelay;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddLogging(logging => logging.AddSimpleConsole())
    .AddHostedService<StreamService>()
    .AddSingleton<Broadcaster>()
    .AddSingleton<IStreamSink>(sp => sp.GetService<Broadcaster>());

switch (builder.Configuration["Source"]?.ToLower())
{
    case "rtsp":
        builder.Services
            .AddSingleton<IStreamSource, RtspSource>()
            .Configure<RtspOptions>(builder.Configuration.GetSection("Rtsp"));
        break;
    case "mjpeg":
        builder.Services
            .AddSingleton<IStreamSource, MjpegSource>()
            .Configure<MjpegOptions>(builder.Configuration.GetSection("Mjpeg"));
        break;
}

var app = builder.Build();

app.MapGet("/Status", async (HttpContext context, Broadcaster broadcaster, CancellationToken cancellationToken) =>
{
    await context.Response.WriteAsync($"Clients: {broadcaster.ClientCount}", cancellationToken);
});

app.MapGet("/Stream", async (HttpContext context, Broadcaster broadcaster, CancellationToken cancellationToken) =>
{
    broadcaster.AddClient(context);
    
    try
    {
        await context.Response.StartAsync(cancellationToken);
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }
    catch (OperationCanceledException)
    {
    }
    finally
    {
        broadcaster.RemoveClient(context);
    }
});

app.Run();