using Serilog;
using Serilog.Context;
using Serilog.Events;

LogEventProperty[] NoProperties = new LogEventProperty[0];

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Default", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {ApplicationName} {Level:u3}] [{RequestScheme}://{RequestHost} [{ClientIp}] {Message:lj}{NewLine}{Exception}")
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ApplicationName", "SerilogCustomEnrichers")
    .CreateLogger();

try
{
    Log.Information("Starting web application");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    var app = builder.Build();

    app.Use(async (httpContext, next) =>
    {
        var ctxHost = LogContext.PushProperty("RequestHost", httpContext.Request.Host.HasValue ? httpContext.Request.Host.Value : string.Empty);
        var ctxScheme = LogContext.PushProperty("RequestScheme", httpContext.Request.Scheme);
        var ctxCientIp = LogContext.PushProperty("ClientIp", httpContext.Connection.RemoteIpAddress);

        await next();

        ctxCientIp.Dispose();
        ctxHost.Dispose();
        ctxScheme.Dispose();
    });

    app.MapGet("/", (ILogger<Program> logger) =>
    {
        logger.LogInformation("Running Hello World");
        return Results.Ok("Hello World!");
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
