namespace LibUringSharp.Exceptions;

public class RingInitFailedException : IOException
{
    public RingInitFailedException(string message) : base(message)
    {
    }
}