using System.Text.Json.Serialization;

namespace Shared.Contracts.Events;

/// <summary>
/// Evento publicado pelo CatalogAPI quando um pedido é criado.
/// Consumido pelo PaymentsAPI para processar o pagamento.
/// </summary>
public record OrderPlacedEvent
{
    public Guid OrderId { get; init; }
    
    public Guid UserId { get; init; }
    
    public Guid GameId { get; init; }
    
    /// <summary>
    /// Preço do jogo. Aceita tanto string quanto decimal para compatibilidade com MassTransit.
    /// </summary>
    [JsonConverter(typeof(DecimalStringConverter))]
    public decimal Price { get; init; }

    public OrderPlacedEvent() { }

    public OrderPlacedEvent(Guid orderId, Guid userId, Guid gameId, decimal price)
    {
        OrderId = orderId;
        UserId = userId;
        GameId = gameId;
        Price = price;
    }
}

/// <summary>
/// Converter para aceitar price como string ou número no JSON.
/// </summary>
public class DecimalStringConverter : System.Text.Json.Serialization.JsonConverter<decimal>
{
    public override decimal Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
    {
        if (reader.TokenType == System.Text.Json.JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (decimal.TryParse(stringValue, out var decimalValue))
            {
                return decimalValue;
            }
        }
        else if (reader.TokenType == System.Text.Json.JsonTokenType.Number)
        {
            return reader.GetDecimal();
        }
        
        throw new System.Text.Json.JsonException($"Unable to convert {reader.TokenType} to decimal");
    }

    public override void Write(System.Text.Json.Utf8JsonWriter writer, decimal value, System.Text.Json.JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}
