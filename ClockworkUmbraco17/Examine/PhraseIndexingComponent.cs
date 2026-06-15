using ClockworkUmbraco.Helpers;
using Examine;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace ClockworkUmbraco.Examine;

/// <summary>
/// InternalIndex'e headword dokümanları için çok-değerli <c>phrases</c> alanı ekler
/// (Idioms + PhrasalVerbs). Stopword içermeyen analyzer sayesinde "from A to B" aranabilir.
/// </summary>
public sealed class PhraseIndexingComponent : IComponent
{
    public const string PhrasesFieldName = "phrases";

    private readonly IExamineManager _examineManager;
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IPublishedValueFallback _publishedValueFallback;

    public PhraseIndexingComponent(
        IExamineManager examineManager,
        IUmbracoContextFactory umbracoContextFactory,
        IPublishedValueFallback publishedValueFallback)
    {
        _examineManager = examineManager;
        _umbracoContextFactory = umbracoContextFactory;
        _publishedValueFallback = publishedValueFallback;
    }

    public void Initialize()
    {
        var internalIndexFound = _examineManager.TryGetIndex(UmbracoConstants.UmbracoIndexes.InternalIndexName, out IIndex? index);

        if (!internalIndexFound || index == null)
        {
            return;
        }

        index.TransformingIndexValues += TransformingIndexValues;
    }

    public void Terminate()
    {
        if (!_examineManager.TryGetIndex(UmbracoConstants.UmbracoIndexes.InternalIndexName, out IIndex? index))
        {
            return;
        }

        index.TransformingIndexValues -= TransformingIndexValues;
    }

    private void TransformingIndexValues(object? sender, IndexingItemEventArgs e)
    {
        if (e.ValueSet.Category != IndexTypes.Content)
        {
            return;
        }

        if (!string.Equals(
                e.ValueSet.GetValue("__NodeTypeAlias")?.ToString(),
                Headword.ModelTypeAlias,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!TryGetNodeId(e.ValueSet, out var nodeId))
        {
            return;
        }

        using var contextReference = _umbracoContextFactory.EnsureUmbracoContext();
        var content = contextReference.UmbracoContext?.Content?.GetById(nodeId);
        if (content == null || content.ContentType.Alias != Headword.ModelTypeAlias)
        {
            return;
        }

        var headword = new Headword(content, _publishedValueFallback);
        var phrases = PhraseExtractor.GetPhrases(headword).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (phrases.Length == 0)
        {
            return;
        }

        var updatedValues = e.ValueSet.Values.ToDictionary(x => x.Key, x => x.Value.ToList());
        updatedValues[PhrasesFieldName] = phrases.Cast<object>().ToList();
        e.SetValues(updatedValues.ToDictionary(x => x.Key, x => (IEnumerable<object>)x.Value));
    }

    private static bool TryGetNodeId(ValueSet valueSet, out int nodeId)
    {
        nodeId = 0;
        var rawId = valueSet.GetValue("id") ?? valueSet.GetValue("__Key");
        if (rawId == null)
        {
            return false;
        }

        return int.TryParse(rawId.ToString(), out nodeId);
    }
}

public sealed class PhraseIndexingComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Components().Append<PhraseIndexingComponent>();
    }
}
