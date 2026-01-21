namespace FestGuide.Domain.Exceptions;

/// <summary>
/// Exception thrown when a device token is invalid.
/// </summary>
public class InvalidDeviceTokenException : DomainException
{
    public InvalidDeviceTokenException(string message) : base(message)
    {
    }

    public InvalidDeviceTokenException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
