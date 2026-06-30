namespace Kelimebull.Tts.Core.Exceptions;

public sealed class TtsBudgetLimitException : Exception
{
    public DateTimeOffset RetryAfterUtc { get; }

    public TtsBudgetLimitException(string message, DateTimeOffset retryAfterUtc)
        : base(message)
    {
        RetryAfterUtc = retryAfterUtc;
    }
}
