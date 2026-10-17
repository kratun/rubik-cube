namespace Rubik.Api.Errors;

/// <summary>The body of every non-2xx response (AD-6). <paramref name="TraceId"/> is the one in the log entry.</summary>
public sealed record ErrorDetails(string Type, string Title, int Status, string Detail, string TraceId);
