using ClockworkUmbraco.Models.Dtos;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common;


namespace ClockworkUmbraco.Extensions
{
    public static class PublishedContentExtensions
    {

        /// <summary>
        /// Bu Panelde oluşturulan anasayfayı döner.
        /// </summary>
        public static MainPage GetMainPage(this IPublishedContent publishedContent)
        {
            return publishedContent?.AncestorOrSelf<MainPage>();
        }


        /// <summary>
        /// Bu Panelde oluşturulan site ayarlarını döner.
        /// </summary>
        /// 

        // NOT => Şimdilik yoruma alınmıştır. Panelde oluşturduğunuz takdirde açabilirsiniz.

        public static SiteSettings GetSiteSettings(this IPublishedContent publishedContent)
        {
            var mainPage = GetMainPage(publishedContent);
            return mainPage?.FirstChild<SiteSettings>();
        }

        public static DictionaryNoResults GetDictionaryNoResults(this IPublishedContent publishedContent)
        {
            var mainPage = GetMainPage(publishedContent);
            return mainPage?.FirstChild<DictionaryNoResults>();
        }


        /// <summary>
        ///  Contact Form Id ve Mail adresini döner.
        /// </summary>
        public static FormIdModel GetContactFormId(this UmbracoHelper helper, IContentTypeService contentTypeService)
        {

            /*  var root = helper.ContentAtRoot().FirstOrDefault();
              var formReceipentMail = root.FirstChild<Contact>().ReceipentMail.ToString();
              var formTemplateId = contentTypeService.Get(Contact.ModelTypeAlias).Id;

              return new FormIdModel
              {
                  receipentMail = formReceipentMail,
                  formTemplateId = formTemplateId
              }; */

            return new FormIdModel();
        }


        public static string Duzenle(string metin)
        {
            if (string.IsNullOrWhiteSpace(metin))
                return metin;

            metin = metin.Trim();

            // İlk harfi büyük yap
            metin = char.ToUpper(metin[0], new System.Globalization.CultureInfo("tr-TR")) + metin.Substring(1);

            // Sonunda nokta yoksa ekle
            if (!metin.EndsWith("."))
                metin += ".";

            return metin;
        }

    }
}

