namespace QRWells.LibUringSharp.Exceptions;

public class RingInitFailedException(string message) : IOException(message)
{
}