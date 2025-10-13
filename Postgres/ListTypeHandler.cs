using System.Data;
using System.Text.Json;

using Npgsql;

using NpgsqlTypes;

using OS.Agent.Models;

namespace OS.Agent.Postgres;

public class ListTypeHandler(JsonSerializerOptions? options = null) : Dapper.SqlMapper.TypeHandler<Tenant.SourceList>
{
    public override void SetValue(IDbDataParameter parameter, Tenant.SourceList? value)
    {
        var p = (NpgsqlParameter)parameter;
        p.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Jsonb;
        p.NpgsqlValue = value is null ? DBNull.Value : value.Select(v => JsonSerializer.Serialize(v, options)).ToArray();
    }

    public override Tenant.SourceList? Parse(object value) => value switch
    {
        string s => JsonSerializer.Deserialize<Tenant.SourceList>(s),
        JsonElement je => JsonSerializer.Deserialize<Tenant.SourceList>(je.GetRawText()),
        ReadOnlyMemory<byte> rom => JsonSerializer.Deserialize<Tenant.SourceList>(rom.ToString()),
        byte[] bytes => JsonSerializer.Deserialize<Tenant.SourceList>(bytes),
        _ => null
    };
}