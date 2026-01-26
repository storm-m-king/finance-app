namespace ExpenseTracker.Services.Contracts;

/// <summary>
/// Defines a lightweight application logging abstraction used to record
/// informational messages, warnings, errors, and execution traces.
/// </summary>
/// <remarks>
/// <para>
/// This interface intentionally avoids binding the application to a specific
/// logging framework (e.g., Serilog, NLog, Application Insights).
/// Implementations may forward log entries to files, structured log stores,
/// telemetry systems, or in-memory sinks.
/// </para>
/// <para>
/// Logging operations should be safe to call from any layer of the application
/// and should not throw exceptions during normal operation.
/// </para>
/// </remarks>
public interface IAppLogger
{
    /// <summary>
    /// Initializes the logging subsystem.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is intended to be called once during application startup
    /// before any logging operations are performed.
    /// </para>
    /// <para>
    /// Implementations may use this method to configure log sinks,
    /// establish file paths, or initialize external telemetry providers.
    /// </para>
    /// </remarks>
    void Initialize();

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <remarks>
    /// Informational messages describe normal application behavior
    /// and high-level state changes useful for diagnostics and auditing.
    /// </remarks>
    void Info(string message);

    /// <summary>
    /// Logs a warning message indicating a potential issue or unusual condition.
    /// </summary>
    /// <param name="message">The warning message to log.</param>
    /// <remarks>
    /// Warnings indicate recoverable situations that do not stop execution
    /// but may require attention (e.g., unexpected input, fallback behavior).
    /// </remarks>
    void Warn(string message);

    /// <summary>
    /// Logs an error message and optional exception details.
    /// </summary>
    /// <param name="message">A description of the error.</param>
    /// <param name="ex">
    /// An optional exception associated with the error.
    /// When provided, implementations should record relevant exception details
    /// such as the stack trace and inner exceptions.
    /// </param>
    /// <remarks>
    /// Errors represent failures that prevent an operation from completing
    /// successfully or indicate an unexpected system state.
    /// </remarks>
    void Error(string message, Exception? ex = null);

    /// <summary>
    /// Executes the specified action while recording a trace entry.
    /// </summary>
    /// <param name="name">A logical name describing the traced operation.</param>
    /// <param name="action">The action to execute.</param>
    /// <remarks>
    /// <para>
    /// Implementations may record timing, execution boundaries,
    /// or structured metadata associated with the operation.
    /// </para>
    /// <para>
    /// Any exception thrown by <paramref name="action"/> should be rethrown
    /// after being logged.
    /// </para>
    /// </remarks>
    void Trace(string name, Action action);

    /// <summary>
    /// Executes the specified function while recording a trace entry
    /// and returns its result.
    /// </summary>
    /// <typeparam name="T">The return type of the traced function.</typeparam>
    /// <param name="name">A logical name describing the traced operation.</param>
    /// <param name="func">The function to execute.</param>
    /// <returns>The result produced by <paramref name="func"/>.</returns>
    /// <remarks>
    /// <para>
    /// This overload is intended for tracing value-producing operations
    /// while preserving their return value.
    /// </para>
    /// <para>
    /// Any exception thrown by <paramref name="func"/> should be rethrown
    /// after being logged.
    /// </para>
    /// </remarks>
    T Trace<T>(string name, Func<T> func);
}