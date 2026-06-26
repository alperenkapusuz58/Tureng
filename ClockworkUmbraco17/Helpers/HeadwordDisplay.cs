using System.Linq;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace ClockworkUmbraco.Helpers;

public static class HeadwordDisplay
{
    public static string? FirstTranslation(Headword hw)
    {
        if (hw.PartofSpeech == null || !hw.PartofSpeech.Any())
        {
            return null;
        }

        foreach (var block in hw.PartofSpeech)
        {
            if (block.Content is not SenseCategoryItem category || category.Items == null)
            {
                continue;
            }

            foreach (var senseBlock in category.Items)
            {
                if (senseBlock.Content is SenseItem sense && !string.IsNullOrWhiteSpace(sense.Translation))
                {
                    return sense.Translation;
                }
            }
        }

        return null;
    }
}
