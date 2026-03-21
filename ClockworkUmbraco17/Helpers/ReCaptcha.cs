using ClockworkUmbraco.Models.Dtos;
using Newtonsoft.Json;

namespace ClockworkUmbraco.Helpers;

public class ReCaptcha
{
    private IConfiguration Config { get; }
    public ReCaptcha()
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json");
        Config = builder.Build();
    }
    public bool ValidateReCaptcha(string encodedResponse)
    {
        try
        {
            if (encodedResponse == null) return false;
            var secret = Config.GetValue<string>("Clockwork:ReCaptchaSecret");
            if (encodedResponse.StartsWith(","))
                encodedResponse = encodedResponse.Substring(1, encodedResponse.Length - 1);
            if (string.IsNullOrEmpty(encodedResponse)) return false;

            using var client = new HttpClient();
            var response = client.GetAsync($"https://www.google.com/recaptcha/api/siteverify?secret={secret}&response={encodedResponse}").Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            var reCaptcha = JsonConvert.DeserializeObject<RecaptchaResponseModel>(responseContent);
            return reCaptcha?.Success == true || (reCaptcha?.Score ?? 0) >= 0.5m;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }
}

