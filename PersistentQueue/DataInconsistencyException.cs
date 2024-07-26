namespace Persistent.Queue;

public class DataInconsistencyException : Exception
{
    public DataInconsistencyException(string? message) : base(message)
    {
    }

    public DataInconsistencyException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
