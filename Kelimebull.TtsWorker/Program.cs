using Kelimebull.Tts.Core.Configuration;
using Kelimebull.Tts.Core.Data;
using Kelimebull.TtsWorker;
using Kelimebull.TtsWorker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddUserSecrets<Program>(optional: true);

builder.Services.Configure<TtsOptions>(builder.Configuration.GetSection(TtsOptions.SectionName));
builder.Services.AddSingleton<ITtsAudioRegistry, TtsAudioRegistry>();
builder.Services.AddSingleton<IOpenAiTtsClient, OpenAiTtsClient>();
builder.Services.AddSingleton<IR2AudioStorage, R2AudioStorage>();
builder.Services.AddSingleton<TtsRateLimiter>();
builder.Services.AddSingleton<TtsGenerationProcessor>();

if (args.Any(x => string.Equals(x, "migrate", StringComparison.OrdinalIgnoreCase)))
{
    var runner = new SqlMigrationRunner(builder.Configuration);
    await runner.RunAsync(CancellationToken.None);
    return;
}

builder.Services.AddHostedService<TtsGenerationWorker>();

var host = builder.Build();
await host.RunAsync();
