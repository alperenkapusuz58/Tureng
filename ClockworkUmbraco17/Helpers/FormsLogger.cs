using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ClockworkUmbraco.Helpers;

public static class FormsLogger
{


    public static Task LogToFile<T>(this T form, string json) where T : Type
    {
        var filePath = form.CreateLogFile();
        System.IO.File.AppendAllText(filePath, $"{JObject.Parse(json).ToString(Formatting.Indented)},");
        return Task.CompletedTask;
    }
    private static string CreateLogFile<T>(this T form) where T : Type
    {
        var folderName = @$"Log\{DateTime.Today:ddMMyyyy}";
        var fileName = @$"\{form.Name}";

        if (!Directory.Exists($"{AppDomain.CurrentDomain.BaseDirectory}{folderName}")) Directory.CreateDirectory($"{AppDomain.CurrentDomain.BaseDirectory}{folderName}");
        if (!System.IO.File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}{folderName}{fileName}"))
            using (System.IO.File.CreateText($"{AppDomain.CurrentDomain.BaseDirectory}{folderName}{fileName}"))
            {

            }

        return $"{AppDomain.CurrentDomain.BaseDirectory}{folderName}{fileName}";
    }

}

