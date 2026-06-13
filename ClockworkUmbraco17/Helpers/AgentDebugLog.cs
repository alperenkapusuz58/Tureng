using System.Text.Json;

namespace ClockworkUmbraco.Helpers;

internal static class AgentDebugLog
{
    private const string SessionId = "8ad337";
    private const string LogPath = @"C:\Users\Alperen\source\repos\kelimebull.com\debug-8ad337.log";

    public static void Write(string location, string message, object data, string hypothesisId, string runId = "pre-fix")
    {
        try
        {
            var payload = new
            {
                sessionId = SessionId,
                id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}",
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                location,
                message,
                data,
                runId,
                hypothesisId,
            };
            global::System.IO.File.AppendAllText(LogPath, JsonSerializer.Serialize(payload) + Environment.NewLine);
        }
        catch
        {
            // Debug instrumentation must never affect request/index flow.
        }
    }
}
