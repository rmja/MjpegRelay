using MjpegRelay;
using Microsoft.Net.Http.Headers;

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
    context.Response.Headers[HeaderNames.CacheControl] = "no-store, no-cache, max-age=0";
    context.Response.Headers[HeaderNames.ContentType] = $"multipart/x-mixed-replace; boundary={Broadcaster.Boundary}";

    await context.Response.StartAsync(cancellationToken);

    var client = context.Response.BodyWriter;
    broadcaster.AddClient(client);
    
    try
    {
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }
    catch (OperationCanceledException)
    {
    }
    finally
    {
        broadcaster.RemoveClient(client);
    }
});

app.Run();