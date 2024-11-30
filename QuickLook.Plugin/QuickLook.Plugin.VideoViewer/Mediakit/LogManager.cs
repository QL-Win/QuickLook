using System;

namespace WPFMediaKit;

/// <summary>
/// Log manager for the WPF-MediaKit. Set <see cref="LoggerFunc"/> to change the logging.
/// </summary>
public static class LogManager
{
    /// <summary>
    /// Main func to get an <see cref="ILog"/> for the provided logger name. Default is set to <see cref="DebugTraceLog"/>.
    /// <para>
    /// May be changed by the user code. Set to <see cref="NullLog.Instance"/> to switch off the logging.
    /// </para>
    /// </summary>
    public static Func<string, ILog> LoggerFunc = name => new DebugTraceLog(name);

    /// <summary>
    /// Returns an <see cref="ILog"/> for the provided logger name.
    /// </summary>
    public static ILog GetLogger(string name)
        => LoggerFunc(name);

    /// <summary>
    /// Retuns an <see cref="ILog"/> for the provided logger name.
    /// </summary>
    public static ILog GetLogger(Type type)
        => LoggerFunc(type.FullName);
}
