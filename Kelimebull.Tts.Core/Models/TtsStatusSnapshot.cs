namespace Kelimebull.Tts.Core.Models;

public sealed record TtsStatusSnapshot(
    TtsAudioRecord Registry,
    string? QueueStatus,
    string? QueueError);

public static class TtsStatusResolver
{
    public static string ResolveStatus(TtsStatusSnapshot snapshot)
    {
        if (string.Equals(snapshot.Registry.Status, TtsStatuses.Completed, StringComparison.OrdinalIgnoreCase))
        {
            return "ready";
        }

        if (string.Equals(snapshot.QueueStatus, TtsStatuses.Processing, StringComparison.OrdinalIgnoreCase)
            || string.Equals(snapshot.Registry.Status, TtsStatuses.Processing, StringComparison.OrdinalIgnoreCase))
        {
            return "processing";
        }

        if (string.Equals(snapshot.Registry.Status, TtsStatuses.Failed, StringComparison.OrdinalIgnoreCase)
            || string.Equals(snapshot.QueueStatus, TtsStatuses.Failed, StringComparison.OrdinalIgnoreCase))
        {
            return "failed";
        }

        return TtsStatuses.Pending;
    }

    public static string? ResolveError(TtsStatusSnapshot snapshot)
        => snapshot.Registry.ErrorMessage ?? snapshot.QueueError;
}
