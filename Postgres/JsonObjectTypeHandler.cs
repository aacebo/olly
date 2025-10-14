using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

using Dapper;

using Npgsql;

using NpgsqlTypes;

namespace OS.Agent.Postgres;

public sealed class JsonObjectTypeHandler : SqlMapper.ITypeHandler
{
    private JsonSerializerOptions Options { get; init; } = new(JsonSerializerDefaults.Web)
    {
        AllowOutOfOrderMetadataProperties = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public void SetValue(IDbDataParameter parameter, object? value)
    {
        var p = (NpgsqlParameter)parameter;
        p.NpgsqlDbType = NpgsqlDbType.Jsonb;
        p.Value = value is null ? DBNull.Value : JsonSerializer.Serialize(value, Options);
    }

    public object? Parse(Type type, object value)
    {
        return value switch
        {
            string s => JsonSerializer.Deserialize(s, type, Options),
            JsonElement je => JsonSerializer.Deserialize(je.GetRawText(), type, Options),
            ReadOnlyMemory<byte> rom => JsonSerializer.Deserialize(rom.Span, type, Options),
            byte[] bytes => JsonSerializer.Deserialize(bytes, type, Options),
            _ => default
        };
    }
}