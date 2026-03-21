using Microsoft.AspNetCore.HttpOverrides;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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
