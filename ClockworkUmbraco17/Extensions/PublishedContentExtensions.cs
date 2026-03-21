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
        //public static MainPage GetMainPage(this IPublishedContent publishedContent)
        //{
        //    return publishedContent.AncestorOrSelf<MainPage>();
        //}


        /// <summary>
        /// Bu Panelde oluşturulan site ayarlarını döner.
        /// </summary>
        /// 

        // NOT => Şimdilik yoruma alınmıştır. Panelde oluşturduğunuz takdirde açabilirsiniz.

        //public static SiteSettings GetSiteSettings(this IPublishedContent publishedContent)
        //{
        //    var mainPage = GetMainPage(publishedContent);
        //    return mainPage?.FirstChild<SiteSettings>();
        //}


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

    }
}

