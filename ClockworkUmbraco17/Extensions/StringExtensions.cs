using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using ClockworkUmbraco.Models.Dtos;
using Umbraco.Cms.Core.Strings;

namespace ClockworkUmbraco.Extensions;

/// <summary>
/// The string extensions.
/// </summary>
public static class StringExtensions
{

    static readonly TimeSpan default_time_out = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The remove from end.
    /// </summary>
    /// <param name="s">
    /// The s.
    /// </param>
    /// <param name="suffix">
    /// The suffix.
    /// </param>
    /// <returns>
    /// The <see cref="string"/>.
    /// </returns>
    //public static string RemoveAfterLast(this string s, string suffix)
    //{
    //    var index = s.LastIndexOf(suffix, StringComparison.InvariantCulture);
    //    if (index > 0) s = s.Substring(0, index);
    //    return s;
    //}

    /// <summary>
    /// Removed invalid xml characters.
    /// </summary>
    /// <param name="text">
    /// The text.
    /// </param>
    /// <returns>
    /// The <see cref="string"/>.
    /// </returns>
    //public static string RemoveInvalidXmlChars(this string text)
    //{
    //    if (text == null) return null;

    //    var validXmlChars = text.Where(XmlConvert.IsXmlChar).ToArray();
    //    return new string(validXmlChars);
    //}


    public static string Slugify(this string phrase)
    {
        var str = phrase.RemoveAccent().ToUrl().ToLower();
        str = Regex.Replace(str, @"[^a-z0-9\s-]", ""); // Remove all non valid chars
        str = Regex.Replace(str, @"\s+", " ").Trim(); // convert multiple spaces into one space
        str = Regex.Replace(str, @"\s", "-"); // //Replace spaces by dashes
        return str;
    }

    private static string RemoveAccent(this string txt)
    {
        var bytes = System.Text.Encoding.GetEncoding("Cyrillic").GetBytes(txt);
        return System.Text.Encoding.ASCII.GetString(bytes);
    }
    public static string ToUrl(this string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Length > 80)
            s = s.Substring(0, 80);
        s = s.ToLower();
        s = s.Replace("ş", "s");
        s = s.Replace("ğ", "g");
        s = s.Replace("ı", "i");
        s = s.Replace("ç", "c");
        s = s.Replace("ö", "o");
        s = s.Replace("ü", "u");
        s = s.Replace("'", "");
        s = s.Replace("\"", "");
        var r = new Regex("[^a-zA-Z0-9_-]");
        //if (r.IsMatch(s))
        s = r.Replace(s, "-");
        if (!string.IsNullOrEmpty(s))
            while (s.IndexOf("--", StringComparison.Ordinal) > -1)
                s = s.Replace("--", "-");
        if (s.StartsWith("-")) s = s.Substring(1);
        if (s.EndsWith("-")) s = s.Substring(0, s.Length - 1);
        return s;
    }


    /// <summary>
    /// Rich Text içerisindeki <p> taglarını temizler.
    /// </summary>
    public static string HandlePTag(this IHtmlEncodedString s)
    {
        return s.ToString().Trim().TrimStart("<p>").TrimEnd("</p>");
    }


    /// <summary>
    /// Converts an object to a query string format.
    /// </summary>
    /// <param name="obj">The object to convert to query string.</param>
    /// <returns>A query string representation of the object.</returns>
    public static string BasicToQueryString(this object obj)
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
                        url += request.BasicToQueryString();
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

