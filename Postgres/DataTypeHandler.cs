using System.Data;
using System.Text.Json;

using Dapper;

using Npgsql;

using NpgsqlTypes;

using OS.Agent.Models;

namespace OS.Agent.Postgres;

public sealed class DataTypeHandler<TData>(JsonSerializerOptions? options = null) : SqlMapper.TypeHandler<TData> where TData : Data
{
    public override void SetValue(IDbDataParameter parameter, TData? value)
    {
        var p = (NpgsqlParameter)parameter;
        p.NpgsqlDbType = NpgsqlDbType.Jsonb;
        p.Value = value is null ? DBNull.Value : JsonSerializer.Serialize(value, options);
    }

    public override TData? Parse(object value) => value switch
    {
        string s => JsonSerializer.Deserialize<TData>(s),
        JsonElement je => JsonSerializer.Deserialize<TData>(je.GetRawText()),
        ReadOnlyMemory<byte> rom => JsonSerializer.Deserialize<TData>(rom.ToString()),
        byte[] bytes => JsonSerializer.Deserialize<TData>(bytes),
        _ => null
    };
}