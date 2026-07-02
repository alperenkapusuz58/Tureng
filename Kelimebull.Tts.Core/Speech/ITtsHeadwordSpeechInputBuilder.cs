namespace Kelimebull.Tts.Core.Speech;

public interface ITtsHeadwordSpeechInputBuilder
{
    string Build(string normalizedText, string language, string sourceType);
}
