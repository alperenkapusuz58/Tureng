using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using ClockworkUmbraco.Models.Dtos;

namespace ClockworkUmbraco.Helpers
{
    public class PIIHelper
    {
        private HttpClient _client = new HttpClient();
        IConfigurationRoot _config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

        public PIIHelper()
        {
            string apiUrl = _config.GetValue<string>("Clockwork:PII:Url");
            _client.BaseAddress = new Uri(apiUrl);
        }

        public bool SendData(string model, string type, int typeId)
        {
            PIIPostFormModel requestModel = new() { data = model, type = type, typeId = typeId };
            var requestJson = JsonConvert.SerializeObject(requestModel);

            var httpContent = new StringContent(requestJson, Encoding.UTF8, "application/json");
            var response = _client.PostAsync(_client.BaseAddress, httpContent);

            var responseContent = response.Result.Content.ReadAsStringAsync().Result;

            if (response.Result.StatusCode == System.Net.HttpStatusCode.OK) return true;
            return false;
        }

    }
}

