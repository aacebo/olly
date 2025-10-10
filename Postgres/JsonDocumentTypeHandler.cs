using System.Data;
using System.Text.Json;

using Dapper;

using Npgsql;

using NpgsqlTypes;

namespace OS.Agent.Postgres;

public sealed class JsonDocumentTypeHandler(JsonSerializerOptions? options = null) : SqlMapper.TypeHandler<JsonDocument?>
{
    public override void SetValue(IDbDataParameter parameter, JsonDocument? value)
    {
        var p = (NpgsqlParameter)parameter;
        p.NpgsqlDbType = NpgsqlDbType.Jsonb;
        p.Value = value is null ? DBNull.Value : JsonSerializer.Serialize(value.RootElement, options);
    }

    public override JsonDocument? Parse(object value) => value switch
    {
        string s => JsonDocument.Parse(s),
        JsonElement je => JsonDocument.Parse(je.GetRawText()),
        ReadOnlyMemory<byte> rom => JsonDocument.Parse(rom.ToString()),
        byte[] bytes => JsonDocument.Parse(bytes),
        _ => null
    };
}