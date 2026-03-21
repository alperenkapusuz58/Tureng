using System.IO;
using ClockworkUmbraco.Models.Settings;
using Microsoft.Extensions.Configuration;

namespace ClockworkUmbraco.Helpers
{
    public static class ConfigurationManagerCustom
    {
        public static IConfiguration AppSettings { get; }
        public static ClockworkSettings ClockworkSettings { get; }
        static ConfigurationManagerCustom()
        {
            AppSettings = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

            ClockworkSettings = AppSettings.GetSection("Clockwork").Get<ClockworkSettings>();
        }
    }
}

