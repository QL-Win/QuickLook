using System;
using System.Diagnostics;

namespace WPFMediaKit;

/// <summary>
/// A logger.
/// </summary>
public interface ILog
{
    /// <summary>
    /// If the info level is enabled.
    /// </summary>
    bool IsInfoEnabled { get; }

    /// <summary>
    /// If the debug level is enabled.
    /// </summary>
    bool IsDebugEnabled { get; }

    /// <summary>
    /// Logs the error message with a possible exception.
    /// </summary>
    /// <param name="exception">The exception. May be null.</param>
    /// <param name="message">A (formatted) message.</param>
    /// <param name="args">Arguments to the formatted message. May be missing.
    void Error(Exception exception, string message, params object[] args);

    /// <summary>
    /// Logs the warning message.
    /// </summary>
    /// <param name="message">A (formatted) message.</param>
    /// <param name="args">Arguments to the formatted message. May be missing.
    void Warn(string message, params object[] args);

    /// <summary>
    /// Logs the info message.
    /// </summary>
    /// <param name="message">A (formatted) message.</param>
    /// <param name="args">Arguments to the formatted message. May be missing.
    void Info(string message, params object[] args);

    /// <summary>
    /// Logs the debug message.
    /// </summary>
    /// <param name="message">A (formatted) message.</param>
    /// <param name="args">Arguments to the formatted message. May be missing.
    void Debug(string message, params object[] args);
}

public static class ILogExtensions
{
    /// <summary>
    /// Log the error without an exception.
    /// </summary>
    public static void Error(this ILog thiz, string message, params object[] args)
        => thiz.Error(null, message, args);
}

/// <summary>
/// Logging to <see cref="System.Diagnostics.Trace"/>.
/// </summary>
public class DebugTraceLog : ILog
{
    private string _name;

    public DebugTraceLog(string name)
    {
        _name = name;
    }

    public virtual bool IsInfoEnabled => true;

    public virtual bool IsDebugEnabled => true;

    public virtual void Error(Exception exception, string message, params object[] args)
    {
        Trace.TraceError(AddName(message), args);
        if (exception != null)
            Trace.TraceError(exception.ToString());
    }

    public virtual void Warn(string message, params object[] args)
    {
        Trace.TraceWarning(AddName(message), args);
    }

    public virtual void Info(string message, params object[] args)
    {
        if (!IsInfoEnabled)
            return;
        Trace.TraceInformation(AddName(message), args);
    }

    public virtual void Debug(string message, params object[] args)
    {
        if (!IsDebugEnabled)
            return;
        Trace.WriteLine(string.Format(AddName(message), args));
    }

    protected virtual string AddName(string message)
    {
        if (string.IsNullOrEmpty(_name))
            return message;
        return _name + " - " + message;
    }
}

/// <summary>
/// No-op implementation of ILog.
/// </summary>
public class NullLog : ILog
{
    private static NullLog _instance;

    public static NullLog Instance
        => _instance != null ? _instance : (_instance = new NullLog());

    public bool IsInfoEnabled => false;

    public virtual bool IsDebugEnabled => false;

    public void Error(Exception exception, string message, params object[] args)
    { }

    public void Info(string message, params object[] args)
    { }

    public void Debug(string message, params object[] args)
    { }

    public void Warn(string message, params object[] args)
    { }
}
