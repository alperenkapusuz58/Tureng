using ClockworkUmbraco.Helpers;
using ClockworkUmbraco.Services;
using ClockworkUmbraco.Services.Interfaces;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Composing;

namespace ClockworkUmbraco.Composers
{
    public class RegisterServiceComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            builder.Services.AddTransient<IConfigureOptions<StaticFileOptions>, ConfigureStaticFileOptions>();
            builder.Services.AddScoped<RenderPartialViewHandler>();
            builder.Services.AddScoped<MailHandler>();
            builder.Services.AddScoped<ISearchService, SearchService>();
            builder.SetContentLastChanceFinder<PageNotFound>();
        }
    }
}

