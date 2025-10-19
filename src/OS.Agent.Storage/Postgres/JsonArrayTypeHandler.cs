using System.Collections;
using System.Data;
using System.Text.Json;

using Dapper;

using Npgsql;

using NpgsqlTypes;

namespace OS.Agent.Storage.Postgres;

public sealed class JsonArrayTypeHandler(JsonSerializerOptions? options = null) : SqlMapper.ITypeHandler
{
    public void SetValue(IDbDataParameter parameter, object? value)
    {
        var isEnumerable = value is not null && value.GetType().IsAssignableTo(typeof(IEnumerable));
        var p = (NpgsqlParameter)parameter;
        p.NpgsqlDbType = NpgsqlDbType.Jsonb;
        p.Value = value is null ? "[]" : JsonSerializer.Serialize(isEnumerable ? value : new[] { value }, options);
    }

    public object? Parse(Type type, object value) => value switch
    {
        string s => JsonSerializer.Deserialize(s, type, options),
        JsonElement je => JsonSerializer.Deserialize(je.GetRawText(), type, options),
        ReadOnlyMemory<byte> rom => JsonSerializer.Deserialize(rom.Span, type, options),
        byte[] bytes => JsonSerializer.Deserialize(bytes, type, options),
        _ => default
    };
}