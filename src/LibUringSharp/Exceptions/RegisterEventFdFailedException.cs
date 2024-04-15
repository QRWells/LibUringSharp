using System.Runtime.Serialization;

namespace QRWells.LibUringSharp.Exceptions;

[Serializable]
internal class RegisterEventFdFailedException : Exception
{
    public RegisterEventFdFailedException()
    {
    }

    public RegisterEventFdFailedException(string? message) : base(message)
    {
    }

    public RegisterEventFdFailedException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}