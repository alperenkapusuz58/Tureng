using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common;

namespace ClockworkUmbraco.Services
{
    public class PageNotFound : IContentLastChanceFinder
    {

        private readonly IServiceProvider _serviceProvider;

        public PageNotFound(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task<bool> TryFindContent(IPublishedRequestBuilder request)
        {
            using var scope = _serviceProvider.CreateScope();

            var umbracoContextAccessor = scope.ServiceProvider.GetRequiredService<IUmbracoContextAccessor>();
            var umbracoHelper = scope.ServiceProvider.GetRequiredService<UmbracoHelper>();

            if (!umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext))
            {
                return Task.FromResult(false);
            }

            var notFoundPage = umbracoHelper
                .ContentAtRoot()
                .SelectMany(x => x.DescendantsOrSelf())
                .FirstOrDefault(c => c.ContentType.Alias == "pageNotFound"); // oluşturduğunuz 404 sayfası alias'ını kullanın

            if (notFoundPage == null)
            {
                return Task.FromResult(false);
            }

            request.SetPublishedContent(notFoundPage);
            request.SetResponseStatus(404);
            return Task.FromResult(true);
        }
    }
}



