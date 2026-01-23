namespace FestConnect.Domain.Exceptions;

/// <summary>
/// Exception thrown when attempting to send to an unregistered device.
/// </summary>
public class UnregisteredDeviceException : DomainException
{
    public UnregisteredDeviceException(string message) : base(message)
    {
    }

    public UnregisteredDeviceException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
