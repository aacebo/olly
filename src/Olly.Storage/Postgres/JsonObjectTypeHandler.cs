using System.Data;
using System.Text.Json;

using Dapper;

using Npgsql;

using NpgsqlTypes;

namespace Olly.Storage.Postgres;

public sealed class JsonObjectTypeHandler(JsonSerializerOptions? options = null) : SqlMapper.ITypeHandler
{
    public void SetValue(IDbDataParameter parameter, object? value)
    {
        var p = (NpgsqlParameter)parameter;
        p.NpgsqlDbType = NpgsqlDbType.Jsonb;
        p.Value = value is null ? DBNull.Value : JsonSerializer.Serialize(value, options);
    }

    public object? Parse(Type type, object value)
    {
        return value switch
        {
            string s => JsonSerializer.Deserialize(s, type, options),
            JsonElement je => JsonSerializer.Deserialize(je.GetRawText(), type, options),
            ReadOnlyMemory<byte> rom => JsonSerializer.Deserialize(rom.Span, type, options),
            byte[] bytes => JsonSerializer.Deserialize(bytes, type, options),
            _ => default
        };
    }
}