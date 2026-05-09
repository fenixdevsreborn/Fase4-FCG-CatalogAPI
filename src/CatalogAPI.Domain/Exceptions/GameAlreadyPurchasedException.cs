namespace CatalogAPI.Domain.Exceptions;

public class GameAlreadyPurchasedException : Exception
{
    public GameAlreadyPurchasedException(Guid userId, Guid gameId) 
        : base($"User '{userId}' has already purchased game '{gameId}'.")
    {
    }
}
