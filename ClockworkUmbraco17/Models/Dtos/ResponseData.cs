using System.Net;

namespace ClockworkUmbraco.Models.Dtos
{
    public class ResponseData
    {
        public HttpStatusCode status { get; set; }
        public string? content { get; set; }
        public string? content_ex { get; set; }
    }
}

