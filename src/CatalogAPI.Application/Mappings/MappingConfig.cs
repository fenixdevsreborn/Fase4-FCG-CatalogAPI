using CatalogAPI.Application.DTOs;
using CatalogAPI.Domain.Entities;
using Mapster;

namespace CatalogAPI.Application.Mappings;

public static class MappingConfig
{
    public static void RegisterMappings()
    {
        TypeAdapterConfig<Game, GameDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.Price, src => src.Price)
            .Map(dest => dest.Genre, src => src.Genre)
            .Map(dest => dest.ImageUrl, src => src.ImageUrl)
            .Map(dest => dest.Developer, src => src.Developer)
            .Map(dest => dest.ReleaseDate, src => src.ReleaseDate);

        // CreateGameDto -> Game mapping
        TypeAdapterConfig<CreateGameDto, Game>.NewConfig()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.Price, src => src.Price)
            .Map(dest => dest.Genre, src => src.Genre)
            .Map(dest => dest.ImageUrl, src => src.ImageUrl ?? string.Empty)
            .Map(dest => dest.Developer, src => src.Developer)
            .Map(dest => dest.ReleaseDate, src => src.ReleaseDate);
    }
}
