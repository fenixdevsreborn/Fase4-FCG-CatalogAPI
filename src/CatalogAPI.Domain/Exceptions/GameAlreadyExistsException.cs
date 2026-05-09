namespace CatalogAPI.Domain.Exceptions;

public class GameAlreadyExistsException : Exception
{
    public GameAlreadyExistsException(string gameName) 
        : base($"Game with name '{gameName}' already exists.")
    {
    }
}
