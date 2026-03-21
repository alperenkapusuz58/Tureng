using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Hosting;
using IHostingEnvironment = Umbraco.Cms.Core.Hosting.IHostingEnvironment;

namespace ClockworkUmbraco.Services
{
    public class ConfigureStaticFileOptions : IConfigureOptions<StaticFileOptions>
    {
        // These are the extensions of the file types we want to cache (add and remove as you see fit)
        private static readonly HashSet<string> _cachedFileExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".ico",
            ".css",
            ".js",
            ".svg",
            ".woff2",
            ".jpg"
        };

        private readonly string _backOfficePath;

        public ConfigureStaticFileOptions(IOptions<GlobalSettings> globalSettings, IHostingEnvironment hostingEnvironment)
        {
            _backOfficePath = hostingEnvironment.GetBackOfficePath();
        }

        public void Configure(StaticFileOptions options)
            => options.OnPrepareResponse = ctx =>
            {
                if (ctx.Context.Request.Path.StartsWithSegments(_backOfficePath))
                {
                    return;
                }

                var fileExtension = Path.GetExtension(ctx.File.Name);
                if (_cachedFileExtensions.Contains(fileExtension))
                {
                    ResponseHeaders headers = ctx.Context.Response.GetTypedHeaders();

                    CacheControlHeaderValue cacheControl = headers.CacheControl ?? new CacheControlHeaderValue();
                    cacheControl.Public = true;
                    cacheControl.MaxAge = TimeSpan.FromDays(365);
                    headers.CacheControl = cacheControl;
                }
            };
    }
}

