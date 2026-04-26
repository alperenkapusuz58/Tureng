using System.Linq;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace ClockworkUmbraco.Helpers;

public static class HeadwordDisplay
{
    public static string? FirstTranslation(Headword hw)
    {
        if (hw.Senses == null || !hw.Senses.Any())
        {
            return null;
        }

        foreach (var block in hw.Senses)
        {
            if (block.Content is SenseItem s && !string.IsNullOrWhiteSpace(s.Translation))
            {
                return s.Translation;
            }
        }

        return null;
    }
}
