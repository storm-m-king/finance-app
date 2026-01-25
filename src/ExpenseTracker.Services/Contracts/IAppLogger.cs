namespace ExpenseTracker.Services.Contracts;

public interface IAppLogger
{
    void Initialize();
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception? ex = null);
    void Trace(string name, Action action);
    T Trace<T>(string name, Func<T> func);
}