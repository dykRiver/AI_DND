using System.Runtime.CompilerServices;

public static class AsyncExtension
{
    public static AsyncAwaiter GetAwaiter(this object input)
    {
        return new AsyncAwaiter();
    }
}

public class AsyncAwaiter : INotifyCompletion
{
    private bool _isCompleted;

    public bool IsCompleted => _isCompleted;

    public object GetAwaiter()
    {
        return this;
    }

    public void OnCompleted(Action continuation)
    {
        continuation?.Invoke();
    }

    public void Complete()
    {
        _isCompleted = true;
    }

    public void GetResult()
    {

    }
}