using Avalonia.Logging;

namespace MonacoLabs.DragDrop.Tests;

/// <summary>
/// Records Avalonia log entries at <paramref name="minimumLevel"/> and above while installed as <see cref="Logger.Sink"/>.
/// Disposing restores the previous sink.
/// </summary>
public sealed class RecordingLogSink(LogEventLevel minimumLevel = LogEventLevel.Warning) : ILogSink, IDisposable
{
    private ILogSink? _previous;

    public List<LogEntry> Entries { get; } = [];

    public static RecordingLogSink Install(LogEventLevel minimumLevel = LogEventLevel.Warning)
    {
        var sink = new RecordingLogSink(minimumLevel) { _previous = Logger.Sink };
        Logger.Sink = sink;
        return sink;
    }

    public bool IsEnabled(LogEventLevel level, string area) => level >= minimumLevel;

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate) =>
        Entries.Add(new LogEntry(level, area, source, messageTemplate, []));

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues) =>
        Entries.Add(new LogEntry(level, area, source, messageTemplate, propertyValues));

    public void Dispose() => Logger.Sink = _previous;

    public sealed record LogEntry(LogEventLevel Level, string Area, object? Source, string MessageTemplate, object?[] Values);
}
