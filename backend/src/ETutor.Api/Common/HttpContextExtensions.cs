using System.Diagnostics;

namespace ETutor.Api.Common;

public static class HttpContextExtensions
{
    public const string TraceIdHeader = "X-Trace-Id";

    /// <summary>
    /// The W3C trace ID of the current request. It is also written to every log event, so a client
    /// can quote it and the matching server logs can be found.
    /// </summary>
    public static string GetTraceId(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    }
}
