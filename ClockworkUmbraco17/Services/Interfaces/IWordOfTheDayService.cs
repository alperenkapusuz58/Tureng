using Umbraco.Cms.Web.Common.PublishedModels;

namespace ClockworkUmbraco.Services.Interfaces;

public interface IWordOfTheDayService
{
    Headword? GetWordOfTheDay(DateTime? date = null);
}
