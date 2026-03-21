using System.Net;
using System.Reflection;
using System.Text;
using ClockworkUmbraco.Models.Dtos;

namespace ClockworkUmbraco.Extensions
{
    public static class HttpRequestExtensions
    {
        static readonly TimeSpan default_time_out = TimeSpan.FromSeconds(30);

        private static string BasicToQueryString(this object obj)
        {
            if (obj == null)
                return string.Empty;

            var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetValue(obj) != null);

            var queryParts = properties.Select(property =>
            {
                var value = property.GetValue(obj);
                var name = property.Name;
                return $"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value?.ToString() ?? string.Empty)}";
            });

            return string.Join("&", queryParts);
        }


        /// <summary>
        /// Belirtilen URL'ye GET isteği gönderir ve yanıt içeriğini ISO-8859-9 (Türkçe) kodlaması ile döner.
        /// </summary>
        /// <param name="url">İstek yapılacak adres (URL).</param>
        /// <param name="request">
        /// Query string parametreleri için kullanılacak nesne. 
        /// Nesnedeki public property'ler otomatik olarak query string'e çevrilir.
        /// </param>
        /// <param name="time_out">Opsiyonel olarak istek için zaman aşımı süresi. (Varsayılan: 30 saniye)</param>
        /// <returns>
        /// Sunucudan dönen HTTP durum kodu ve içerik bilgilerini barındıran <c>ResponseData</c> nesnesi.
        /// </returns>
        public static Task<ResponseData> GetResponseEncodingIso8859_9(string url, object request, TimeSpan? time_out = null)
        {
            return Task.Run(async () =>
            {
                ResponseData result = new ResponseData();
                try
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
                        if (!time_out.HasValue)
                            httpClient.Timeout = default_time_out;
                        else
                            httpClient.Timeout = time_out.Value;



                        if (request != null)
                        {
                            if (!url.EndsWith("?"))
                                url += "?";
                            url += BasicToQueryString(request);
                            if (url.EndsWith("&"))
                                url = url.Substring(0, url.Length - 1);
                        }


                        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                        using (HttpResponseMessage response = await httpClient.GetAsync(url))
                        {
                            result.status = response.StatusCode;
                            byte[] arr = await response.Content.ReadAsByteArrayAsync();
                            if (arr != null)
                                result.content = Encoding.GetEncoding("iso-8859-9").GetString(arr);
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.status = HttpStatusCode.InternalServerError;
                    result.content = ex.ToString();
                    result.content_ex = url;
                }
                return result;
            });
        }

    }
}

