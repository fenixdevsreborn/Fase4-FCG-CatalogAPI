namespace CatalogAPI.Domain.Exceptions;

public class PublishEventFailedException : Exception
{
    public PublishEventFailedException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
