namespace Kelimebull.Tts.Core.Speech;

public interface ICmuPronunciationLookup
{
    bool TryGetPhones(string word, out IReadOnlyList<string> phones);

    bool HasUnstressedInitialVowel(string word);
}
