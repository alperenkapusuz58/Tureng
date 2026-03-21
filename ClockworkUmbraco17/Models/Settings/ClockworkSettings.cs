using System.Collections.Generic;

namespace ClockworkUmbraco.Models.Settings
{
    public class ClockworkSettings
    {
        public List<string>? VirtualNodes { get; set; }
        public string? ReCaptchaSecret { get; set; }
    }
}

