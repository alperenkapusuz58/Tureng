using ClockworkUmbraco.Helpers;
using ClockworkUmbraco.Services;
using ClockworkUmbraco.Services.Interfaces;
using ClockworkUmbraco.Services.Tts;
using Microsoft.Extensions.Options;
using Kelimebull.Tts.Core.Configuration;
using Kelimebull.Tts.Core.Data;
using Kelimebull.Tts.Core.Voices;
using Umbraco.Cms.Core.Composing;
using PageNotFound = ClockworkUmbraco.Services.PageNotFound;

namespace ClockworkUmbraco.Composers
{
    public class RegisterServiceComposer : IComposer
    {
        public const string TtsAudioRateLimitPolicy = "tts-audio";

        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            builder.Services.AddTransient<IConfigureOptions<StaticFileOptions>, ConfigureStaticFileOptions>();
            builder.Services.AddScoped<RenderPartialViewHandler>();
            builder.Services.AddScoped<MailHandler>();
            builder.Services.AddScoped<ISearchService, SearchService>();
            builder.Services.AddScoped<IWordOfTheDayService, WordOfTheDayService>();
            builder.Services.AddScoped<HeadwordSearchMapper>();
            builder.Services.Configure<TtsOptions>(builder.Config.GetSection(TtsOptions.SectionName));
            builder.Services.AddScoped<TtsVoiceResolver>();
            builder.Services.AddScoped<ITtsAudioRegistry, TtsAudioRegistry>();
            builder.Services.AddScoped<ITtsAudioUrlBuilder, TtsAudioUrlBuilder>();
            builder.Services.AddScoped<ITtsInventoryService, TtsInventoryService>();
            builder.Services.AddHttpClient("TtsAudioStream", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            builder.Services.AddHttpClient<IOpenAiTtsClient, OpenAiTtsClient>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(90);
            });
            builder.Services.AddSingleton<TtsRateLimiter>();
            builder.Services.AddSingleton<TtsDispatchService>();
            builder.Services.AddScoped<IR2AudioStorage, R2AudioStorage>();
            builder.Services.AddScoped<TtsGenerationProcessor>();
            builder.Services.AddHostedService<TtsGenerationWorker>();
            builder.Services.AddHostedService<TtsQueueMaintenanceHostedService>();
            builder.Services.AddHostedService<TtsWarmupHostedService>();
            builder.SetContentLastChanceFinder<PageNotFound>();
        }
    }
}

