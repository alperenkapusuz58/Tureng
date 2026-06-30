using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using ClockworkUmbraco.Composers;
using ClockworkUmbraco.Services.Tts;
using Microsoft.Extensions.Options;
using Kelimebull.Tts.Core.Configuration;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
        }

        await context.HttpContext.Response.WriteAsJsonAsync(
            new { status = "rate_limited", error = "Çok fazla istek. Lütfen biraz bekleyip tekrar deneyin." },
            cancellationToken);
    };

    options.AddPolicy(RegisterServiceComposer.TtsAudioRateLimitPolicy, httpContext =>
    {
        var ttsOptions = httpContext.RequestServices.GetRequiredService<IOptions<TtsOptions>>().Value;
        var permitLimit = Math.Max(1, ttsOptions.ApiRequestsPerMinutePerIp);

        return RateLimitPartition.GetFixedWindowLimiter(
            TtsClientIpResolver.Resolve(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            });
    });
});

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .Build();

WebApplication app = builder.Build();

//bu alan loadbalancer arkasinda calisan uygulamalar icin gerekli dogru calismasi icin loadbalancer den bu iki header ekli olmali => X-Forwarded-For ve X-Forwarded-Proto
// Add the forwarded headers middleware here
//var forwardedHeaderOptions = new ForwardedHeadersOptions
//{
//    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
//};
//forwardedHeaderOptions.KnownIPNetworks.Clear(); // Removes restrictions on proxy IP addresses
//forwardedHeaderOptions.KnownProxies.Clear(); // Allows Azure proxies to be trusted
//app.UseForwardedHeaders(forwardedHeaderOptions);

await app.BootUmbracoAsync();

app.UseRateLimiter();

app.UseStaticFiles();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
    await next();
});

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();
