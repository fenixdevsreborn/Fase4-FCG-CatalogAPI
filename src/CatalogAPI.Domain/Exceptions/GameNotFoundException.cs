namespace CatalogAPI.Domain.Exceptions;

public class GameNotFoundException : Exception
{
    public GameNotFoundException(Guid gameId) 
        : base($"Game with ID '{gameId}' was not found.")
    {
    }
}
